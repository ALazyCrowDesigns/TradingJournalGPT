using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TradingJournalGPT.Models;
using TradingJournalGPT.Services;

namespace TradingJournalGPT.Forms
{
    public partial class MainForm : Form
    {
        private readonly TradingJournalService _tradingJournalService;
        private readonly LocalStorageService _localStorageService;
        private DataTable _tradesDataTable = new DataTable();
        private bool _isProcessing = false;
        private ContextMenuStrip _contextMenu = null!;
        
        // Enhanced Undo/Redo system with temporary state
        private readonly Stack<UndoRedoAction> _undoStack = new Stack<UndoRedoAction>();
        private readonly Stack<UndoRedoAction> _redoStack = new Stack<UndoRedoAction>();
        private bool _isUndoRedoAction = false;
        
        // Temporary state management
        private List<TradeData> _temporaryTrades = new List<TradeData>();
        private List<TradeData> _deletedTrades = new List<TradeData>(); // Track deleted trades for image cleanup
        private bool _hasUnsavedChanges = false;
        private Button _btnSaveChanges = null!;
        private Label _lblUnsavedChanges = null!;

        public MainForm()
        {
            InitializeComponent();
            _tradingJournalService = new TradingJournalService();
            _localStorageService = new LocalStorageService();
            InitializeDataTable();
            InitializeTemporaryState();
            
            // Load initial data from storage
            _ = Task.Run(async () => 
            {
                await Task.Delay(100); // Small delay to ensure UI is ready
                this.Invoke(() => LoadRecentTrades());
            });
        }

        private void InitializeDataTable()
        {
            _tradesDataTable = new DataTable();
            _tradesDataTable.Columns.Add("Symbol", typeof(string));
            _tradesDataTable.Columns.Add("Date", typeof(DateTime));
            _tradesDataTable.Columns.Add("Trade Seq", typeof(int));
            _tradesDataTable.Columns.Add("Previous Close", typeof(decimal));
            _tradesDataTable.Columns.Add("High After Volume Surge", typeof(decimal));
            _tradesDataTable.Columns.Add("Low After Volume Surge", typeof(decimal));
            _tradesDataTable.Columns.Add("Gap % (Close to High)", typeof(decimal));
            _tradesDataTable.Columns.Add("Gap % (High to Low)", typeof(decimal));
            _tradesDataTable.Columns.Add("Volume (M)", typeof(decimal));
            _tradesDataTable.Columns.Add("Chart", typeof(string));

            dataGridViewTrades.DataSource = _tradesDataTable;
            dataGridViewTrades.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dataGridViewTrades.AllowUserToAddRows = false;
            dataGridViewTrades.ReadOnly = false; // Allow editing
            dataGridViewTrades.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewTrades.MultiSelect = false;
            
            // Set specific columns as read-only
            dataGridViewTrades.Columns["Symbol"].ReadOnly = true;
            dataGridViewTrades.Columns["Date"].ReadOnly = true;
            dataGridViewTrades.Columns["Trade Seq"].ReadOnly = true;
            dataGridViewTrades.Columns["Chart"].ReadOnly = true;
            
            // Configure Chart column for image display
            var chartColumn = dataGridViewTrades.Columns["Chart"];
            chartColumn.Width = 100;
            chartColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            // Add event handlers
            dataGridViewTrades.CellEndEdit += DataGridViewTrades_CellEndEdit;
            dataGridViewTrades.KeyDown += DataGridViewTrades_KeyDown;
            dataGridViewTrades.MouseClick += DataGridViewTrades_MouseClick;
            dataGridViewTrades.CellFormatting += DataGridViewTrades_CellFormatting;
            dataGridViewTrades.DataError += DataGridViewTrades_DataError;
            
            // Create context menu for delete functionality
            CreateContextMenu();
        }

        private async void LoadRecentTrades()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                
                // Always load from storage to get the latest data
                var storageTrades = await _tradingJournalService.GetRecentTrades(50, true);
                
                // If we have unsaved changes, merge with temporary state
                if (_hasUnsavedChanges)
                {
                    // Keep temporary trades and merge with new storage trades
                    var mergedTrades = new List<TradeData>();
                    
                    // Add all storage trades that aren't in deleted list
                    foreach (var storageTrade in storageTrades)
                    {
                        var isDeleted = _deletedTrades.Any(d => 
                            d.Symbol == storageTrade.Symbol && 
                            d.Date.Date == storageTrade.Date.Date &&
                            d.TradeSeq == storageTrade.TradeSeq);
                        
                        if (!isDeleted)
                        {
                            mergedTrades.Add(storageTrade);
                        }
                    }
                    
                    // Add temporary trades (which include edits)
                    mergedTrades.AddRange(_temporaryTrades);
                    
                    // Update temporary trades with merged result
                    _temporaryTrades = mergedTrades;
                }
                else
                {
                    // No unsaved changes, just use storage data
                    _temporaryTrades = storageTrades;
                }
                
                _tradesDataTable.Clear();
                foreach (var trade in _temporaryTrades)
                {
                    _tradesDataTable.Rows.Add(
                        trade.Symbol,
                        trade.Date,
                        trade.TradeSeq,
                        trade.PreviousDayClose,
                        trade.HighAfterVolumeSurge,
                        trade.LowAfterVolumeSurge,
                        trade.GapPercentToHigh,
                        trade.GapPercentHighToLow,
                        trade.Volume / 1000000m,
                        trade.ChartImagePath ?? "No Image"
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading trades: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private async void btnAnalyzeChartImage_Click(object? sender, EventArgs e)
        {
            if (_isProcessing) 
            {
                MessageBox.Show("Already processing an image. Please wait for the current analysis to complete.", "Processing", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Console.WriteLine("Analyze Chart Image button clicked - starting analysis");
            
            // Set processing flag immediately to prevent multiple clicks
            _isProcessing = true;
            btnAnalyzeChartImage.Enabled = false;
            btnAnalyzeChartFolder.Enabled = false;
            
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select Chart Image";
                    openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        Console.WriteLine($"Selected image: {openFileDialog.FileName}");
                        await ProcessSingleImage(openFileDialog.FileName);
                    }
                    else
                    {
                        Console.WriteLine("No image selected - user cancelled");
                    }
                }
            }
            finally
            {
                // Reset processing flag if user cancels file dialog
                _isProcessing = false;
                btnAnalyzeChartImage.Enabled = true;
                btnAnalyzeChartFolder.Enabled = true;
            }
        }

        private async void btnAnalyzeChartFolder_Click(object? sender, EventArgs e)
        {
            if (_isProcessing) return;

            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select folder containing chart images";
                folderBrowserDialog.ShowNewFolderButton = false;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    await ProcessFolder(folderBrowserDialog.SelectedPath);
                }
            }
        }

        private async Task ProcessSingleImage(string imagePath)
        {
            Console.WriteLine($"ProcessSingleImage started for: {imagePath}");
            // _isProcessing is already set to true in btnAnalyzeChartImage_Click
            // Buttons are already disabled in btnAnalyzeChartImage_Click
            progressBar.Visible = true;
            lblStatus.Text = "Analyzing chart image...";

            try
            {
                Cursor = Cursors.WaitCursor;
                Console.WriteLine("Calling ChatGPT analysis...");
                var tradeData = await _tradingJournalService.AnalyzeChartImage(imagePath);
                Console.WriteLine($"ChatGPT analysis completed. Symbol: {tradeData.Symbol}, Date: {tradeData.Date}, TradeSeq: {tradeData.TradeSeq}");

                // Show results dialog
                Console.WriteLine("Showing AnalysisResultForm dialog...");
                using (var resultForm = new AnalysisResultForm(tradeData))
                {
                    var dialogResult = resultForm.ShowDialog();
                    Console.WriteLine($"Dialog result: {dialogResult}");
                    
                    if (dialogResult == DialogResult.OK)
                    {
                        Console.WriteLine($"Recording trade with SaveLocally: {resultForm.SaveLocally}");
                        
                        if (resultForm.SaveLocally)
                        {
                            // Save to local storage
                            await _tradingJournalService.RecordTrade(tradeData, true);
                            
                            // Add to temporary state for immediate display
                            _temporaryTrades.Add(tradeData);
                            
                            Console.WriteLine("Trade recorded to local storage and added to temporary state");
                        }
                        else
                        {
                            // Save to Google Sheets
                            await _tradingJournalService.RecordTrade(tradeData, false);
                            
                            // Add to temporary state for immediate display
                            _temporaryTrades.Add(tradeData);
                            
                            Console.WriteLine("Trade recorded to Google Sheets and added to temporary state");
                        }
                        
                        Console.WriteLine("Trade recorded, refreshing data table...");
                        // Refresh the data table
                        LoadRecentTrades();
                        
                        MessageBox.Show("Trade recorded successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Console.WriteLine("ProcessSingleImage completed successfully");
                    }
                    else
                    {
                        Console.WriteLine("User cancelled the trade recording");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessSingleImage: {ex.Message}");
                MessageBox.Show($"Error analyzing chart: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Console.WriteLine("ProcessSingleImage cleanup - re-enabling buttons");
                _isProcessing = false;
                btnAnalyzeChartImage.Enabled = true;
                btnAnalyzeChartFolder.Enabled = true;
                progressBar.Visible = false;
                lblStatus.Text = "Ready";
                Cursor = Cursors.Default;
            }
        }

        private async Task ProcessFolder(string folderPath)
        {
            _isProcessing = true;
            btnAnalyzeChartImage.Enabled = false;
            btnAnalyzeChartFolder.Enabled = false;
            progressBar.Visible = true;

            try
            {
                var supportedExtensions = new[] { ".png", ".jpg", ".jpeg" };
                var imageFiles = Directory.GetFiles(folderPath)
                    .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .ToList();

                if (!imageFiles.Any())
                {
                    MessageBox.Show("No supported image files found in the selected folder.", "No Images", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Always use local storage for batch processing
                int processedCount = 0;
                int recordedCount = 0;

                foreach (var imageFile in imageFiles)
                {
                    lblStatus.Text = $"Processing {Path.GetFileName(imageFile)} ({processedCount + 1}/{imageFiles.Count})";
                    progressBar.Value = (processedCount * 100) / imageFiles.Count;
                    Application.DoEvents();

                    try
                    {
                        var tradeData = await _tradingJournalService.AnalyzeChartImage(imageFile);
                        
                        // Record the trade automatically to local storage
                        await _tradingJournalService.RecordTrade(tradeData, true); // true = use local storage
                        
                        // Add to temporary state for immediate display
                        _temporaryTrades.Add(tradeData);
                        recordedCount++;
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing
                        Console.WriteLine($"Error processing {Path.GetFileName(imageFile)}: {ex.Message}");
                    }

                    processedCount++;
                }

                // Refresh the data table
                LoadRecentTrades();

                MessageBox.Show(
                    $"Processing complete!\n\nProcessed: {processedCount} images\nRecorded: {recordedCount} trades",
                    "Processing Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                btnAnalyzeChartImage.Enabled = true;
                btnAnalyzeChartFolder.Enabled = true;
                progressBar.Visible = false;
                lblStatus.Text = "Ready";
            }
        }

        private void btnRefresh_Click(object? sender, EventArgs e)
        {
            // Force reload from storage by clearing temporary state if no unsaved changes
            if (!_hasUnsavedChanges)
            {
                _temporaryTrades.Clear();
                _deletedTrades.Clear();
            }
            
            LoadRecentTrades();
            RefreshThumbnails();
        }

        private void RefreshThumbnails()
        {
            // Force the DataGridView to refresh and recreate thumbnails
            dataGridViewTrades.Refresh();
        }

        private void dataGridViewTrades_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dataGridViewTrades.Columns["Chart"].Index)
            {
                try
                {
                    // Get the image path directly from the cell
                    var imagePath = dataGridViewTrades.Rows[e.RowIndex].Cells["Chart"].Value?.ToString();
                    
                    if (string.IsNullOrEmpty(imagePath))
                    {
                        MessageBox.Show("No chart image available for this trade.", "No Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // Check if the image file exists
                    if (File.Exists(imagePath))
                    {
                        try
                        {
                            // Open the chart image in the default image viewer
                            var imageStorageService = new ImageStorageService();
                            imageStorageService.OpenImageInDefaultViewer(imagePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error opening chart image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Chart image file not found: {imagePath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error displaying chart: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            this.Text = "Trading Journal";
            this.WindowState = FormWindowState.Maximized;
            
            // Debug storage locations on startup
            DebugStorageLocations();
            
            // Cleanup orphaned images on startup
            CleanupOrphanedImages();
        }

        private void DataGridViewTrades_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    var newValue = dataGridViewTrades.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    
                    // Handle empty values - set to 0 for numeric fields
                    if (newValue == null || string.IsNullOrEmpty(newValue.ToString()))
                    {
                        if (e.ColumnIndex >= 3 && e.ColumnIndex <= 8) // Numeric columns (adjusted for Trade Seq column)
                        {
                            dataGridViewTrades.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0m;
                        }
                    }
                    
                    // Update the temporary state with the edited data
                    UpdateTradeInStorage(e.RowIndex);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating trade: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DataGridViewTrades_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && dataGridViewTrades.SelectedRows.Count > 0)
            {
                DeleteSelectedRow();
            }
        }

        private void DeleteSelectedRow()
        {
            if (dataGridViewTrades.SelectedRows.Count == 0) return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this trade?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var selectedRow = dataGridViewTrades.SelectedRows[0];
                    var symbol = selectedRow.Cells["Symbol"].Value?.ToString();
                    var date = selectedRow.Cells["Date"].Value?.ToString();
                    var tradeSeq = Convert.ToInt32(selectedRow.Cells["Trade Seq"].Value ?? 0);

                    // Parse the date string properly
                    DateTime parsedDate;
                    if (!DateTime.TryParse(date, out parsedDate))
                    {
                        Console.WriteLine($"Warning: Could not parse date '{date}' for delete");
                        MessageBox.Show($"Could not parse date '{date}'. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Find the trade in temporary state
                    var tradeToDelete = _temporaryTrades.FirstOrDefault(t => 
                        t.Symbol == symbol && 
                        t.Date.Date == parsedDate.Date &&
                        t.TradeSeq == tradeSeq);

                    if (tradeToDelete != null)
                    {
                        Console.WriteLine($"Found trade to delete: {tradeToDelete.Symbol} on {tradeToDelete.Date:yyyy-MM-dd} with TradeSeq {tradeToDelete.TradeSeq}");
                        
                        // Create undo action for temporary state
                        var undoAction = new DeleteTradeAction(tradeToDelete, _temporaryTrades, _deletedTrades);
                        AddUndoAction(undoAction);

                        // Track deleted trade for image cleanup
                        _deletedTrades.Add(tradeToDelete);

                        // Remove from temporary state
                        _temporaryTrades.Remove(tradeToDelete);

                        // Remove from data table
                        dataGridViewTrades.Rows.Remove(selectedRow);

                        // Mark as having unsaved changes
                        SetUnsavedChanges(true);

                        MessageBox.Show("Trade deleted successfully! (Changes are temporary until saved)", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        Console.WriteLine($"No trade found matching: Symbol={symbol}, Date={parsedDate:yyyy-MM-dd}, TradeSeq={tradeSeq}");
                        Console.WriteLine("Available trades in temporary state:");
                        foreach (var trade in _temporaryTrades)
                        {
                            Console.WriteLine($"  - {trade.Symbol} on {trade.Date:yyyy-MM-dd} with TradeSeq {trade.TradeSeq}");
                        }
                        MessageBox.Show("Trade not found in temporary state. Please refresh and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting trade: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateTradeInStorage(int rowIndex)
        {
            try
            {
                var symbol = dataGridViewTrades.Rows[rowIndex].Cells["Symbol"].Value?.ToString();
                var date = dataGridViewTrades.Rows[rowIndex].Cells["Date"].Value?.ToString();
                var tradeSeq = Convert.ToInt32(dataGridViewTrades.Rows[rowIndex].Cells["Trade Seq"].Value ?? 0);
                
                // Parse the date string properly
                DateTime parsedDate;
                if (!DateTime.TryParse(date, out parsedDate))
                {
                    Console.WriteLine($"Warning: Could not parse date '{date}' for update");
                    return;
                }
                
                var tradeToUpdate = _temporaryTrades.FirstOrDefault(t => 
                    t.Symbol == symbol && 
                    t.Date.Date == parsedDate.Date &&
                    t.TradeSeq == tradeSeq);

                if (tradeToUpdate != null)
                {
                    // Create a copy of the original trade for undo
                    var originalTrade = new TradeData
                    {
                        Symbol = tradeToUpdate.Symbol,
                        Date = tradeToUpdate.Date,
                        TradeSeq = tradeToUpdate.TradeSeq,
                        PreviousDayClose = tradeToUpdate.PreviousDayClose,
                        HighAfterVolumeSurge = tradeToUpdate.HighAfterVolumeSurge,
                        LowAfterVolumeSurge = tradeToUpdate.LowAfterVolumeSurge,
                        GapPercentToHigh = tradeToUpdate.GapPercentToHigh,
                        GapPercentHighToLow = tradeToUpdate.GapPercentHighToLow,
                        Volume = tradeToUpdate.Volume,
                        ChartImagePath = tradeToUpdate.ChartImagePath
                    };

                    // Update the trade data with edited values
                    tradeToUpdate.PreviousDayClose = Convert.ToDecimal(dataGridViewTrades.Rows[rowIndex].Cells["Previous Close"].Value ?? 0m);
                    tradeToUpdate.HighAfterVolumeSurge = Convert.ToDecimal(dataGridViewTrades.Rows[rowIndex].Cells["High After Volume Surge"].Value ?? 0m);
                    tradeToUpdate.LowAfterVolumeSurge = Convert.ToDecimal(dataGridViewTrades.Rows[rowIndex].Cells["Low After Volume Surge"].Value ?? 0m);
                    
                    // Ensure gap percentages are positive integers
                    var gapToHigh = Convert.ToDecimal(dataGridViewTrades.Rows[rowIndex].Cells["Gap % (Close to High)"].Value ?? 0m);
                    var gapHighToLow = Convert.ToDecimal(dataGridViewTrades.Rows[rowIndex].Cells["Gap % (High to Low)"].Value ?? 0m);
                    
                    tradeToUpdate.GapPercentToHigh = Math.Abs(Math.Round(gapToHigh, 0)); // Ensure positive integer
                    tradeToUpdate.GapPercentHighToLow = Math.Abs(Math.Round(gapHighToLow, 0)); // Ensure positive integer
                    
                    // Update the display to show the corrected values
                    dataGridViewTrades.Rows[rowIndex].Cells["Gap % (Close to High)"].Value = tradeToUpdate.GapPercentToHigh;
                    dataGridViewTrades.Rows[rowIndex].Cells["Gap % (High to Low)"].Value = tradeToUpdate.GapPercentHighToLow;
                    
                    tradeToUpdate.Volume = Convert.ToInt64((Convert.ToDecimal(dataGridViewTrades.Rows[rowIndex].Cells["Volume (M)"].Value ?? 0m) * 1000000m));
                    
                    // Update Trade Seq if it was changed (though it should be read-only)
                    var currentTradeSeq = Convert.ToInt32(dataGridViewTrades.Rows[rowIndex].Cells["Trade Seq"].Value ?? 0);
                    tradeToUpdate.TradeSeq = currentTradeSeq;

                    // Create undo action for the edit
                    var modifiedTrade = new TradeData
                    {
                        Symbol = tradeToUpdate.Symbol,
                        Date = tradeToUpdate.Date,
                        TradeSeq = tradeToUpdate.TradeSeq,
                        PreviousDayClose = tradeToUpdate.PreviousDayClose,
                        HighAfterVolumeSurge = tradeToUpdate.HighAfterVolumeSurge,
                        LowAfterVolumeSurge = tradeToUpdate.LowAfterVolumeSurge,
                        GapPercentToHigh = tradeToUpdate.GapPercentToHigh,
                        GapPercentHighToLow = tradeToUpdate.GapPercentHighToLow,
                        Volume = tradeToUpdate.Volume,
                        ChartImagePath = tradeToUpdate.ChartImagePath
                    };

                    var undoAction = new EditTradeAction(originalTrade, modifiedTrade, _temporaryTrades);
                    AddUndoAction(undoAction);

                    // Mark as having unsaved changes
                    SetUnsavedChanges(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating trade in temporary state: {ex.Message}");
            }
        }

        private async Task DeleteTradeFromStorage(string symbol, string date, int tradeSeq)
        {
            try
            {
                Console.WriteLine($"DeleteTradeFromStorage called with: Symbol={symbol}, Date={date}, TradeSeq={tradeSeq}");
                
                var trades = await _tradingJournalService.GetRecentTrades(50, true);
                Console.WriteLine($"Found {trades.Count} trades in storage");
                
                // Parse the date string properly
                DateTime parsedDate;
                if (!DateTime.TryParse(date, out parsedDate))
                {
                    Console.WriteLine($"Warning: Could not parse date '{date}'");
                    return;
                }
                
                var tradeToDelete = trades.FirstOrDefault(t => 
                    t.Symbol == symbol && 
                    t.Date.Date == parsedDate.Date &&
                    t.TradeSeq == tradeSeq);

                if (tradeToDelete != null)
                {
                    Console.WriteLine($"Found trade to delete: {tradeToDelete.Symbol} on {tradeToDelete.Date:yyyy-MM-dd} with TradeSeq {tradeToDelete.TradeSeq}");
                    
                    // Delete the linked chart image if it exists
                    if (!string.IsNullOrEmpty(tradeToDelete.ChartImagePath))
                    {
                        var imageStorageService = new ImageStorageService();
                        if (imageStorageService.DeleteImage(tradeToDelete.ChartImagePath))
                        {
                            Console.WriteLine($"Deleted chart image: {tradeToDelete.ChartImagePath}");
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Could not delete chart image {tradeToDelete.ChartImagePath}");
                        }
                    }

                    // Remove the trade from storage
                    trades.Remove(tradeToDelete);
                    await _localStorageService.SaveTrades(trades);
                    Console.WriteLine($"Trade removed from storage. Remaining trades: {trades.Count}");
                }
                else
                {
                    Console.WriteLine($"No trade found matching: Symbol={symbol}, Date={parsedDate:yyyy-MM-dd}, TradeSeq={tradeSeq}");
                    Console.WriteLine("Available trades:");
                    foreach (var trade in trades)
                    {
                        Console.WriteLine($"  - {trade.Symbol} on {trade.Date:yyyy-MM-dd} with TradeSeq {trade.TradeSeq}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteTradeFromStorage: {ex.Message}");
                throw new Exception($"Error deleting trade from storage: {ex.Message}");
            }
        }

        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Delete Trade");
            deleteItem.Click += (sender, e) => DeleteSelectedRow();
            _contextMenu.Items.Add(deleteItem);
        }

        private void DataGridViewTrades_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridViewTrades.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    dataGridViewTrades.ClearSelection();
                    dataGridViewTrades.Rows[hit.RowIndex].Selected = true;
                    _contextMenu.Show(dataGridViewTrades, e.Location);
                }
            }
        }

        private void DataGridViewTrades_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == dataGridViewTrades.Columns["Chart"].Index && e.Value != null)
            {
                var imagePath = e.Value.ToString();
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    // Just show "View Chart" text instead of thumbnail to avoid formatting errors
                    e.Value = "View Chart";
                    e.FormattingApplied = true;
                }
                else
                {
                    e.Value = "No Image";
                    e.FormattingApplied = true;
                }
            }
        }

        private void DataGridViewTrades_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            // Handle DataGridView errors gracefully
            if (e.Exception is FormatException)
            {
                e.ThrowException = false;
                Console.WriteLine($"DataGridView formatting error in column {e.ColumnIndex}, row {e.RowIndex}: {e.Exception.Message}");
            }
        }

        private Image CreateThumbnail(string imagePath, int width, int height)
        {
            try
            {
                using (var originalImage = Image.FromFile(imagePath))
                {
                    var thumbnail = new Bitmap(width, height);
                    using (var graphics = Graphics.FromImage(thumbnail))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(originalImage, 0, 0, width, height);
                    }
                    return thumbnail;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating thumbnail for {imagePath}: {ex.Message}");
                // Return a placeholder image
                var placeholder = new Bitmap(width, height);
                using (var graphics = Graphics.FromImage(placeholder))
                {
                    graphics.Clear(Color.LightGray);
                    using (var font = new Font("Arial", 8))
                    {
                        graphics.DrawString("Error", font, Brushes.Red, 2, 2);
                    }
                }
                return placeholder;
            }
        }



        // Debug method to check storage locations
        private void DebugStorageLocations()
        {
            try
            {
                var localStorageService = new LocalStorageService();
                var imageStorageService = new ImageStorageService();
                
                Console.WriteLine($"Data Directory: {localStorageService.GetDataLocation()}");
                Console.WriteLine($"Image Storage Path: {imageStorageService.GetImageStoragePath()}");
                
                // Check if directories exist
                Console.WriteLine($"Data Directory Exists: {Directory.Exists(localStorageService.GetDataLocation())}");
                Console.WriteLine($"Image Directory Exists: {Directory.Exists(imageStorageService.GetImageStoragePath())}");
                
                // List files in data directory
                if (Directory.Exists(localStorageService.GetDataLocation()))
                {
                    var files = Directory.GetFiles(localStorageService.GetDataLocation());
                    Console.WriteLine($"Files in Data Directory: {string.Join(", ", files)}");
                }
                
                // List files in image directory
                if (Directory.Exists(imageStorageService.GetImageStoragePath()))
                {
                    var imageFiles = Directory.GetFiles(imageStorageService.GetImageStoragePath());
                    Console.WriteLine($"Files in Image Directory: {string.Join(", ", imageFiles)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DebugStorageLocations: {ex.Message}");
            }
        }

        // Cleanup orphaned images
        private async void CleanupOrphanedImages()
        {
            try
            {
                var trades = await _tradingJournalService.GetRecentTrades(1000, true);
                var validImagePaths = trades
                    .Where(t => !string.IsNullOrEmpty(t.ChartImagePath))
                    .Select(t => t.ChartImagePath)
                    .ToList();

                var imageStorageService = new ImageStorageService();
                imageStorageService.CleanupOrphanedImages(validImagePaths);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during orphaned image cleanup: {ex.Message}");
            }
        }

        private void InitializeTemporaryState()
        {
            // Create save button - positioned between analyze folder and refresh buttons
            _btnSaveChanges = new Button
            {
                Text = "Save Changes",
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(120, 30),
                Location = new Point(324, 12), // Position between folder button (168) and refresh button (638)
                Visible = true // Make visible for testing
            };
            _btnSaveChanges.Click += BtnSaveChanges_Click;

            // Create unsaved changes label
            _lblUnsavedChanges = new Label
            {
                Text = "⚠️ Unsaved Changes",
                ForeColor = Color.Orange,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(12, 85),
                Visible = false
            };

            // Add controls to bottom panel
            panelBottom.Controls.Add(_btnSaveChanges);
            panelBottom.Controls.Add(_lblUnsavedChanges);
        }

        private void SetUnsavedChanges(bool hasChanges)
        {
            _hasUnsavedChanges = hasChanges;
            _btnSaveChanges.Visible = hasChanges;
            _lblUnsavedChanges.Visible = hasChanges;
            
            // Update window title to show unsaved changes
            if (hasChanges)
            {
                if (!Text.EndsWith(" *"))
                    Text += " *";
            }
            else
            {
                Text = Text.Replace(" *", "");
            }
        }

        private async void BtnSaveChanges_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to save all changes? This will permanently update your trade data.",
                "Confirm Save",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    _btnSaveChanges.Enabled = false;
                    _btnSaveChanges.Text = "Saving...";

                    // Save all temporary trades to storage
                    await _localStorageService.SaveTrades(_temporaryTrades);

                    // Clean up images for deleted trades
                    if (_deletedTrades.Count > 0)
                    {
                        var imageStorageService = new ImageStorageService();
                        foreach (var deletedTrade in _deletedTrades)
                        {
                            if (!string.IsNullOrEmpty(deletedTrade.ChartImagePath))
                            {
                                imageStorageService.DeleteImage(deletedTrade.ChartImagePath);
                            }
                        }
                        _deletedTrades.Clear(); // Clear the deleted trades list
                    }

                    // Clear undo/redo stacks since changes are now permanent
                    _undoStack.Clear();
                    _redoStack.Clear();
                    UpdateUndoRedoMenuItems();

                    // Reset unsaved changes state and reload from storage
                    SetUnsavedChanges(false);
                    
                    // Reload from storage to ensure consistency
                    LoadRecentTrades();

                    MessageBox.Show("Changes saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                    _btnSaveChanges.Enabled = true;
                    _btnSaveChanges.Text = "Save Changes";
                }
            }
        }

        #region Menu Event Handlers

        private void newTradeMenuItem_Click(object? sender, EventArgs e)
        {
            btnAnalyzeChartImage_Click(sender, e);
        }

        private void openImageMenuItem_Click(object? sender, EventArgs e)
        {
            btnAnalyzeChartImage_Click(sender, e);
        }

        private void openFolderMenuItem_Click(object? sender, EventArgs e)
        {
            btnAnalyzeChartFolder_Click(sender, e);
        }

        private void exportToCSVMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                    saveFileDialog.FilterIndex = 1;
                    saveFileDialog.FileName = $"TradingJournal_{DateTime.Now:yyyyMMdd}.csv";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportToCSV(saveFileDialog.FileName);
                        MessageBox.Show("Data exported to CSV successfully!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exportToExcelMenuItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Excel export functionality will be implemented in a future version.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exitMenuItem_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void undoMenuItem_Click(object? sender, EventArgs e)
        {
            Undo();
        }

        private void redoMenuItem_Click(object? sender, EventArgs e)
        {
            Redo();
        }

        private void cutMenuItem_Click(object? sender, EventArgs e)
        {
            if (dataGridViewTrades.SelectedRows.Count > 0)
            {
                CopySelectedRows();
                DeleteSelectedRow();
            }
        }

        private void copyMenuItem_Click(object? sender, EventArgs e)
        {
            CopySelectedRows();
        }

        private void pasteMenuItem_Click(object? sender, EventArgs e)
        {
            // Paste functionality would be implemented for manual trade entry
            MessageBox.Show("Paste functionality will be implemented for manual trade entry.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void selectAllMenuItem_Click(object? sender, EventArgs e)
        {
            dataGridViewTrades.SelectAll();
        }

        private void deleteMenuItem_Click(object? sender, EventArgs e)
        {
            DeleteSelectedRow();
        }

        private void refreshMenuItem_Click(object? sender, EventArgs e)
        {
            btnRefresh_Click(sender, e);
        }

        private void cleanupOrphanedImagesMenuItem_Click(object? sender, EventArgs e)
        {
            CleanupOrphanedImages();
            MessageBox.Show("Orphaned images cleanup completed!", "Cleanup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void optionsMenuItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Options dialog will be implemented in a future version.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void aboutMenuItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "Trading Journal GPT\n\n" +
                "Version 1.0\n" +
                "A Windows application for analyzing trading charts using AI.\n\n" +
                "Features:\n" +
                "• Chart image analysis with ChatGPT\n" +
                "• Local data storage\n" +
                "• Trade data management\n" +
                "• Undo/Redo functionality\n\n" +
                "© 2024 Trading Journal GPT",
                "About Trading Journal GPT",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        #endregion

        #region Undo/Redo System

        private void AddUndoAction(UndoRedoAction action)
        {
            if (!_isUndoRedoAction)
            {
                _undoStack.Push(action);
                _redoStack.Clear(); // Clear redo stack when new action is performed
                UpdateUndoRedoMenuItems();
            }
        }

        private void Undo()
        {
            if (_undoStack.Count > 0)
            {
                _isUndoRedoAction = true;
                var action = _undoStack.Pop();
                action.Undo();
                _redoStack.Push(action);
                _isUndoRedoAction = false;
                UpdateUndoRedoMenuItems();
                
                // Refresh the display and mark as having unsaved changes
                LoadRecentTrades();
                SetUnsavedChanges(true);
            }
        }

        private void Redo()
        {
            if (_redoStack.Count > 0)
            {
                _isUndoRedoAction = true;
                var action = _redoStack.Pop();
                action.Redo();
                _undoStack.Push(action);
                _isUndoRedoAction = false;
                UpdateUndoRedoMenuItems();
                
                // Refresh the display and mark as having unsaved changes
                LoadRecentTrades();
                SetUnsavedChanges(true);
            }
        }

        private void UpdateUndoRedoMenuItems()
        {
            undoMenuItem.Enabled = _undoStack.Count > 0;
            redoMenuItem.Enabled = _redoStack.Count > 0;
        }

        private void CopySelectedRows()
        {
            if (dataGridViewTrades.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridViewTrades.SelectedRows[0];
                var tradeData = new
                {
                    Symbol = selectedRow.Cells["Symbol"].Value?.ToString(),
                    Date = selectedRow.Cells["Date"].Value?.ToString(),
                    TradeSeq = selectedRow.Cells["Trade Seq"].Value?.ToString(),
                    PreviousClose = selectedRow.Cells["Previous Close"].Value?.ToString(),
                    HighAfterVolumeSurge = selectedRow.Cells["High After Volume Surge"].Value?.ToString(),
                    LowAfterVolumeSurge = selectedRow.Cells["Low After Volume Surge"].Value?.ToString(),
                    GapPercentToHigh = selectedRow.Cells["Gap % (Close to High)"].Value?.ToString(),
                    GapPercentHighToLow = selectedRow.Cells["Gap % (High to Low)"].Value?.ToString(),
                    Volume = selectedRow.Cells["Volume (M)"].Value?.ToString()
                };

                var json = System.Text.Json.JsonSerializer.Serialize(tradeData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                Clipboard.SetText(json);
            }
        }

        private void ExportToCSV(string filePath)
        {
            var trades = _tradesDataTable.AsEnumerable();
            var csv = new System.Text.StringBuilder();
            
            // Add headers
            csv.AppendLine("Symbol,Date,Trade Seq,Previous Close,High After Volume Surge,Low After Volume Surge,Gap % (Close to High),Gap % (High to Low),Volume (M)");
            
            // Add data rows
            foreach (var row in trades)
            {
                csv.AppendLine($"{row["Symbol"]},{row["Date"]},{row["Trade Seq"]},{row["Previous Close"]},{row["High After Volume Surge"]},{row["Low After Volume Surge"]},{row["Gap % (Close to High)"]},{row["Gap % (High to Low)"]},{row["Volume (M)"]}");
            }
            
            File.WriteAllText(filePath, csv.ToString());
        }

        #endregion
    }

    #region Undo/Redo Action Classes

    public abstract class UndoRedoAction
    {
        public abstract void Undo();
        public abstract void Redo();
    }

    public class DeleteTradeAction : UndoRedoAction
    {
        private readonly TradeData _deletedTrade;
        private readonly List<TradeData> _temporaryTrades;
        private readonly List<TradeData> _deletedTrades;

        public DeleteTradeAction(TradeData deletedTrade, List<TradeData> temporaryTrades, List<TradeData> deletedTrades)
        {
            _deletedTrade = deletedTrade;
            _temporaryTrades = temporaryTrades;
            _deletedTrades = deletedTrades;
        }

        public override void Undo()
        {
            try
            {
                _temporaryTrades.Add(_deletedTrade);
                _deletedTrades.Remove(_deletedTrade); // Remove from deleted trades list
                Console.WriteLine($"Undo: Restored trade {_deletedTrade.Symbol} on {_deletedTrade.Date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during undo: {ex.Message}");
            }
        }

        public override void Redo()
        {
            try
            {
                var tradeToDelete = _temporaryTrades.FirstOrDefault(t =>
                    t.Symbol == _deletedTrade.Symbol &&
                    t.Date.Date == _deletedTrade.Date.Date &&
                    t.TradeSeq == _deletedTrade.TradeSeq);

                if (tradeToDelete != null)
                {
                    _temporaryTrades.Remove(tradeToDelete);
                    _deletedTrades.Add(_deletedTrade); // Add back to deleted trades list
                    Console.WriteLine($"Redo: Deleted trade {_deletedTrade.Symbol} on {_deletedTrade.Date:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during redo: {ex.Message}");
            }
        }
    }

    public class EditTradeAction : UndoRedoAction
    {
        private readonly TradeData _originalTrade;
        private readonly TradeData _modifiedTrade;
        private readonly List<TradeData> _temporaryTrades;

        public EditTradeAction(TradeData originalTrade, TradeData modifiedTrade, List<TradeData> temporaryTrades)
        {
            _originalTrade = originalTrade;
            _modifiedTrade = modifiedTrade;
            _temporaryTrades = temporaryTrades;
        }

        public override void Undo()
        {
            try
            {
                var tradeToRestore = _temporaryTrades.FirstOrDefault(t => 
                    t.Symbol == _modifiedTrade.Symbol && 
                    t.Date.Date == _modifiedTrade.Date.Date &&
                    t.TradeSeq == _modifiedTrade.TradeSeq);

                if (tradeToRestore != null)
                {
                    // Restore original values
                    tradeToRestore.PreviousDayClose = _originalTrade.PreviousDayClose;
                    tradeToRestore.HighAfterVolumeSurge = _originalTrade.HighAfterVolumeSurge;
                    tradeToRestore.LowAfterVolumeSurge = _originalTrade.LowAfterVolumeSurge;
                    tradeToRestore.GapPercentToHigh = _originalTrade.GapPercentToHigh;
                    tradeToRestore.GapPercentHighToLow = _originalTrade.GapPercentHighToLow;
                    tradeToRestore.Volume = _originalTrade.Volume;

                    Console.WriteLine($"Undo: Restored original values for trade {_originalTrade.Symbol}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during undo: {ex.Message}");
            }
        }

        public override void Redo()
        {
            try
            {
                var tradeToModify = _temporaryTrades.FirstOrDefault(t => 
                    t.Symbol == _originalTrade.Symbol && 
                    t.Date.Date == _originalTrade.Date.Date &&
                    t.TradeSeq == _originalTrade.TradeSeq);

                if (tradeToModify != null)
                {
                    // Apply modified values
                    tradeToModify.PreviousDayClose = _modifiedTrade.PreviousDayClose;
                    tradeToModify.HighAfterVolumeSurge = _modifiedTrade.HighAfterVolumeSurge;
                    tradeToModify.LowAfterVolumeSurge = _modifiedTrade.LowAfterVolumeSurge;
                    tradeToModify.GapPercentToHigh = _modifiedTrade.GapPercentToHigh;
                    tradeToModify.GapPercentHighToLow = _modifiedTrade.GapPercentHighToLow;
                    tradeToModify.Volume = _modifiedTrade.Volume;

                    Console.WriteLine($"Redo: Applied modified values for trade {_modifiedTrade.Symbol}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during redo: {ex.Message}");
            }
        }
    }

    #endregion
} 