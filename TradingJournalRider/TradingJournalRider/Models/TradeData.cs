namespace TradingJournalGPT.Models
{
    public class TradeData
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal HighAfterVolumeSurge { get; set; }
        public decimal LowAfterVolumeSurge { get; set; }
        public decimal PreviousDayClose { get; set; }
        public decimal GapPercentToHigh { get; set; }
        public decimal GapPercentHighToLow { get; set; }
        public string Analysis { get; set; } = string.Empty;
        
        // Legacy fields for backward compatibility
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime ExitDate { get; set; }
        public int PositionSize { get; set; }
        public string TradeType { get; set; } = string.Empty;
        public decimal ProfitLoss { get; set; }
        public long Volume { get; set; }
        public DateTime RecordedDate { get; set; } = DateTime.Now;
        
        // Additional fields for future expansion
        public string ChartImagePath { get; set; } = string.Empty;
        public int TradeSeq { get; set; } = 0; // Trade sequence number for the same symbol/date
        public string Notes { get; set; } = string.Empty;
        public string ScreenshotPath { get; set; } = string.Empty;
    }
} 