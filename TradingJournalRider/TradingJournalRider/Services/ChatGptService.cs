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

                // Single prompt: Extract chart data only
                var prompt = @"Look at this chart and extract ONLY the stock name, date, post volume surge high, and new low after that volume surge high.

                Return ONLY this JSON format with actual numbers (not null):
                {
                    ""symbol"": ""stock symbol"",
                    ""date"": ""YYYY-MM-DD"",
                    ""highAfterVolumeSurge"": 0.00,
                    ""lowAfterVolumeSurge"": 0.00
                }

                IMPORTANT: Use the high and low AFTER the volume surge, not the day's HOD/LOD. All numeric values must be actual numbers, not null.";

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

        public async Task<OnlineData> GetOnlineDataForTrade(string symbol, DateTime date)
        {
            try
            {
                var prompt = $@"You are a financial data expert. Get the exact previous day closing price and trading volume for {symbol} on {date:yyyy-MM-dd}.

                Use reliable financial sources like Yahoo Finance, MarketWatch, or similar to get:
                1. The previous trading day's closing price for {symbol}
                2. The total volume traded on {date:yyyy-MM-dd} for {symbol}

                Return ONLY this JSON format with actual market data (not placeholder values):
                {{
                    ""previousDayClose"": 0.00,
                    ""volume"": 0.00
                }}

                CRITICAL REQUIREMENTS:
                - previousDayClose: Must be the actual previous trading day's closing price (not 0.00)
                - volume: Must be the actual volume traded on {date:yyyy-MM-dd} in MILLIONS (e.g., 100.00 for 100M shares)
                - Use real market data, not estimates or placeholders
                - If data is not available, return the closest available data with a note
                - Format numbers as decimals (e.g., 15.75, 123.45)";

                // Call ChatGPT API without image (text-only)
                var response = await CallChatGptApiTextOnly(prompt);
                return ParseOnlineDataFromResponse(response);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting online data for {symbol} on {date:yyyy-MM-dd}: {ex.Message}");
            }
        }

        private async Task<string> CallChatGptApiTextOnly(string prompt)
        {
            var requestPayload = new
            {
                model = "gpt-4o",
                messages = new object[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
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
                    LowAfterVolumeSurge = GetDecimalValue(root, "lowAfterVolumeSurge")
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing chart data response: {ex.Message}");
            }
        }



        private OnlineData ParseOnlineDataFromResponse(string responseText)
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

                return new OnlineData
                {
                    PreviousDayClose = GetDecimalValue(root, "previousDayClose"),
                    Volume = GetDecimalValue(root, "volume")
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing online data response: {ex.Message}");
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
                PreviousDayClose = 0m, // Will be populated by Get Online Data
                GapPercentToHigh = 0m, // Will be calculated when previous day close is available
                GapPercentHighToLow = 0m, // Will be calculated when previous day close is available
                Volume = 0, // Will be populated by Get Online Data
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
        }

        public class OnlineData
        {
            public decimal PreviousDayClose { get; set; }
            public decimal Volume { get; set; }
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