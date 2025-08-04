using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using TradingJournalGPT.Models;

namespace TradingJournalGPT.Services
{
    public class GoogleSheetsService
    {
        private readonly SheetsService _sheetsService;
        private readonly string _spreadsheetId;
        private readonly string _sheetName;

        public GoogleSheetsService(string credentialsPath, string spreadsheetId, string sheetName = "")
        {
            _spreadsheetId = spreadsheetId;
            _sheetName = string.IsNullOrEmpty(sheetName) ? "Sheet1" : sheetName;

            try
            {
                // Load credentials
                GoogleCredential credential;
                using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
                {
                    credential = GoogleCredential.FromStream(stream)
                        .CreateScoped(SheetsService.Scope.Spreadsheets);
                }

                // Create the service
                _sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Trading Journal GPT"
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Error initializing Google Sheets service: {ex.Message}");
            }
        }

        public async Task RecordTrade(TradeData tradeData)
        {
            try
            {
                // Prepare the row data to match the actual sheet structure
                                   var rowData = new List<object>
                   {
                       tradeData.Symbol,                                    // A: Symbol
                       tradeData.Date.ToString("yyyy-MM-dd"),              // B: Date
                       tradeData.PreviousDayClose.ToString("F2"),          // C: Previous Day Close
                       tradeData.HighAfterVolumeSurge.ToString("F2"),      // D: High After Volume Surge
                       tradeData.LowAfterVolumeSurge.ToString("F2"),       // E: Low After Volume Surge
                                               tradeData.GapPercentToHigh.ToString("F2") + "%",    // F: Gap % (Close to High)
                        tradeData.GapPercentHighToLow.ToString("F2") + "%", // G: Gap % (High to Low)
                        (tradeData.Volume / 1000000).ToString("F2")        // H: Volume (in millions, no M suffix)
                   };

                // Create the value range
                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { rowData }
                };

                // Append the data to the sheet (A:H)
                var appendRequest = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, $"{_sheetName}!A:H");
                appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
                
                await appendRequest.ExecuteAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error recording trade to Google Sheets: {ex.Message}");
            }
        }

        public async Task<List<TradeData>> GetRecentTrades(int limit = 10)
        {
            try
            {
                // Get the data from the sheet (A:H)
                var range = $"{_sheetName}!A:H";
                var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
                var response = await request.ExecuteAsync();

                var trades = new List<TradeData>();

                if (response.Values != null && response.Values.Count > 1) // Skip header row
                {
                    // Get the last 'limit' rows (most recent trades)
                    var recentRows = response.Values.Skip(Math.Max(0, response.Values.Count - limit - 1)).Take(limit + 1).ToList();
                    
                    foreach (var row in recentRows.Skip(1)) // Skip header
                    {
                        if (row.Count >= 8) // Ensure we have enough columns for A-H
                        {
                            var trade = new TradeData
                            {
                                Symbol = row[0]?.ToString() ?? "",
                                Date = ParseDate(row[1]?.ToString() ?? ""),
                                PreviousDayClose = ParseDecimal(row[2]?.ToString() ?? "0"),
                                HighAfterVolumeSurge = ParseDecimal(row[3]?.ToString() ?? "0"),
                                LowAfterVolumeSurge = ParseDecimal(row[4]?.ToString() ?? "0"),
                                                                 GapPercentToHigh = ParseDecimal((row[5]?.ToString() ?? "0").Replace("%", "")),
                                 GapPercentHighToLow = ParseDecimal((row[6]?.ToString() ?? "0").Replace("%", "")),
                                 Volume = ParseVolumeFromMillions(row[7]?.ToString() ?? "0"),
                                RecordedDate = DateTime.Now // Set current time as recorded date
                            };
                            trades.Add(trade);
                        }
                    }
                }

                return trades.OrderByDescending(t => t.RecordedDate).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving trades from Google Sheets: {ex.Message}");
            }
        }

        public async Task InitializeSheet()
        {
            try
            {
                Console.WriteLine($"Attempting to access Google Sheet: {_spreadsheetId}");
                Console.WriteLine($"Service account email: tradingjournalbot@long-equinox-467916-v9.iam.gserviceaccount.com");
                
                // First, try to get the spreadsheet metadata to test access
                var spreadsheetRequest = _sheetsService.Spreadsheets.Get(_spreadsheetId);
                var spreadsheet = await spreadsheetRequest.ExecuteAsync();
                Console.WriteLine($"Successfully accessed spreadsheet: {spreadsheet.Properties.Title}");
                
                // Check if the sheet exists and has headers (A1:H1)
                var range = $"{_sheetName}!A1:H1";
                var request = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range);
                var response = await request.ExecuteAsync();

                if (response.Values == null || response.Values.Count == 0)
                {
                    Console.WriteLine("No headers found, adding headers to the sheet...");
                    // Add headers to match the actual sheet structure
                    var headers = new List<object>
                    {
                        "Symbol",                    // A
                        "Date",                      // B
                        "Previous Day Close",        // C
                        "High After Volume Surge",   // D
                        "Low After Volume Surge",    // E
                        "Gap % (Close to High)",    // F
                        "Gap % (High to Low)",      // G
                        "Volume"                     // H
                    };

                    var valueRange = new ValueRange
                    {
                        Values = new List<IList<object>> { headers }
                    };

                    var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, $"{_sheetName}!A1");
                    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    
                    await updateRequest.ExecuteAsync();
                    Console.WriteLine("Headers added successfully!");
                }
                else
                {
                    Console.WriteLine("Headers already exist in the sheet.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing Google Sheet: {ex.Message}");
                Console.WriteLine("Please ensure:");
                Console.WriteLine("1. The Google Sheet exists with ID: " + _spreadsheetId);
                Console.WriteLine("2. The sheet is shared with: tradingjournalbot@long-equinox-467916-v9.iam.gserviceaccount.com");
                Console.WriteLine("3. The service account has 'Editor' permissions");
                throw new Exception($"Error initializing Google Sheet: {ex.Message}");
            }
        }

        private DateTime ParseDateTime(string value)
        {
            return DateTime.TryParse(value, out var date) ? date : DateTime.Now;
        }

        private DateTime ParseDate(string value)
        {
            return DateTime.TryParse(value, out var date) ? date : DateTime.Now;
        }

        private decimal ParseDecimal(string value)
        {
            return decimal.TryParse(value, out var result) ? result : 0m;
        }

        private int ParseInt(string value)
        {
            return int.TryParse(value, out var result) ? result : 0;
        }

        private long ParseLong(string value)
        {
            return long.TryParse(value, out var result) ? result : 0;
        }

        private long ParseVolumeFromMillions(string value)
        {
            // Handle format like "30.64" (no M suffix)
            value = value.Replace(",", "");
            if (decimal.TryParse(value, out var result))
            {
                return (long)(result * 1000000); // Convert millions to actual number
            }
            return 0;
        }


    }
} 