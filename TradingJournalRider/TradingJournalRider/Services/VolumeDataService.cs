using System.Text.RegularExpressions;

namespace TradingJournalGPT.Services
{
    public class VolumeDataService
    {
        private readonly HttpClient _httpClient;

        public VolumeDataService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<long> GetVolumeData(string symbol, DateTime date)
        {
            try
            {
                Console.WriteLine($"Fetching volume data for {symbol} on {date:yyyy-MM-dd}...");
                
                // Try multiple sources for volume data
                var volume = await TryYahooFinance(symbol, date);
                if (volume > 0) return volume;
                
                volume = await TryMarketWatch(symbol, date);
                if (volume > 0) return volume;
                
                volume = await TryFinviz(symbol, date);
                if (volume > 0) return volume;
                
                Console.WriteLine($"No volume data found for {symbol}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching volume data for {symbol}: {ex.Message}");
                return 0;
            }
        }

        private async Task<long> TryYahooFinance(string symbol, DateTime date)
        {
            try
            {
                var url = $"https://finance.yahoo.com/quote/{symbol.ToUpper()}/history";
                var response = await _httpClient.GetStringAsync(url);
                
                // Look for volume data in the response
                var volumeMatch = Regex.Match(response, $@"{date:MM/dd/yyyy}[^>]*>([0-9,]+)", RegexOptions.IgnoreCase);
                if (volumeMatch.Success)
                {
                    var volumeStr = volumeMatch.Groups[1].Value.Replace(",", "");
                    if (long.TryParse(volumeStr, out var volume))
                    {
                        Console.WriteLine($"Found volume data from Yahoo Finance: {volume:N0}");
                        return volume;
                    }
                }
                
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<long> TryMarketWatch(string symbol, DateTime date)
        {
            try
            {
                var url = $"https://www.marketwatch.com/investing/stock/{symbol.ToLower()}";
                var response = await _httpClient.GetStringAsync(url);
                
                // Look for volume data
                var volumeMatch = Regex.Match(response, @"volume[^>]*>([0-9,]+)", RegexOptions.IgnoreCase);
                if (volumeMatch.Success)
                {
                    var volumeStr = volumeMatch.Groups[1].Value.Replace(",", "");
                    if (long.TryParse(volumeStr, out var volume))
                    {
                        Console.WriteLine($"Found volume data from MarketWatch: {volume:N0}");
                        return volume;
                    }
                }
                
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<long> TryFinviz(string symbol, DateTime date)
        {
            try
            {
                var url = $"https://finviz.com/quote.ashx?t={symbol.ToUpper()}";
                var response = await _httpClient.GetStringAsync(url);
                
                // Look for volume data
                var volumeMatch = Regex.Match(response, @"Volume[^>]*>([0-9,]+)", RegexOptions.IgnoreCase);
                if (volumeMatch.Success)
                {
                    var volumeStr = volumeMatch.Groups[1].Value.Replace(",", "");
                    if (long.TryParse(volumeStr, out var volume))
                    {
                        Console.WriteLine($"Found volume data from Finviz: {volume:N0}");
                        return volume;
                    }
                }
                
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
} 