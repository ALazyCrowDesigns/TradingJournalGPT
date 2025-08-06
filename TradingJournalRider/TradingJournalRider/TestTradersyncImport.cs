using System;
using System.IO;
using System.Threading.Tasks;
using TradingJournalGPT.Services;

namespace TradingJournalGPT
{
    public class TestTradersyncImport
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Testing Tradersync Import Service...");
                
                var importService = new TradersyncImportService();
                var csvFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "TradersyncData", "trade_data.csv");
                
                if (!File.Exists(csvFilePath))
                {
                    Console.WriteLine($"CSV file not found at: {csvFilePath}");
                    return;
                }
                
                Console.WriteLine($"Found CSV file at: {csvFilePath}");
                Console.WriteLine("Importing trades...");
                
                var importedTrades = await importService.ImportTradesAsync(csvFilePath);
                
                Console.WriteLine($"Successfully imported {importedTrades.Count} trades");
                
                // Display first few trades as a sample
                for (int i = 0; i < Math.Min(5, importedTrades.Count); i++)
                {
                    var trade = importedTrades[i];
                    Console.WriteLine($"Trade {i + 1}: {trade.Symbol} - {trade.Date:MMM dd, yyyy} - {trade.TradeType} - Entry: ${trade.EntryPrice:F2} - Exit: ${trade.ExitPrice:F2} - P/L: ${trade.ProfitLoss:F2} ({trade.ProfitLossPercent:F2}%)");
                }
                
                Console.WriteLine("Test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
} 