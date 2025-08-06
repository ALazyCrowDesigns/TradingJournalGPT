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
                Console.WriteLine($"Total lines in file: {lines.Length}");
                
                // Skip header line
                for (int i = 1; i < lines.Length; i++)
                {
                    Console.WriteLine($"Processing line {i}: {lines[i].Substring(0, Math.Min(100, lines[i].Length))}");
                    var parts = ParseCsvLine(lines[i]);
                    var trade = ParseTradeLine(parts);
                    if (trade != null)
                    {
                        Console.WriteLine($"Successfully parsed trade: {trade.Symbol} - {trade.Side} - {trade.EntryPrice} -> {trade.ExitPrice}");
                        var tradeData = ConvertToTradeData(trade);
                        trades.Add(tradeData);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to parse line {i}");
                    }
                }
                
                Console.WriteLine($"Total trades imported: {trades.Count}");
                
                // Merge trades with same symbol, side, and date
                var mergedTrades = MergeTradesBySymbolSideAndDate(trades);
                Console.WriteLine($"After merging: {mergedTrades.Count} trades");
                
                return mergedTrades;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing trades: {ex.Message}");
            }
            
            return trades;
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            var inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.Trim());
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            
            result.Add(current.Trim());
            return result.ToArray();
        }

        private TradersyncTrade? ParseTradeLine(string[] parts)
        {
            if (parts.Length < 13)
            {
                Console.WriteLine($"Invalid line format - not enough columns: {parts.Length} columns, need at least 13");
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

                Console.WriteLine($"Parsed values: Status={status}, Symbol={symbol}, Date={openDateStr}, Entry={entryPriceStr}, Exit={exitPriceStr}, Return$={returnDollarStr}, Return%={returnPercentStr}, Side={side}");

                // Skip OPEN trades that don't have exit prices
                if (status == "OPEN")
                {
                    Console.WriteLine($"Skipping OPEN trade for {symbol}");
                    return null;
                }

                // Parse date
                if (!DateTime.TryParseExact(openDateStr, "MMM dd, yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime tradeDate))
                {
                    Console.WriteLine($"Could not parse date: {openDateStr}");
                    return null;
                }

                // Parse prices - handle empty exit prices
                if (!decimal.TryParse(entryPriceStr, out decimal entryPrice))
                {
                    Console.WriteLine($"Could not parse entry price: {entryPriceStr}");
                    return null;
                }

                // Handle empty exit prices (some trades might not have closed yet)
                decimal exitPrice = 0;
                if (!string.IsNullOrEmpty(exitPriceStr) && decimal.TryParse(exitPriceStr, out exitPrice))
                {
                    // Exit price parsed successfully
                }
                else
                {
                    Console.WriteLine($"Could not parse exit price: {exitPriceStr}, using 0");
                    exitPrice = 0;
                }

                // Parse profit/loss - handle empty values
                decimal profitLoss = 0;
                if (!string.IsNullOrEmpty(returnDollarStr) && decimal.TryParse(returnDollarStr, out profitLoss))
                {
                    // Profit/loss parsed successfully
                }
                else
                {
                    Console.WriteLine($"Could not parse profit/loss: {returnDollarStr}, using 0");
                    profitLoss = 0;
                }

                // Parse profit/loss percentage - handle empty values
                decimal profitLossPercent = 0;
                if (!string.IsNullOrEmpty(returnPercentStr) && decimal.TryParse(returnPercentStr, out profitLossPercent))
                {
                    // Profit/loss percentage parsed successfully
                }
                else
                {
                    Console.WriteLine($"Could not parse profit/loss percentage: {returnPercentStr}, using 0");
                    profitLossPercent = 0;
                }

                // Use the Side column if available, otherwise use Status
                var displayStatus = !string.IsNullOrEmpty(side) ? side : status;

                Console.WriteLine($"Successfully parsed all values for {symbol}");

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

        private List<TradeData> MergeTradesBySymbolSideAndDate(List<TradeData> trades)
        {
            var mergedTrades = new List<TradeData>();
            var tradeGroups = trades.GroupBy(t => new { t.Symbol, t.TradeType, t.Date.Date }).ToList();
            
            Console.WriteLine($"Found {tradeGroups.Count} unique symbol/side/date combinations");
            
            foreach (var group in tradeGroups)
            {
                if (group.Count() == 1)
                {
                    // Single trade, no merging needed
                    mergedTrades.Add(group.First());
                    Console.WriteLine($"Single trade: {group.Key.Symbol} {group.Key.TradeType} on {group.Key.Date:MM/dd/yyyy}");
                }
                else
                {
                    // Multiple trades to merge
                    var mergedTrade = MergeTradeGroup(group.ToList());
                    mergedTrades.Add(mergedTrade);
                    Console.WriteLine($"Merged {group.Count()} trades: {group.Key.Symbol} {group.Key.TradeType} on {group.Key.Date:MM/dd/yyyy}");
                }
            }
            
            return mergedTrades;
        }

        private TradeData MergeTradeGroup(List<TradeData> trades)
        {
            if (trades.Count == 0) return null;
            if (trades.Count == 1) return trades[0];
            
            var firstTrade = trades[0];
            var totalEntryValue = 0m;
            var totalExitValue = 0m;
            var totalShares = 0m;
            var totalProfitLoss = 0m;
            var totalProfitLossPercent = 0m;
            var entryPrices = new List<decimal>();
            var exitPrices = new List<decimal>();
            
            foreach (var trade in trades)
            {
                // Calculate weighted averages for prices
                if (trade.EntryPrice > 0)
                {
                    entryPrices.Add(trade.EntryPrice);
                    totalEntryValue += trade.EntryPrice;
                }
                if (trade.ExitPrice > 0)
                {
                    exitPrices.Add(trade.ExitPrice);
                    totalExitValue += trade.ExitPrice;
                }
                
                totalProfitLoss += trade.ProfitLoss;
                totalProfitLossPercent += trade.ProfitLossPercent;
                totalShares += 1; // Assuming each trade represents 1 share/contract
            }
            
            // Calculate weighted average prices
            var avgEntryPrice = entryPrices.Count > 0 ? totalEntryValue / entryPrices.Count : 0;
            var avgExitPrice = exitPrices.Count > 0 ? totalExitValue / exitPrices.Count : 0;
            
            // Create merged trade
            var mergedTrade = new TradeData
            {
                Symbol = firstTrade.Symbol,
                Date = firstTrade.Date,
                EntryPrice = avgEntryPrice,
                ExitPrice = avgExitPrice,
                EntryDate = firstTrade.EntryDate,
                ExitDate = firstTrade.ExitDate,
                ProfitLoss = totalProfitLoss,
                ProfitLossPercent = totalProfitLossPercent,
                TradeType = firstTrade.TradeType,
                Analysis = $"Merged {trades.Count} Tradersync trades - {firstTrade.Analysis}",
                RecordedDate = DateTime.Now,
                TradeSeq = 1
            };
            
            Console.WriteLine($"Merged trade: {mergedTrade.Symbol} {mergedTrade.TradeType} - Avg Entry: {avgEntryPrice:F2}, Avg Exit: {avgExitPrice:F2}, Total P/L: {totalProfitLoss:F2}");
            
            return mergedTrade;
        }
    }
} 