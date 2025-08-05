using TradingJournalGPT.Models;
using System.Text.Json;
using System.Text;

namespace TradingJournalGPT.Services
{
    public class ChatGptService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public ChatGptService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<TradeData> AnalyzeChartImage(string imagePath)
        {
            try
            {
                // Read the image file
                var imageBytes = await File.ReadAllBytesAsync(imagePath);
                var imageBase64 = Convert.ToBase64String(imageBytes);

                // Enhanced prompt: Extract chart data including low before volume surge and total volume
                var prompt = @"Look at this chart and extract the stock name, date, volume surge high, low after volume surge, low BEFORE the volume surge high, and total volume traded for this symbol on this date.

                Return ONLY this JSON format with actual numbers (not null):
                {
                    ""symbol"": ""stock symbol"",
                    ""date"": ""YYYY-MM-DD"",
                    ""highAfterVolumeSurge"": 0.00,
                    ""lowAfterVolumeSurge"": 0.00,
                    ""lowBeforeVolumeSurge"": 0.00,
                    ""totalVolume"": 0
                }

                IMPORTANT: 
                - Use the high and low AFTER the volume surge, not the day's HOD/LOD
                - lowBeforeVolumeSurge should be the low price BEFORE the volume surge high occurred
                - totalVolume should be the total volume traded for this symbol on this date (in whole numbers)
                - All numeric values must be actual numbers, not null";

                // Single API call
                var response = await CallChatGptApi(prompt, imageBase64);
                var chartData = ParseChartDataFromResponse(response);

                // Create TradeData with default values for missing data
                return CreateTradeDataFromChartData(chartData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error analyzing chart image: {ex.Message}");
            }
        }





        private async Task<string> CallChatGptApi(string prompt, string imageBase64)
        {
            var requestPayload = new
            {
                model = "gpt-4o",
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "text",
                                text = prompt
                            },
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:image/png;base64,{imageBase64}"
                                }
                            }
                        }
                    }
                },
                max_tokens = 1000
            };

            var jsonRequest = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI API error: {response.StatusCode} - {responseText}");
            }

            using var responseDoc = JsonDocument.Parse(responseText);
            var choices = responseDoc.RootElement.GetProperty("choices");
            var firstChoice = choices[0];
            var message = firstChoice.GetProperty("message");
            return message.GetProperty("content").GetString() ?? "";
        }

        private ChartData ParseChartDataFromResponse(string responseText)
        {
            try
            {
                var jsonStart = responseText.IndexOf('{');
                var jsonEnd = responseText.LastIndexOf('}') + 1;
                
                if (jsonStart == -1 || jsonEnd == 0)
                {
                    throw new Exception("Could not find JSON in response");
                }

                var jsonText = responseText.Substring(jsonStart, jsonEnd - jsonStart);
                using var document = System.Text.Json.JsonDocument.Parse(jsonText);
                var root = document.RootElement;

                return new ChartData
                {
                    Symbol = GetStringValue(root, "symbol"),
                    Date = GetDateTimeValue(root, "date"),
                    HighAfterVolumeSurge = GetDecimalValue(root, "highAfterVolumeSurge"),
                    LowAfterVolumeSurge = GetDecimalValue(root, "lowAfterVolumeSurge"),
                    LowBeforeVolumeSurge = GetDecimalValue(root, "lowBeforeVolumeSurge"),
                    TotalVolume = GetIntValue(root, "totalVolume")
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing chart data response: {ex.Message}");
            }
        }





        private TradeData CreateTradeDataFromChartData(ChartData chartData)
        {
            var tradeData = new TradeData
            {
                Symbol = chartData.Symbol,
                Date = chartData.Date,
                HighAfterVolumeSurge = chartData.HighAfterVolumeSurge,
                LowAfterVolumeSurge = chartData.LowAfterVolumeSurge,
                LowBeforeVolumeSurge = chartData.LowBeforeVolumeSurge,
                PreviousDayClose = 0m, // Will be populated by Get Online Data
                GapPercentToHigh = 0m, // Will be calculated when previous day close is available
                GapPercentHighToLow = 0m, // Will be calculated when previous day close is available
                Volume = 0, // Will be populated by Get Online Data
                TotalVolume = chartData.TotalVolume, // Use the volume from ChatGPT analysis
                Analysis = ""
            };

            // Set legacy fields for backward compatibility
            tradeData.EntryDate = tradeData.Date;
            tradeData.ExitDate = tradeData.Date;
            tradeData.EntryPrice = tradeData.PreviousDayClose;
            tradeData.ExitPrice = tradeData.LowAfterVolumeSurge;
            tradeData.PositionSize = 1000;
            tradeData.TradeType = "Analysis";
            tradeData.ProfitLoss = 0;

            return tradeData;
        }

        private class ChartData
        {
            public string Symbol { get; set; } = "";
            public DateTime Date { get; set; }
            public decimal HighAfterVolumeSurge { get; set; }
            public decimal LowAfterVolumeSurge { get; set; }
            public decimal LowBeforeVolumeSurge { get; set; }
            public int TotalVolume { get; set; }
        }







        private string GetStringValue(System.Text.Json.JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
        }

        private decimal GetDecimalValue(System.Text.Json.JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return 0m;
            
            if (prop.ValueKind == JsonValueKind.Null)
                return 0m;
                
            return prop.GetDecimal();
        }

        private int GetIntValue(System.Text.Json.JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetInt32() : 0;
        }

        private DateTime GetDateTimeValue(System.Text.Json.JsonElement element, string propertyName)
        {
            var dateString = GetStringValue(element, propertyName);
            return DateTime.TryParse(dateString, out var date) ? date : DateTime.Now;
        }
    }
} 