using System;
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
                
                // Test with a sample CSV file path
                string csvFilePath = "tradersyncdata/sample_trades.csv";
                
                Console.WriteLine($"Attempting to import from: {csvFilePath}");
                
                var trades = await importService.ImportTradesAsync(csvFilePath);
                
                Console.WriteLine($"\nSuccessfully imported {trades.Count} trades:");
                foreach (var trade in trades)
                {
                    Console.WriteLine($"Symbol: {trade.Symbol}, Date: {trade.Date:MM/dd/yyyy}, Side: {trade.Status}, Entry: ${trade.EntryPrice}, Exit: ${trade.ExitPrice}, P/L: ${trade.ProfitLoss} ({trade.ProfitLossPercent:F2}%)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 