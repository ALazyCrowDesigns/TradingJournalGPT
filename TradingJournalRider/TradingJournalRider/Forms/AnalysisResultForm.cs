using System;
using System.Drawing;
using System.Windows.Forms;
using TradingJournalGPT.Models;

namespace TradingJournalGPT.Forms
{
    public partial class AnalysisResultForm : Form
    {
        public bool SaveLocally { get; private set; }
        private readonly TradeData _tradeData;

        public AnalysisResultForm(TradeData tradeData)
        {
            InitializeComponent();
            _tradeData = tradeData;
            DisplayTradeData();
        }

        private void DisplayTradeData()
        {
            lblSymbol.Text = _tradeData.Symbol;
            lblDate.Text = _tradeData.Date.ToString("yyyy-MM-dd");
            
            // Display previous close with appropriate message if not available
            if (_tradeData.PreviousDayClose > 0)
            {
                lblPreviousClose.Text = $"${_tradeData.PreviousDayClose:F2}";
            }
            else
            {
                lblPreviousClose.Text = "Use 'Get Online Data' to populate";
                lblPreviousClose.ForeColor = Color.Gray;
            }
            
            lblHighAfterVolumeSurge.Text = $"${_tradeData.HighAfterVolumeSurge:F2}";
            lblLowAfterVolumeSurge.Text = $"${_tradeData.LowAfterVolumeSurge:F2}";
            
            // Display gap percentages with appropriate message if not available
            if (_tradeData.GapPercentToHigh > 0)
            {
                lblGapPercentToHigh.Text = $"{_tradeData.GapPercentToHigh:F1}%";
            }
            else
            {
                lblGapPercentToHigh.Text = "Calculate after getting online data";
                lblGapPercentToHigh.ForeColor = Color.Gray;
            }
            
            if (_tradeData.GapPercentHighToLow > 0)
            {
                lblGapPercentHighToLow.Text = $"{_tradeData.GapPercentHighToLow:F1}%";
            }
            else
            {
                lblGapPercentHighToLow.Text = "Calculate after getting online data";
                lblGapPercentHighToLow.ForeColor = Color.Gray;
            }
            
            // Display volume with appropriate message if not available
            if (_tradeData.Volume > 0)
            {
                lblVolume.Text = $"{_tradeData.Volume / 1000000:F2}M";
            }
            else
            {
                lblVolume.Text = "Use 'Get Online Data' to populate";
                lblVolume.ForeColor = Color.Gray;
            }
        }

        private void btnSaveToGoogleSheets_Click(object? sender, EventArgs e)
        {
            SaveLocally = true; // "Local" button saves locally
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnSaveLocally_Click(object? sender, EventArgs e)
        {
            SaveLocally = false; // "Upload" button saves to Google Sheets
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
} 