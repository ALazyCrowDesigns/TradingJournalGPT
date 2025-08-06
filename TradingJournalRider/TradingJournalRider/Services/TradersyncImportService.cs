using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TradingJournalGPT.Models;

namespace TradingJournalGPT.Services
{
    public class TradersyncTrade
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty; // WIN/LOSS/BREAKEVEN
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercent { get; set; }
        public string Side { get; set; } = string.Empty; // SHORT/LONG
    }

    public class TradersyncImportService
    {
        public async Task<List<TradeData>> ImportTradesAsync(string filePath)
        {
            var trades = new List<TradeData>();
            
            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                
                // Skip header line
                for (int i = 1; i < lines.Length; i++)
                {
                    var trade = ParseTradeLine(lines[i].Split(','));
                    if (trade != null)
                    {
                        var tradeData = ConvertToTradeData(trade);
                        trades.Add(tradeData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing trades: {ex.Message}");
            }
            
            return trades;
        }

        private TradersyncTrade? ParseTradeLine(string[] parts)
        {
            if (parts.Length < 13)
            {
                Console.WriteLine($"Invalid line format: {string.Join(",", parts)}");
                return null;
            }

            try
            {
                var status = parts[0].Trim().Replace("\"", ""); // Status (WIN/LOSS/BREAKEVEN)
                var symbol = parts[1].Trim().Replace("\"", ""); // Symbol
                var openDateStr = parts[3].Trim().Replace("\"", ""); // Open Date
                var entryPriceStr = parts[9].Trim().Replace("$", "").Replace(",", ""); // Entry Price
                var exitPriceStr = parts[10].Trim().Replace("$", "").Replace(",", ""); // Exit Price
                var returnDollarStr = parts[11].Trim().Replace("$", "").Replace("\"", "").Replace(",", ""); // Return $
                var returnPercentStr = parts[12].Trim().Replace("%", "").Replace("\"", "").Replace(",", ""); // Return %
                var side = parts.Length > 21 ? parts[21].Trim() : ""; // Side (SHORT/LONG)

                // Parse date
                if (!DateTime.TryParseExact(openDateStr, "MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tradeDate))
                {
                    Console.WriteLine($"Could not parse date: {openDateStr}");
                    return null;
                }

                // Parse prices
                if (!decimal.TryParse(entryPriceStr, out decimal entryPrice))
                {
                    Console.WriteLine($"Could not parse entry price: {entryPriceStr}");
                    return null;
                }

                if (!decimal.TryParse(exitPriceStr, out decimal exitPrice))
                {
                    Console.WriteLine($"Could not parse exit price: {exitPriceStr}");
                    return null;
                }

                // Parse profit/loss
                if (!decimal.TryParse(returnDollarStr, out decimal profitLoss))
                {
                    Console.WriteLine($"Could not parse profit/loss: {returnDollarStr}");
                    return null;
                }

                // Parse profit/loss percentage
                if (!decimal.TryParse(returnPercentStr, out decimal profitLossPercent))
                {
                    Console.WriteLine($"Could not parse profit/loss percentage: {returnPercentStr}");
                    return null;
                }

                // Use the Side column if available, otherwise use Status
                var displayStatus = !string.IsNullOrEmpty(side) ? side : status;

                return new TradersyncTrade
                {
                    Symbol = symbol,
                    Date = tradeDate,
                    Status = status, // Keep the original status (WIN/LOSS/BREAKEVEN)
                    EntryPrice = entryPrice,
                    ExitPrice = exitPrice,
                    ProfitLoss = profitLoss,
                    ProfitLossPercent = profitLossPercent,
                    Side = side
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
                EntryPrice = tradersyncTrade.EntryPrice,
                ExitPrice = tradersyncTrade.ExitPrice,
                EntryDate = tradersyncTrade.Date,
                ExitDate = tradersyncTrade.Date,
                ProfitLoss = tradersyncTrade.ProfitLoss,
                ProfitLossPercent = tradersyncTrade.ProfitLossPercent,
                TradeType = tradersyncTrade.Side, // Use the Side (SHORT/LONG) as trade type
                Analysis = $"Imported from Tradersync - {tradersyncTrade.Status}",
                RecordedDate = DateTime.Now,
                TradeSeq = 1 // Default sequence for imported trades
            };
        }
    }
} 