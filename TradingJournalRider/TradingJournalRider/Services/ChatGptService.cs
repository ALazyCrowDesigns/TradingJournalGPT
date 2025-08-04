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

                // Create the prompt for chart analysis
                                       var prompt = @"Don't use online data, look at this chart, give me the date, the name of the stock, the High after volume surge, the low after volume surge, and the previous day close, then give me the %gap of yesterday's close to the high after volume surge, and the high to the new low. Also extract the volume traded for the day in millions of shares (if it shows 10 million shares traded, return 10).

                Please return this information in JSON format:
                {
                    ""symbol"": ""stock symbol"",
                    ""date"": ""YYYY-MM-DD"",
                    ""highAfterVolumeSurge"": decimal,
                    ""lowAfterVolumeSurge"": decimal,
                                           ""previousDayClose"": decimal,
                       ""gapPercentToHigh"": decimal,
                       ""gapPercentHighToLow"": decimal,
                       ""volume"": decimal
                }

                Focus on:
                - Stock symbol from the chart
                - Date shown on the chart
                - High price after volume surge
                - Low price after volume surge  
                - Previous day's closing price
                                   - Calculate % gap from yesterday's close to high after volume surge
                   - Calculate % gap from high to new low
                   - Extract volume traded for the day in millions of shares

                Return only the JSON object, no additional text.";

                // Create the request payload for OpenAI API
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

                // Serialize the request
                var jsonRequest = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                // Call the OpenAI API
                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"OpenAI API error: {response.StatusCode} - {responseText}");
                }

                // Parse the response
                using var responseDoc = JsonDocument.Parse(responseText);
                var choices = responseDoc.RootElement.GetProperty("choices");
                var firstChoice = choices[0];
                var message = firstChoice.GetProperty("message");
                var contentText = message.GetProperty("content").GetString() ?? "";

                // Parse the JSON response
                return ParseTradeDataFromResponse(contentText);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error analyzing chart image: {ex.Message}");
            }
        }

        private TradeData ParseTradeDataFromResponse(string responseText)
        {
            try
            {
                // Clean up the response text to extract JSON
                var jsonStart = responseText.IndexOf('{');
                var jsonEnd = responseText.LastIndexOf('}') + 1;
                
                if (jsonStart == -1 || jsonEnd == 0)
                {
                    throw new Exception("Could not find JSON in response");
                }

                var jsonText = responseText.Substring(jsonStart, jsonEnd - jsonStart);
                
                // Parse JSON using System.Text.Json
                using var document = System.Text.Json.JsonDocument.Parse(jsonText);
                var root = document.RootElement;

                var tradeData = new TradeData
                {
                    Symbol = GetStringValue(root, "symbol"),
                    Date = GetDateTimeValue(root, "date"),
                    HighAfterVolumeSurge = GetDecimalValue(root, "highAfterVolumeSurge"),
                    LowAfterVolumeSurge = GetDecimalValue(root, "lowAfterVolumeSurge"),
                    PreviousDayClose = GetDecimalValue(root, "previousDayClose"),
                    GapPercentToHigh = GetDecimalValue(root, "gapPercentToHigh"),
                    GapPercentHighToLow = Math.Abs(GetDecimalValue(root, "gapPercentHighToLow")),
                    Volume = (long)(GetDecimalValue(root, "volume") * 1000000), // Convert millions to actual volume
                    Analysis = "" // No longer needed
                };

                // Set legacy fields for backward compatibility
                tradeData.EntryDate = tradeData.Date;
                tradeData.ExitDate = tradeData.Date;
                tradeData.EntryPrice = tradeData.PreviousDayClose;
                tradeData.ExitPrice = tradeData.LowAfterVolumeSurge;
                tradeData.PositionSize = 1000; // Default position size
                tradeData.TradeType = "Analysis";
                tradeData.ProfitLoss = 0; // Not applicable for this analysis
                


                return tradeData;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error parsing ChatGPT response: {ex.Message}");
            }
        }

        private string GetStringValue(System.Text.Json.JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
        }

        private decimal GetDecimalValue(System.Text.Json.JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetDecimal() : 0m;
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