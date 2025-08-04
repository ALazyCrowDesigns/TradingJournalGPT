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
            lblPreviousClose.Text = $"${_tradeData.PreviousDayClose:F2}";
            lblHighAfterVolumeSurge.Text = $"${_tradeData.HighAfterVolumeSurge:F2}";
            lblLowAfterVolumeSurge.Text = $"${_tradeData.LowAfterVolumeSurge:F2}";
            lblGapPercentToHigh.Text = $"{_tradeData.GapPercentToHigh:F1}%";
            lblGapPercentHighToLow.Text = $"{_tradeData.GapPercentHighToLow:F1}%";
            lblVolume.Text = $"{_tradeData.Volume / 1000000:F2}M";
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