using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TradingJournalGPT.Models;

namespace TradingJournalGPT.Services
{
    public class TradersyncImportService
    {
        public class TradersyncTrade
        {
            public string Status { get; set; } = string.Empty; // SHORT/LONG from Tradersync
            public string Symbol { get; set; } = string.Empty;
            public DateTime Date { get; set; }
            public decimal EntryPrice { get; set; }
            public decimal ExitPrice { get; set; }
            public decimal ProfitLoss { get; set; }
            public decimal ProfitLossPercent { get; set; }
        }

        public async Task<List<TradeData>> ImportTradesAsync(string csvFilePath)
        {
            var trades = new List<TradeData>();
            
            try
            {
                if (!File.Exists(csvFilePath))
                {
                    throw new FileNotFoundException($"Tradersync CSV file not found: {csvFilePath}");
                }

                var lines = await File.ReadAllLinesAsync(csvFilePath);
                
                // Skip header line
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var parts = line.Split(',');
                    var trade = ParseTradeLine(parts);
                    
                    if (trade != null)
                    {
                        var tradeData = ConvertToTradeData(trade);
                        trades.Add(tradeData);
                    }
                }

                Console.WriteLine($"Successfully imported {trades.Count} trades from Tradersync CSV");
                return trades;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing Tradersync data: {ex.Message}");
                throw;
            }
        }

        private TradersyncTrade? ParseTradeLine(string[] parts)
        {
            try
            {
                // CSV columns: Status,Symbol,Size,Open Date,Close Date,Open Time,Close Time,Setups,Mistakes,Entry Price,Exit Price,Return $,Return %,Avg Buy,Avg Sell,Net Return,Commision,Notes,Expire,Strike,Type,Side,Spread,Cost,Executions,Fees,Swap,Holdtime,Last Order,Portfolio,Position,Privacy,Return Share,Risk,MAE,MFE,Expectancy,R-Multiple,Best Exit $,Best Exit %
                
                var winLossStatus = parts[0].Trim(); // WIN/LOSS/OPEN
                var symbol = parts[1].Trim();
                var openDateStr = parts[3].Trim().Replace("\"", ""); // Open Date
                var entryPriceStr = parts[9].Trim().Replace("$", ""); // Entry Price
                var exitPriceStr = parts[10].Trim().Replace("$", ""); // Exit Price
                var returnDollarStr = parts[11].Trim().Replace("$", "").Replace("\"", ""); // Return $
                var returnPercentStr = parts[12].Trim().Replace("%", "").Replace("\"", ""); // Return %
                var side = parts.Length > 21 ? parts[21].Trim() : ""; // Side (SHORT/LONG)

                // Parse date
                if (!DateTime.TryParseExact(openDateStr, "MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tradeDate))
                {
                    Console.WriteLine($"Could not parse date: {openDateStr}");
                    return null;
                }

                // Parse prices
                if (!decimal.TryParse(entryPriceStr.Replace(",", ""), out decimal entryPrice))
                {
                    Console.WriteLine($"Could not parse entry price: {entryPriceStr}");
                    return null;
                }

                if (!decimal.TryParse(exitPriceStr.Replace(",", ""), out decimal exitPrice))
                {
                    Console.WriteLine($"Could not parse exit price: {exitPriceStr}");
                    return null;
                }

                // Parse profit/loss
                if (!decimal.TryParse(returnDollarStr.Replace(",", ""), out decimal profitLoss))
                {
                    profitLoss = 0;
                }

                if (!decimal.TryParse(returnPercentStr.Replace(",", ""), out decimal profitLossPercent))
                {
                    profitLossPercent = 0;
                }

                // Use SHORT/LONG from Side column if available, otherwise use WIN/LOSS status
                var displayStatus = !string.IsNullOrEmpty(side) ? side : winLossStatus;

                return new TradersyncTrade
                {
                    Status = displayStatus, // Use SHORT/LONG instead of WIN/LOSS
                    Symbol = symbol.ToUpper(),
                    Date = tradeDate,
                    EntryPrice = entryPrice,
                    ExitPrice = exitPrice,
                    ProfitLoss = profitLoss,
                    ProfitLossPercent = profitLossPercent
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing trade line: {ex.Message}");
                return null;
            }
        }

        private TradeData ConvertToTradeData(TradersyncTrade tradersyncTrade)
        {
            return new TradeData
            {
                Symbol = tradersyncTrade.Symbol,
                Date = tradersyncTrade.Date,
                Status = tradersyncTrade.Status, // SHORT/LONG
                EntryPrice = tradersyncTrade.EntryPrice,
                ExitPrice = tradersyncTrade.ExitPrice,
                ProfitLoss = tradersyncTrade.ProfitLoss,
                ProfitLossPercent = tradersyncTrade.ProfitLossPercent,
                TradeSeq = 1, // Default sequence number
                RecordedDate = DateTime.Now
            };
        }

        public async Task TestTradersyncImport(string csvFilePath)
        {
            try
            {
                Console.WriteLine("Testing Tradersync import...");
                var trades = await ImportTradesAsync(csvFilePath);
                
                Console.WriteLine($"\nImported {trades.Count} trades:");
                foreach (var trade in trades.Take(5)) // Show first 5 trades
                {
                    Console.WriteLine($"Symbol: {trade.Symbol}, Date: {trade.Date:MM/dd/yyyy}, Side: {trade.Status}, Entry: ${trade.EntryPrice}, Exit: ${trade.ExitPrice}, P/L: ${trade.ProfitLoss} ({trade.ProfitLossPercent:F2}%)");
                }
                
                if (trades.Count > 5)
                {
                    Console.WriteLine($"... and {trades.Count - 5} more trades");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
            }
        }
    }
} 