using TradingJournalGPT.Models;
using TradingJournalGPT.Utils;

namespace TradingJournalGPT.Services
{
    public class TradingJournalService
    {
        private readonly ChatGptService _chatGptService;
        private readonly GoogleSheetsService _googleSheetsService;
        private readonly LocalStorageService _localStorageService;
        private readonly ImageStorageService _imageStorageService;
        private readonly FloatDataService _floatDataService;

        public TradingJournalService()
        {
            // Initialize services with configuration
            var config = ConfigurationManager.GetConfiguration();
            
            _chatGptService = new ChatGptService(config.OpenAiApiKey);
            _googleSheetsService = new GoogleSheetsService(
                config.GoogleCredentialsPath,
                config.GoogleSpreadsheetId,
                config.GoogleSheetName
            );
            _localStorageService = new LocalStorageService();
            _imageStorageService = new ImageStorageService();
            _floatDataService = new FloatDataService();
        }

        public async Task<TradeData> AnalyzeChartImage(string imagePath)
        {
            Console.WriteLine($"TradingJournalService.AnalyzeChartImage called for: {imagePath}");
            try
            {
                // Initialize float data service
                Console.WriteLine("Initializing float data service...");
                await _floatDataService.InitializeAsync();
                
                // Use ChatGPT to analyze the chart image
                Console.WriteLine("Calling ChatGPT service...");
                var tradeData = await _chatGptService.AnalyzeChartImage(imagePath);
                Console.WriteLine($"ChatGPT returned: Symbol={tradeData.Symbol}, Date={tradeData.Date}");
                
                // Get float data for the symbol
                Console.WriteLine($"Getting float data for symbol: {tradeData.Symbol}");
                var floatValue = _floatDataService.GetFloat(tradeData.Symbol);
                tradeData.Float = floatValue;
                Console.WriteLine($"Float value for {tradeData.Symbol}: {floatValue}");
                
                // Get the next trade sequence number for this symbol and date
                Console.WriteLine("Getting next trade sequence...");
                var nextSeq = _imageStorageService.GetNextTradeSeq(tradeData.Symbol, tradeData.Date);
                tradeData.TradeSeq = nextSeq;
                Console.WriteLine($"Assigned TradeSeq: {nextSeq}");
                
                // Store the image locally with the sequence number
                Console.WriteLine("Storing chart image...");
                var storedImagePath = await _imageStorageService.StoreChartImageAsync(imagePath, tradeData.Symbol, tradeData.Date, nextSeq);
                tradeData.ChartImagePath = storedImagePath;
                Console.WriteLine($"Image stored at: {storedImagePath}");
                
                return tradeData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TradingJournalService.AnalyzeChartImage: {ex.Message}");
                throw new Exception($"Error analyzing chart image: {ex.Message}");
            }
        }

        public async Task<ChatGptService.OnlineData> GetOnlineDataForTrade(string symbol, DateTime date)
        {
            try
            {
                Console.WriteLine($"Getting online data for {symbol} on {date:yyyy-MM-dd}");
                var onlineData = await _chatGptService.GetOnlineDataForTrade(symbol, date);
                Console.WriteLine($"Online data retrieved: PreviousClose={onlineData.PreviousDayClose}, Volume={onlineData.Volume}");
                return onlineData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting online data for {symbol}: {ex.Message}");
                throw new Exception($"Error getting online data for {symbol}: {ex.Message}");
            }
        }

        public async Task RecordTrade(TradeData tradeData, bool useLocalStorage = false)
        {
            try
            {
                if (useLocalStorage)
                {
                    await _localStorageService.RecordTrade(tradeData);
                    Console.WriteLine("Trade saved locally successfully!");
                }
                else
                {
                    // Save to Google Sheets
                    await _googleSheetsService.InitializeSheet();
                    await _googleSheetsService.RecordTrade(tradeData);
                    Console.WriteLine("Trade saved to Google Sheets successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving trade: {ex.Message}");
                throw;
            }
        }

        public async Task<List<TradeData>> GetRecentTrades(int limit = 10, bool useLocalStorage = false)
        {
            try
            {
                if (useLocalStorage)
                {
                    return await _localStorageService.GetRecentTrades(limit);
                }
                else
                {
                    return await _googleSheetsService.GetRecentTrades(limit);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving trades: {ex.Message}");
                return new List<TradeData>();
            }
        }

        public async Task<TradeData?> GetTradeBySymbol(string symbol, DateTime? date = null)
        {
            try
            {
                var trades = await _googleSheetsService.GetRecentTrades(100); // Get more trades to search
                
                var query = trades.Where(t => t.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
                
                if (date.HasValue)
                {
                    query = query.Where(t => t.EntryDate.Date == date.Value.Date);
                }
                
                return query.OrderByDescending(t => t.RecordedDate).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving trade by symbol: {ex.Message}");
            }
        }

        public async Task<List<TradeData>> GetTradesByDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                var trades = await _googleSheetsService.GetRecentTrades(1000); // Get many trades
                
                return trades.Where(t => t.EntryDate.Date >= startDate.Date && t.EntryDate.Date <= endDate.Date)
                           .OrderByDescending(t => t.EntryDate)
                           .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving trades by date range: {ex.Message}");
            }
        }

        public async Task<decimal> GetTotalProfitLoss(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var trades = await _googleSheetsService.GetRecentTrades(1000);
                
                var query = trades.AsEnumerable();
                
                if (startDate.HasValue)
                {
                    query = query.Where(t => t.EntryDate.Date >= startDate.Value.Date);
                }
                
                if (endDate.HasValue)
                {
                    query = query.Where(t => t.EntryDate.Date <= endDate.Value.Date);
                }
                
                return query.Sum(t => t.ProfitLoss);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating total P&L: {ex.Message}");
            }
        }

        public async Task<Dictionary<string, object>> GetTradingStatistics(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var trades = await _googleSheetsService.GetRecentTrades(1000);
                
                var query = trades.AsEnumerable();
                
                if (startDate.HasValue)
                {
                    query = query.Where(t => t.EntryDate.Date >= startDate.Value.Date);
                }
                
                if (endDate.HasValue)
                {
                    query = query.Where(t => t.EntryDate.Date <= endDate.Value.Date);
                }
                
                var tradeList = query.ToList();
                
                var stats = new Dictionary<string, object>
                {
                    ["TotalTrades"] = tradeList.Count,
                    ["TotalProfitLoss"] = tradeList.Sum(t => t.ProfitLoss),
                    ["WinningTrades"] = tradeList.Count(t => t.ProfitLoss > 0),
                    ["LosingTrades"] = tradeList.Count(t => t.ProfitLoss < 0),
                    ["WinRate"] = tradeList.Count > 0 ? (decimal)tradeList.Count(t => t.ProfitLoss > 0) / tradeList.Count * 100 : 0,
                    ["AverageProfit"] = tradeList.Count > 0 ? tradeList.Average(t => t.ProfitLoss) : 0,
                    ["LargestWin"] = tradeList.Any() ? tradeList.Max(t => t.ProfitLoss) : 0,
                    ["LargestLoss"] = tradeList.Any() ? tradeList.Min(t => t.ProfitLoss) : 0
                };
                
                return stats;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calculating trading statistics: {ex.Message}");
            }
        }
    }
} 