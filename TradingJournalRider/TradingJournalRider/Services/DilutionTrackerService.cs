using System.Text.RegularExpressions;
using TradingJournalGPT.Models;

namespace TradingJournalGPT.Services
{
    public class DilutionTrackerService
    {
        private readonly HttpClient _httpClient;

        public DilutionTrackerService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<decimal> GetFloatData(string symbol)
        {
            try
            {
                Console.WriteLine($"Fetching float data for {symbol} from Dilution Tracker...");
                
                // Construct the URL for the symbol
                var url = $"https://dilutiontracker.com/stock/{symbol.ToUpper()}";
                
                // Fetch the webpage
                var response = await _httpClient.GetStringAsync(url);
                
                // Look for float data in the HTML
                var floatValue = ExtractFloatFromHtml(response, symbol);
                
                if (floatValue > 0)
                {
                    Console.WriteLine($"Found float data for {symbol}: {floatValue:N0}");
                    return floatValue;
                }
                else
                {
                    Console.WriteLine($"No float data found for {symbol}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching float data for {symbol}: {ex.Message}");
                return 0;
            }
        }

        private decimal ExtractFloatFromHtml(string html, string symbol)
        {
            try
            {
                // Look for common patterns where float data might appear
                var patterns = new[]
                {
                    $@"Float[^>]*>([0-9,\.]+)M", // "Float" followed by numbers with M suffix
                    $@"Float[^>]*>([0-9,\.]+)", // "Float" followed by numbers
                    $@"float[^>]*>([0-9,\.]+)M", // "float" followed by numbers with M suffix
                    $@"float[^>]*>([0-9,\.]+)", // "float" followed by numbers
                    $@"shares[^>]*>([0-9,]+)", // "shares" followed by numbers
                    $@"outstanding[^>]*>([0-9,]+)", // "outstanding" followed by numbers
                    $@"{symbol}[^>]*float[^>]*>([0-9,]+)", // symbol + float + numbers
                    $@"float[^>]*{symbol}[^>]*>([0-9,]+)" // float + symbol + numbers
                };

                foreach (var pattern in patterns)
                {
                    var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var valueStr = match.Groups[1].Value.Replace(",", "");
                        if (decimal.TryParse(valueStr, out var value))
                        {
                            // If the pattern included "M", multiply by 1,000,000
                            if (match.Value.Contains("M"))
                            {
                                return value * 1000000;
                            }
                            return value;
                        }
                    }
                }

                // If no specific patterns found, look for any large numbers that might be float
                var numberMatches = Regex.Matches(html, @"([0-9,]{6,})");
                foreach (Match match in numberMatches)
                {
                    var valueStr = match.Groups[1].Value.Replace(",", "");
                    if (decimal.TryParse(valueStr, out var value) && value > 1000000) // Likely a float if > 1M
                    {
                        return value;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting float from HTML: {ex.Message}");
                return 0;
            }
        }
    }
} 