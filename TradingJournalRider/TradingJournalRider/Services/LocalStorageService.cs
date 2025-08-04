using TradingJournalGPT.Models;
using System.Text.Json;

namespace TradingJournalGPT.Services
{
    public class LocalStorageService
    {
        private readonly string _dataDirectory;
        private readonly string _tradesFile;

        public LocalStorageService()
        {
            // Store data in a subfolder of the application directory
            var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            _dataDirectory = Path.Combine(appDirectory, "Data");
            _tradesFile = Path.Combine(_dataDirectory, "trades.json");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        public async Task RecordTrade(TradeData tradeData)
        {
            try
            {
                Console.WriteLine($"LocalStorageService.RecordTrade called for: {tradeData.Symbol} on {tradeData.Date:yyyy-MM-dd} with TradeSeq: {tradeData.TradeSeq}");
                var trades = await LoadTrades();
                Console.WriteLine($"Current trades in storage: {trades.Count}");
                trades.Add(tradeData);
                Console.WriteLine($"Added trade, now have: {trades.Count} trades");
                await SaveTrades(trades);
                Console.WriteLine("Trade saved to local storage successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LocalStorageService.RecordTrade: {ex.Message}");
                throw new Exception($"Error recording trade locally: {ex.Message}");
            }
        }

        public async Task<List<TradeData>> GetRecentTrades(int limit = 10)
        {
            try
            {
                var trades = await LoadTrades();
                return trades.OrderByDescending(t => t.RecordedDate).Take(limit).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving local trades: {ex.Message}");
            }
        }

        private async Task<List<TradeData>> LoadTrades()
        {
            if (!File.Exists(_tradesFile))
            {
                return new List<TradeData>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_tradesFile);
                if (string.IsNullOrEmpty(json))
                {
                    return new List<TradeData>();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<List<TradeData>>(json, options) ?? new List<TradeData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load existing trades file: {ex.Message}");
                return new List<TradeData>();
            }
        }

        public async Task SaveTrades(List<TradeData> trades)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(trades, options);
                await File.WriteAllTextAsync(_tradesFile, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving trades: {ex.Message}");
            }
        }

        public string GetDataLocation()
        {
            return _dataDirectory;
        }
    }
} 