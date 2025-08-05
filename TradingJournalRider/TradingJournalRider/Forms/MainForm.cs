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
using OfficeOpenXml;

namespace TradingJournalGPT.Forms
{
    public partial class MainForm : Form
    {
        private readonly TradingJournalService _tradingJournalService;
        private readonly LocalStorageService _localStorageService;
        private readonly FloatDataService _floatDataService;
        private DataTable _tradesDataTable = new DataTable();
        private DataTable _setupsDataTable = new DataTable();
        private DataTable _technicalsDataTable = new DataTable();
        private bool _isProcessing = false;
        private ContextMenuStrip _contextMenu = null!;
        private ContextMenuStrip _setupsContextMenu = null!;
        private ContextMenuStrip _technicalsContextMenu = null!;
        
        // Enhanced Undo/Redo system with temporary state
        private readonly Stack<UndoRedoAction> _undoStack = new Stack<UndoRedoAction>();
        private readonly Stack<UndoRedoAction> _redoStack = new Stack<UndoRedoAction>();
        private bool _isUndoRedoAction = false;
        
        // Temporary state management
        private List<TradeData> _temporaryTrades = new List<TradeData>();
        private List<TradeData> _deletedTrades = new List<TradeData>(); // Track deleted trades for image cleanup
        private List<Dictionary<string, object>> _temporarySetups = new List<Dictionary<string, object>>();
        private List<Dictionary<string, object>> _deletedSetups = new List<Dictionary<string, object>>(); // Track deleted setups
        private List<Dictionary<string, object>> _temporaryTechnicals = new List<Dictionary<string, object>>();
        private List<Dictionary<string, object>> _deletedTechnicals = new List<Dictionary<string, object>>(); // Track deleted technicals
        private bool _hasUnsavedChanges = false;

        public MainForm()
        {
            InitializeComponent();
            _tradingJournalService = new TradingJournalService();
            _localStorageService = new LocalStorageService();
            _floatDataService = new FloatDataService();
            
            // Load application icon
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "Assets", "Icon", "TradingJournalIcon.png");
                if (File.Exists(iconPath))
                {
                    using (var bitmap = new Bitmap(iconPath))
                    {
                        this.Icon = Icon.FromHandle(bitmap.GetHicon());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load application icon: {ex.Message}");
            }
            
            InitializeDataTable();
            InitializeTemporaryState();
            
            // Handle form closing to check for unsaved changes
            this.FormClosing += MainForm_FormClosing;
            
            // Load initial data from storage
            _ = Task.Run(async () => 
            {
                await Task.Delay(200); // Increased delay to ensure UI is fully ready
                await _floatDataService.InitializeAsync(); // Initialize float data service
                this.Invoke(() => LoadRecentTrades());
                await LoadSetupsData(); // Load setups data on startup
                await LoadTechnicalsData(); // Load technicals data on startup
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
            _tradesDataTable.Columns.Add("Strategy", typeof(string));
            _tradesDataTable.Columns.Add("Float", typeof(decimal));
            _tradesDataTable.Columns.Add("Catalyst", typeof(string));
            _tradesDataTable.Columns.Add("Technicals", typeof(string));
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
            dataGridViewTrades.Columns["Float"].ReadOnly = true; // Float is read-only (from CSV data)
            dataGridViewTrades.Columns["Chart"].ReadOnly = true;
            
            // Configure Chart column for image display
            var chartColumn = dataGridViewTrades.Columns["Chart"];
            chartColumn.Width = 100;
            chartColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            // Configure Strategy column
            var strategyColumn = dataGridViewTrades.Columns["Strategy"];
            strategyColumn.Width = 150;
            
            // Configure Float column
            var floatColumn = dataGridViewTrades.Columns["Float"];
            floatColumn.Width = 80;
            floatColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            
            // Configure Catalyst column
            var catalystColumn = dataGridViewTrades.Columns["Catalyst"];
            catalystColumn.Width = 150;
            
            // Configure Technicals column
            var technicalsColumn = dataGridViewTrades.Columns["Technicals"];
            technicalsColumn.Width = 150;
            
            // Add event handlers
            dataGridViewTrades.CellEndEdit += DataGridViewTrades_CellEndEdit;
            dataGridViewTrades.KeyDown += DataGridViewTrades_KeyDown;
            dataGridViewTrades.CellFormatting += DataGridViewTrades_CellFormatting;
            dataGridViewTrades.DataError += DataGridViewTrades_DataError;
            dataGridViewTrades.CellClick += dataGridViewTrades_CellClick;
            dataGridViewTrades.MouseClick += DataGridViewTrades_MouseClick;
            
            // Create context menu for delete functionality
            CreateContextMenu();

            // Initialize Setups DataTable
            InitializeSetupsDataTable();
            
            // Initialize Technicals DataTable
            InitializeTechnicalsDataTable();
        }

        private void InitializeSetupsDataTable()
        {
            _setupsDataTable = new DataTable();
            _setupsDataTable.Columns.Add("Strategy", typeof(string));
            _setupsDataTable.Columns.Add("Direction", typeof(string));
            _setupsDataTable.Columns.Add("Cycle", typeof(string));
            _setupsDataTable.Columns.Add("Meta Grade", typeof(string));
            _setupsDataTable.Columns.Add("Description", typeof(string));
            _setupsDataTable.Columns.Add("Pre-req", typeof(string));
            _setupsDataTable.Columns.Add("Ruin Variables", typeof(string));
            _setupsDataTable.Columns.Add("Entry (s)", typeof(string));
            _setupsDataTable.Columns.Add("Exit 1", typeof(string));
            _setupsDataTable.Columns.Add("Exit 2", typeof(string));
            _setupsDataTable.Columns.Add("Exit 3", typeof(string));
            _setupsDataTable.Columns.Add("Stop", typeof(string));
            _setupsDataTable.Columns.Add("Examples", typeof(string));

            dataGridViewSetups.DataSource = _setupsDataTable;
            dataGridViewSetups.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dataGridViewSetups.AllowUserToAddRows = true;
            dataGridViewSetups.ReadOnly = false;
            dataGridViewSetups.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewSetups.MultiSelect = false;
            
            // Add event handlers for setups
            dataGridViewSetups.CellEndEdit += DataGridViewSetups_CellEndEdit;
            dataGridViewSetups.KeyDown += DataGridViewSetups_KeyDown;
            dataGridViewSetups.DataError += DataGridViewSetups_DataError;
            dataGridViewSetups.MouseClick += DataGridViewSetups_MouseClick;

            // Create context menu for setups
            CreateSetupsContextMenu();

            // Load sample setups data
            LoadSampleSetupsData();
        }

        private void InitializeTechnicalsDataTable()
        {
            _technicalsDataTable = new DataTable();
            _technicalsDataTable.Columns.Add("Type", typeof(string));
            _technicalsDataTable.Columns.Add("Description", typeof(string));
            _technicalsDataTable.Columns.Add("Examples", typeof(string));

            dataGridViewTechnicals.DataSource = _technicalsDataTable;
            dataGridViewTechnicals.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
            dataGridViewTechnicals.AllowUserToAddRows = true;
            dataGridViewTechnicals.ReadOnly = false;
            dataGridViewTechnicals.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewTechnicals.MultiSelect = false;
            
            // Add event handlers for technicals
            dataGridViewTechnicals.CellEndEdit += DataGridViewTechnicals_CellEndEdit;
            dataGridViewTechnicals.KeyDown += DataGridViewTechnicals_KeyDown;
            dataGridViewTechnicals.DataError += DataGridViewTechnicals_DataError;
            dataGridViewTechnicals.MouseClick += DataGridViewTechnicals_MouseClick;

            // Create context menu for technicals
            CreateTechnicalsContextMenu();

            // Load sample technicals data
            LoadSampleTechnicalsData();
        }

        private void LoadSampleSetupsData()
        {
            Console.WriteLine("LoadSampleSetupsData called");
            
            // Add sample setups data to temporary state
            var sampleSetups = new[]
            {
                new Dictionary<string, object> { 
                    ["Strategy"] = "Gap and Go", 
                    ["Direction"] = "Long", 
                    ["Cycle"] = "Daily", 
                    ["Meta Grade"] = "A", 
                    ["Description"] = "Stock gaps up with high volume and continues higher", 
                    ["Pre-req"] = "High volume gap up", 
                    ["Ruin Variables"] = "Low volume, gap down", 
                    ["Entry (s)"] = "Break of first 5min high", 
                    ["Exit 1"] = "2R target", 
                    ["Exit 2"] = "3R target", 
                    ["Exit 3"] = "5R target", 
                    ["Stop"] = "Below gap fill", 
                    ["Examples"] = "TSLA, NVDA" 
                },
                new Dictionary<string, object> { 
                    ["Strategy"] = "Breakout", 
                    ["Direction"] = "Long/Short", 
                    ["Cycle"] = "Daily", 
                    ["Meta Grade"] = "B", 
                    ["Description"] = "Breakout from consolidation with volume", 
                    ["Pre-req"] = "Consolidation pattern", 
                    ["Ruin Variables"] = "False breakout", 
                    ["Entry (s)"] = "Break of resistance/support", 
                    ["Exit 1"] = "1.5R target", 
                    ["Exit 2"] = "2.5R target", 
                    ["Exit 3"] = "4R target", 
                    ["Stop"] = "Below/above breakout level", 
                    ["Examples"] = "AAPL, MSFT" 
                }
            };
            
            _temporarySetups = new List<Dictionary<string, object>>(sampleSetups);
            Console.WriteLine($"Loaded {_temporarySetups.Count} sample setups");
            
            // Refresh the display
            RefreshSetupsDisplay();
            
            Console.WriteLine("LoadSampleSetupsData completed");
        }

        private void LoadSampleTechnicalsData()
        {
            Console.WriteLine("LoadSampleTechnicalsData called");
            
            // Add sample technicals data to temporary state
            _temporaryTechnicals.Add(new Dictionary<string, object>
            {
                ["Type"] = "Breakout",
                ["Description"] = "Price breaks above resistance level with increased volume",
                ["Examples"] = "AAPL, TSLA, NVDA"
            });
            
            _temporaryTechnicals.Add(new Dictionary<string, object>
            {
                ["Type"] = "Gap Up",
                ["Description"] = "Price opens significantly higher than previous close",
                ["Examples"] = "MRNA, ZM, PTON"
            });
            
            _temporaryTechnicals.Add(new Dictionary<string, object>
            {
                ["Type"] = "Volume Spike",
                ["Description"] = "Unusual high volume compared to average daily volume",
                ["Examples"] = "GME, AMC, BB"
            });
            
            _temporaryTechnicals.Add(new Dictionary<string, object>
            {
                ["Type"] = "RSI Divergence",
                ["Description"] = "Price makes new highs while RSI makes lower highs",
                ["Examples"] = "SPY, QQQ, IWM"
            });
            
            _temporaryTechnicals.Add(new Dictionary<string, object>
            {
                ["Type"] = "Moving Average Crossover",
                ["Description"] = "Short-term MA crosses above long-term MA",
                ["Examples"] = "MSFT, GOOGL, AMZN"
            });
            
            // Refresh the display
            RefreshTechnicalsDisplay();
            
            Console.WriteLine("LoadSampleTechnicalsData completed");
        }

        private async Task SaveSetupsData()
        {
            try
            {
                // Create the setups file path
                var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
                var dataDirectory = Path.Combine(appDirectory, "Data");
                var setupsFile = Path.Combine(dataDirectory, "setups.json");
                
                // Ensure directory exists
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }

                // Save temporary setups to file
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = System.Text.Json.JsonSerializer.Serialize(_temporarySetups, options);
                await File.WriteAllTextAsync(setupsFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving setups data: {ex.Message}");
            }
        }

        private async Task LoadSetupsData()
        {
            try
            {
                Console.WriteLine("LoadSetupsData called");
                
                // Create the setups file path
                var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
                var dataDirectory = Path.Combine(appDirectory, "Data");
                var setupsFile = Path.Combine(dataDirectory, "setups.json");
                
                Console.WriteLine($"Looking for setups file at: {setupsFile}");
                
                if (File.Exists(setupsFile))
                {
                    Console.WriteLine("Setups file found, loading data...");
                    var json = await File.ReadAllTextAsync(setupsFile);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var setupsData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json, options);
                        
                        if (setupsData != null && setupsData.Count() > 0)
                        {
                            Console.WriteLine($"Loaded {setupsData.Count()} setups from file");
                            // Initialize temporary state with loaded data
                            _temporarySetups = new List<Dictionary<string, object>>(setupsData);
                            
                            // Refresh the display
                            RefreshSetupsDisplay();
                            
                            // Refresh the Strategy ComboBox items in the trades grid
                            // RefreshStrategyComboBoxItems(); // This method is being removed
                            return; // Successfully loaded data
                        }
                    }
                }
                
                Console.WriteLine("No saved setups data found, loading sample data...");
                // Load sample data if no saved data exists
                LoadSampleSetupsData();
                
                // Refresh the Strategy ComboBox items in the trades grid
                // RefreshStrategyComboBoxItems(); // This method is being removed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading setups data: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                LoadSampleSetupsData();
            }
        }

        private async Task SaveTechnicalsData()
        {
            try
            {
                var technicalsData = _temporaryTechnicals.Select(setup => new Dictionary<string, object>(setup)).ToList();
                var json = System.Text.Json.JsonSerializer.Serialize(technicalsData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
                var technicalsFilePath = Path.Combine(appDirectory, "technicals.json");
                
                await File.WriteAllTextAsync(technicalsFilePath, json);
                Console.WriteLine($"Technicals data saved to {technicalsFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving technicals data: {ex.Message}");
                throw;
            }
        }

        private async Task LoadTechnicalsData()
        {
            try
            {
                var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
                var technicalsFilePath = Path.Combine(appDirectory, "technicals.json");
                
                if (File.Exists(technicalsFilePath))
                {
                    var json = await File.ReadAllTextAsync(technicalsFilePath);
                    var technicalsData = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
                    
                    if (technicalsData != null && technicalsData.Count() > 0)
                    {
                        _temporaryTechnicals = technicalsData;
                        Console.WriteLine($"Loaded {technicalsData.Count} technicals from {technicalsFilePath}");
                    }
                    else
                    {
                        Console.WriteLine("No technicals data found, using sample data");
                        LoadSampleTechnicalsData();
                    }
                }
                else
                {
                    Console.WriteLine("Technicals file not found, using sample data");
                    LoadSampleTechnicalsData();
                }
                
                RefreshTechnicalsDisplay();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading technicals data: {ex.Message}");
                LoadSampleTechnicalsData();
            }
        }

        private void RefreshSetupsDisplay()
        {
            // Skip refreshing if we're in the middle of an undo/redo operation
            if (_isUndoRedoAction)
            {
                return;
            }

            _setupsDataTable.Clear();
            
            // Add setups from temporary state
            foreach (var setup in _temporarySetups)
            {
                var row = _setupsDataTable.NewRow();
                foreach (var kvp in setup)
                {
                    if (_setupsDataTable.Columns.Contains(kvp.Key))
                    {
                        row[kvp.Key] = kvp.Value;
                    }
                }
                _setupsDataTable.Rows.Add(row);
            }
            
            // Refresh the Strategy ComboBox items in the trades grid
            // RefreshStrategyComboBoxItems(); // This method is being removed
            Console.WriteLine($"RefreshSetupsDisplay completed - {_temporarySetups.Count} setups displayed");
        }
        
        private async void LoadRecentTrades()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                
                // Skip merging if we're in the middle of an undo/redo operation
                if (_isUndoRedoAction)
                {
                    // Just refresh the display with current temporary trades
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
                            trade.Setup,
                            trade.Float,
                            trade.Catalyst,
                            trade.Technicals,
                            trade.ChartImagePath ?? "No Image"
                        );
                    }
                    return; // Exit early to avoid merging during undo/redo
                }
                
                // Always load from storage to get the latest data
                var storageTrades = await _tradingJournalService.GetRecentTrades(50, true);
                
                // If we have unsaved changes, merge with temporary state
                if (_hasUnsavedChanges)
                {
                    // Create a merged list starting with temporary trades (which include edits)
                    var mergedTrades = new List<TradeData>(_temporaryTrades);
                    
                    // Add storage trades that aren't already in temporary trades and aren't deleted
                    foreach (var storageTrade in storageTrades)
                    {
                        var isDeleted = _deletedTrades.Any(d => 
                            d.Symbol == storageTrade.Symbol && 
                            d.Date.Date == storageTrade.Date.Date &&
                            d.TradeSeq == storageTrade.TradeSeq);
                        
                        var alreadyInTemporary = _temporaryTrades.Any(t =>
                            t.Symbol == storageTrade.Symbol &&
                            t.Date.Date == storageTrade.Date.Date &&
                            t.TradeSeq == storageTrade.TradeSeq);
                        
                        if (!isDeleted && !alreadyInTemporary)
                        {
                            mergedTrades.Add(storageTrade);
                        }
                    }
                    
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
                        trade.Setup,
                        trade.Float,
                        trade.Catalyst,
                        trade.Technicals,
                        trade.ChartImagePath ?? "No Image"
                    );
                }
                
                // Refresh the Strategy ComboBox items after loading trades
                // RefreshStrategyComboBoxItems(); // This method is being removed
                
                // Update Examples columns in setups and technicals tabs
                UpdateSetupsExamplesFromTrades();
                UpdateTechnicalsExamplesFromTrades();
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
            btnGetOnlineData.Enabled = false;
            
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
                btnGetOnlineData.Enabled = true;
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

        private async void btnGetOnlineData_Click(object? sender, EventArgs e)
        {
            if (_isProcessing)
            {
                MessageBox.Show("Already processing. Please wait for the current operation to complete.", "Processing", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (dataGridViewTrades.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select one or more trades to get online data for.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                _isProcessing = true;
                btnAnalyzeChartImage.Enabled = false;
                btnAnalyzeChartFolder.Enabled = false;
                btnGetOnlineData.Enabled = false;
                btnRefresh.Enabled = false;

                var selectedTrades = new List<(int rowIndex, TradeData trade)>();
                
                // Get selected trades
                foreach (DataGridViewRow row in dataGridViewTrades.SelectedRows)
                {
                    var symbol = row.Cells["Symbol"].Value?.ToString();
                    var dateValue = row.Cells["Date"].Value;
                    
                    if (string.IsNullOrEmpty(symbol) || dateValue == null)
                        continue;

                    if (DateTime.TryParse(dateValue.ToString(), out var date))
                    {
                        var trade = _temporaryTrades.FirstOrDefault(t => 
                            t.Symbol == symbol && 
                            t.Date.Date == date.Date);
                        
                        if (trade != null)
                        {
                            selectedTrades.Add((row.Index, trade));
                        }
                    }
                }

                if (selectedTrades.Count == 0)
                {
                    MessageBox.Show("No valid trades selected for online data retrieval.", "No Valid Trades", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Process each selected trade
                for (int i = 0; i < selectedTrades.Count; i++)
                {
                    var (rowIndex, trade) = selectedTrades[i];
                    
                    try
                    {
                        lblStatus.Text = $"Getting online data for {trade.Symbol} ({i + 1}/{selectedTrades.Count})...";
                        Application.DoEvents();

                        var onlineData = await _tradingJournalService.GetOnlineDataForTrade(trade.Symbol, trade.Date);
                        
                        // Update the trade with online data
                        Console.WriteLine($"Updating trade {trade.Symbol} on {trade.Date:yyyy-MM-dd}:");
                        Console.WriteLine($"  Previous Close: {trade.PreviousDayClose} → {onlineData.PreviousDayClose}");
                        Console.WriteLine($"  Volume: {trade.Volume / 1000000m:F2}M → {onlineData.Volume:F2}M");
                        
                        trade.PreviousDayClose = onlineData.PreviousDayClose;
                        trade.Volume = (long)(onlineData.Volume * 1000000); // Convert millions to actual volume
                        
                        // Calculate gap percentages
                        if (onlineData.PreviousDayClose > 0)
                        {
                            var gapPercentToHigh = ((trade.HighAfterVolumeSurge - onlineData.PreviousDayClose) / onlineData.PreviousDayClose) * 100;
                            trade.GapPercentToHigh = Math.Round(gapPercentToHigh, 1);
                            Console.WriteLine($"  Gap % (Close to High): {trade.GapPercentToHigh}%");
                        }
                        
                        if (trade.HighAfterVolumeSurge > 0)
                        {
                            var gapPercentHighToLow = ((trade.LowAfterVolumeSurge - trade.HighAfterVolumeSurge) / trade.HighAfterVolumeSurge) * 100;
                            trade.GapPercentHighToLow = Math.Round(Math.Abs(gapPercentHighToLow), 1);
                            Console.WriteLine($"  Gap % (High to Low): {trade.GapPercentHighToLow}%");
                        }

                        // Update the display immediately
                        dataGridViewTrades.Rows[rowIndex].Cells["Previous Close"].Value = trade.PreviousDayClose;
                        dataGridViewTrades.Rows[rowIndex].Cells["Volume (M)"].Value = onlineData.Volume;
                        dataGridViewTrades.Rows[rowIndex].Cells["Gap % (Close to High)"].Value = trade.GapPercentToHigh;
                        dataGridViewTrades.Rows[rowIndex].Cells["Gap % (High to Low)"].Value = trade.GapPercentHighToLow;
                        
                        // Force the DataGridView to refresh the display
                        dataGridViewTrades.Refresh();
                        Application.DoEvents();

                        // Update status to show the trade was updated
                        lblStatus.Text = $"Updated {trade.Symbol} - Previous Close: {trade.PreviousDayClose}, Volume: {onlineData.Volume:F2}M";
                        Application.DoEvents();

                        // Mark as having unsaved changes
                        SetUnsavedChanges(true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting online data for {trade.Symbol}: {ex.Message}");
                        // Continue with next trade instead of stopping
                    }
                }

                lblStatus.Text = "Online data retrieval completed.";
                MessageBox.Show($"Successfully updated online data for {selectedTrades.Count} trade(s).", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting online data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isProcessing = false;
                btnAnalyzeChartImage.Enabled = true;
                btnAnalyzeChartFolder.Enabled = true;
                btnGetOnlineData.Enabled = true;
                btnRefresh.Enabled = true;
                lblStatus.Text = "Ready";
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

                // Add float data to the trade
                tradeData.Float = _floatDataService.GetFloat(tradeData.Symbol);
                Console.WriteLine($"Added float data for {tradeData.Symbol}: {tradeData.Float}");

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
                        
                        // Mark as having unsaved changes for immediate display
                        SetUnsavedChanges(true);
                        
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
            btnGetOnlineData.Enabled = false;
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
                        
                        // Mark as having unsaved changes for immediate display
                        SetUnsavedChanges(true);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing
                        Console.WriteLine($"Error processing {Path.GetFileName(imageFile)}: {ex.Message}");
                    }

                    processedCount++;
                }

                // Refresh the data table without merging (since we have unsaved changes)
                // Just refresh the display with current temporary trades
                _tradesDataTable.Clear();
                foreach (var trade in _temporaryTrades.OrderByDescending(t => t.Date).ThenByDescending(t => t.TradeSeq))
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
                        trade.Setup,
                        trade.Float,
                        trade.Catalyst,
                        trade.Technicals,
                        trade.ChartImagePath ?? "No Image"
                    );
                }

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
                btnGetOnlineData.Enabled = true;
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
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            
            var columnName = dataGridViewTrades.Columns[e.ColumnIndex].Name;
            Console.WriteLine($"CellClick: Column={columnName}, Row={e.RowIndex}");
            
            if (columnName == "Chart")
            {
                var imagePath = dataGridViewTrades.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                if (string.IsNullOrEmpty(imagePath) || imagePath == "No Image")
                {
                    MessageBox.Show("No chart image available for this trade.", "No Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                try
                {
                    if (File.Exists(imagePath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = imagePath,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        MessageBox.Show($"Image file not found: {imagePath}", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (columnName == "Strategy")
            {
                ShowStrategySelectionDialog(e.RowIndex);
            }
            else if (columnName == "Technicals")
            {
                ShowTechnicalSelectionDialog(e.RowIndex);
            }
        }

        private void ShowStrategySelectionDialog(int rowIndex)
        {
            try
            {
                // Get available strategies from setups
                var availableStrategies = _temporarySetups
                    .Select(s => s["Strategy"]?.ToString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

                if (!availableStrategies.Any())
                {
                    MessageBox.Show("No strategies available. Please add some strategies in the Setups tab first.", 
                        "No Strategies", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Create a simple form with ComboBox
                using var form = new Form
                {
                    Text = "Select Strategy",
                    Size = new Size(300, 150),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var comboBox = new ComboBox
                {
                    Location = new Point(20, 20),
                    Size = new Size(240, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Add empty option and available strategies
                comboBox.Items.Add("");
                comboBox.Items.AddRange(availableStrategies.ToArray());

                // Set current value if any
                var currentValue = dataGridViewTrades.Rows[rowIndex].Cells["Strategy"].Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(currentValue))
                {
                    comboBox.SelectedItem = currentValue;
                }
                else
                {
                    comboBox.SelectedIndex = 0; // Select empty option
                }

                var button = new Button
                {
                    Text = "OK",
                    Location = new Point(100, 60),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.OK
                };

                form.Controls.Add(comboBox);
                form.Controls.Add(button);
                form.AcceptButton = button;

                // Show dialog
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var selectedStrategy = comboBox.SelectedItem?.ToString() ?? "";
                    dataGridViewTrades.Rows[rowIndex].Cells["Strategy"].Value = selectedStrategy;
                    
                    // Update the trade in temporary storage
                    UpdateTradeInStorage(rowIndex);
                    
                    // Update Examples column in setups to reflect the strategy change
                    UpdateSetupsExamplesFromTrades();
                    
                    SetUnsavedChanges(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing strategy selection dialog: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowTechnicalSelectionDialog(int rowIndex)
        {
            try
            {
                // Get available technical types from technicals
                var availableTechnicals = _temporaryTechnicals
                    .Select(t => t["Type"]?.ToString() ?? "")
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                if (!availableTechnicals.Any())
                {
                    MessageBox.Show("No technical types available. Please add some technical types in the Technicals tab first.", 
                        "No Technicals", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Create a simple form with ComboBox
                using var form = new Form
                {
                    Text = "Select Technical Type",
                    Size = new Size(300, 150),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var comboBox = new ComboBox
                {
                    Location = new Point(20, 20),
                    Size = new Size(240, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Add empty option and available technical types
                comboBox.Items.Add("");
                comboBox.Items.AddRange(availableTechnicals.ToArray());

                // Set current value if any
                var currentValue = dataGridViewTrades.Rows[rowIndex].Cells["Technicals"].Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(currentValue))
                {
                    comboBox.SelectedItem = currentValue;
                }
                else
                {
                    comboBox.SelectedIndex = 0; // Select empty option
                }

                var button = new Button
                {
                    Text = "OK",
                    Location = new Point(100, 60),
                    Size = new Size(80, 30),
                    DialogResult = DialogResult.OK
                };

                form.Controls.Add(comboBox);
                form.Controls.Add(button);
                form.AcceptButton = button;

                // Show dialog
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var selectedTechnical = comboBox.SelectedItem?.ToString() ?? "";
                    dataGridViewTrades.Rows[rowIndex].Cells["Technicals"].Value = selectedTechnical;
                    
                    // Update the trade in temporary storage
                    UpdateTradeInStorage(rowIndex);
                    
                    // Update Examples column in technicals to reflect the technical change
                    UpdateTechnicalsExamplesFromTrades();
                    
                    SetUnsavedChanges(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing technical selection dialog: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            
            // Display float data information
            DisplayFloatDataInfo();
        }

        /// <summary>
        /// Displays information about the loaded float data
        /// </summary>
        private void DisplayFloatDataInfo()
        {
            try
            {
                var floatInfo = _floatDataService.GetCurrentFileInfo();
                Console.WriteLine($"Float Data Status: {floatInfo}");
                
                // Also show in status label if available
                if (lblStatus != null)
                {
                    lblStatus.Text = $"Ready - {floatInfo}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying float data info: {ex.Message}");
            }
        }

        private void DataGridViewTrades_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    var columnName = dataGridViewTrades.Columns[e.ColumnIndex].Name;
                    var newValue = dataGridViewTrades.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                    
                    Console.WriteLine($"CellEndEdit triggered: Column={columnName}, Row={e.RowIndex}, Value={newValue}");
                    
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
                    
                    // If Strategy column was edited, update Examples in setups
                    if (columnName == "Strategy")
                    {
                        UpdateSetupsExamplesFromTrades();
                    }
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

                        // Track deleted trade for image cleanup (but don't delete image yet - only when saving)
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
                        Setup = tradeToUpdate.Setup,
                        Float = tradeToUpdate.Float,
                        Catalyst = tradeToUpdate.Catalyst,
                        Technicals = tradeToUpdate.Technicals,
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
                    
                    // Update Strategy field
                    var strategyValue = dataGridViewTrades.Rows[rowIndex].Cells["Strategy"].Value?.ToString() ?? "";
                    tradeToUpdate.Setup = strategyValue;
                    Console.WriteLine($"Updated Strategy for trade {tradeToUpdate.Symbol} on {tradeToUpdate.Date:yyyy-MM-dd}: '{strategyValue}'");
                    
                    // Update Technicals field
                    var technicalsValue = dataGridViewTrades.Rows[rowIndex].Cells["Technicals"].Value?.ToString() ?? "";
                    tradeToUpdate.Technicals = technicalsValue;
                    Console.WriteLine($"Updated Technicals for trade {tradeToUpdate.Symbol} on {tradeToUpdate.Date:yyyy-MM-dd}: '{technicalsValue}'");
                    
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
                        Setup = tradeToUpdate.Setup,
                        Float = tradeToUpdate.Float,
                        Catalyst = tradeToUpdate.Catalyst,
                        Technicals = tradeToUpdate.Technicals,
                        ChartImagePath = tradeToUpdate.ChartImagePath
                    };

                    var undoAction = new EditTradeAction(originalTrade, modifiedTrade, _temporaryTrades);
                    AddUndoAction(undoAction);

                    // Update Examples columns in setups and technicals tabs
                    UpdateSetupsExamplesFromTrades();
                    UpdateTechnicalsExamplesFromTrades();

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

        private void CreateSetupsContextMenu()
        {
            _setupsContextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Delete Setup");
            deleteItem.Click += (sender, e) => DeleteSelectedSetupRow();
            _setupsContextMenu.Items.Add(deleteItem);
        }

        private void DeleteSelectedSetupRow()
        {
            if (dataGridViewSetups.SelectedRows.Count == 0) return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this setup?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var selectedRow = dataGridViewSetups.SelectedRows[0];
                    var rowIndex = selectedRow.Index;
                    
                    // Get the setup data before deletion
                    var setupToDelete = new Dictionary<string, object>();
                    for (int i = 0; i < _setupsDataTable.Columns.Count; i++)
                    {
                        var col = _setupsDataTable.Columns[i];
                        setupToDelete[col.ColumnName] = selectedRow.Cells[i].Value ?? "";
                    }
                    
                    // Get the strategy name before deletion
                    var deletedStrategyName = setupToDelete.ContainsKey("Strategy") ? setupToDelete["Strategy"]?.ToString() ?? "" : "";
                    
                    // Remove from temporary state
                    _temporarySetups.RemoveAt(rowIndex);
                    _deletedSetups.Add(setupToDelete);
                    
                    // Add undo action
                    var deleteAction = new DeleteSetupAction(setupToDelete, _temporarySetups, _deletedSetups);
                    AddUndoAction(deleteAction);
                    
                    // Clear strategy references in trades that were using this strategy
                    if (!string.IsNullOrEmpty(deletedStrategyName))
                    {
                        ClearTradesForDeletedStrategy(deletedStrategyName);
                    }
                    
                    // Refresh display
                    RefreshSetupsDisplay();
                    
                    // Mark as having unsaved changes
                    SetUnsavedChanges(true);

                    MessageBox.Show("Setup deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting setup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
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

        private void DataGridViewSetups_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            // Handle cell editing for setups
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var columnName = _setupsDataTable.Columns[e.ColumnIndex].ColumnName;
                var newValue = dataGridViewSetups.Rows[e.RowIndex].Cells[e.ColumnIndex].Value ?? "";
                
                // Update temporary state
                if (e.RowIndex < _temporarySetups.Count)
                {
                    var setup = _temporarySetups[e.RowIndex];
                    var oldValue = setup.ContainsKey(columnName) ? setup[columnName]?.ToString() ?? "" : "";
                    setup[columnName] = newValue;
                    
                    // If Strategy column was changed, update trades that reference this strategy
                    if (columnName == "Strategy" && oldValue != newValue.ToString())
                    {
                        UpdateTradesForStrategyChange(oldValue, newValue.ToString() ?? "");
                    }
                }
                
                SetUnsavedChanges(true);
            }
        }

        private void DataGridViewSetups_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                // Handle paste operation for setups
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete && dataGridViewSetups.SelectedRows.Count > 0)
            {
                DeleteSelectedSetupRow();
            }
        }

        private void DataGridViewSetups_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            // Handle data errors gracefully for setups
            e.ThrowException = false;
        }

        private void DataGridViewSetups_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridViewSetups.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    dataGridViewSetups.ClearSelection();
                    dataGridViewSetups.Rows[hit.RowIndex].Selected = true;
                    _setupsContextMenu.Show(dataGridViewSetups, e.Location);
                }
            }
        }

        private void UpdateTradesForStrategyChange(string oldStrategyName, string newStrategyName)
        {
            try
            {
                if (string.IsNullOrEmpty(oldStrategyName) || string.IsNullOrEmpty(newStrategyName))
                    return;

                Console.WriteLine($"Updating trades: '{oldStrategyName}' -> '{newStrategyName}'");
                
                // Update trades in temporary state
                var updatedTrades = 0;
                foreach (var trade in _temporaryTrades)
                {
                    if (trade.Setup == oldStrategyName)
                    {
                        trade.Setup = newStrategyName;
                        updatedTrades++;
                        Console.WriteLine($"Updated trade {trade.Symbol} {trade.Date:yyyy-MM-dd} strategy from '{oldStrategyName}' to '{newStrategyName}'");
                    }
                }
                
                // Update trades in deleted trades list as well
                foreach (var trade in _deletedTrades)
                {
                    if (trade.Setup == oldStrategyName)
                    {
                        trade.Setup = newStrategyName;
                        Console.WriteLine($"Updated deleted trade {trade.Symbol} {trade.Date:yyyy-MM-dd} strategy from '{oldStrategyName}' to '{newStrategyName}'");
                    }
                }
                
                // Update Examples column in setups to reflect the strategy name change
                UpdateSetupsExamplesFromTrades();
                
                // Refresh the trades display to show the updated strategy names
                if (updatedTrades > 0)
                {
                    LoadRecentTrades();
                    Console.WriteLine($"Updated {updatedTrades} trades for strategy name change");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating trades for strategy change: {ex.Message}");
            }
        }

        private void ClearTradesForDeletedStrategy(string deletedStrategyName)
        {
            try
            {
                if (string.IsNullOrEmpty(deletedStrategyName))
                    return;

                Console.WriteLine($"Clearing trades for deleted strategy: '{deletedStrategyName}'");
                
                // Clear strategy references in temporary state
                var clearedTrades = 0;
                foreach (var trade in _temporaryTrades)
                {
                    if (trade.Setup == deletedStrategyName)
                    {
                        trade.Setup = "";
                        clearedTrades++;
                        Console.WriteLine($"Cleared strategy for trade {trade.Symbol} {trade.Date:yyyy-MM-dd} (was '{deletedStrategyName}')");
                    }
                }
                
                // Clear strategy references in deleted trades list as well
                foreach (var trade in _deletedTrades)
                {
                    if (trade.Setup == deletedStrategyName)
                    {
                        trade.Setup = "";
                        Console.WriteLine($"Cleared strategy for deleted trade {trade.Symbol} {trade.Date:yyyy-MM-dd} (was '{deletedStrategyName}')");
                    }
                }
                
                // Update Examples column in setups to reflect the deleted strategy
                UpdateSetupsExamplesFromTrades();
                
                // Refresh the trades display to show the cleared strategy names
                if (clearedTrades > 0)
                {
                    LoadRecentTrades();
                    Console.WriteLine($"Cleared strategy for {clearedTrades} trades");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing trades for deleted strategy: {ex.Message}");
            }
        }

        private void UpdateTradesForTechnicalChange(string oldTechnicalName, string newTechnicalName)
        {
            try
            {
                if (string.IsNullOrEmpty(oldTechnicalName) || string.IsNullOrEmpty(newTechnicalName))
                    return;

                Console.WriteLine($"Updating trades: '{oldTechnicalName}' -> '{newTechnicalName}'");
                
                // Update trades in temporary state
                var updatedTrades = 0;
                foreach (var trade in _temporaryTrades)
                {
                    if (trade.Technicals == oldTechnicalName)
                    {
                        trade.Technicals = newTechnicalName;
                        updatedTrades++;
                        Console.WriteLine($"Updated trade {trade.Symbol} {trade.Date:yyyy-MM-dd} technical from '{oldTechnicalName}' to '{newTechnicalName}'");
                    }
                }
                
                // Update trades in deleted trades list as well
                foreach (var trade in _deletedTrades)
                {
                    if (trade.Technicals == oldTechnicalName)
                    {
                        trade.Technicals = newTechnicalName;
                        Console.WriteLine($"Updated deleted trade {trade.Symbol} {trade.Date:yyyy-MM-dd} technical from '{oldTechnicalName}' to '{newTechnicalName}'");
                    }
                }
                
                // Update Examples column in technicals to reflect the technical name change
                UpdateTechnicalsExamplesFromTrades();
                
                // Refresh the trades display to show the updated technical names
                if (updatedTrades > 0)
                {
                    LoadRecentTrades();
                    Console.WriteLine($"Updated {updatedTrades} trades for technical name change");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating trades for technical change: {ex.Message}");
            }
        }

        private void ClearTradesForDeletedTechnical(string deletedTechnicalName)
        {
            try
            {
                if (string.IsNullOrEmpty(deletedTechnicalName))
                    return;

                Console.WriteLine($"Clearing trades for deleted technical: '{deletedTechnicalName}'");
                
                // Clear technical references in temporary state
                var clearedTrades = 0;
                foreach (var trade in _temporaryTrades)
                {
                    if (trade.Technicals == deletedTechnicalName)
                    {
                        trade.Technicals = "";
                        clearedTrades++;
                        Console.WriteLine($"Cleared technical for trade {trade.Symbol} {trade.Date:yyyy-MM-dd} (was '{deletedTechnicalName}')");
                    }
                }
                
                // Clear technical references in deleted trades list as well
                foreach (var trade in _deletedTrades)
                {
                    if (trade.Technicals == deletedTechnicalName)
                    {
                        trade.Technicals = "";
                        Console.WriteLine($"Cleared technical for deleted trade {trade.Symbol} {trade.Date:yyyy-MM-dd} (was '{deletedTechnicalName}')");
                    }
                }
                
                // Update Examples column in technicals to reflect the deleted technical
                UpdateTechnicalsExamplesFromTrades();
                
                // Refresh the trades display to show the cleared technical names
                if (clearedTrades > 0)
                {
                    LoadRecentTrades();
                    Console.WriteLine($"Cleared technical for {clearedTrades} trades");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing trades for deleted technical: {ex.Message}");
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
            // The save button and unsaved changes label are now created in the Designer
            // and are globally accessible from the top panel
            btnSaveChanges.Click += BtnSaveChanges_Click;
        }

        private void SetUnsavedChanges(bool hasChanges)
        {
            _hasUnsavedChanges = hasChanges;
            btnSaveChanges.Visible = hasChanges;
            lblUnsavedChanges.Visible = hasChanges;
            
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
                    btnSaveChanges.Enabled = false;
                    btnSaveChanges.Text = "Saving...";

                    // Save all temporary trades to storage
                    await _localStorageService.SaveTrades(_temporaryTrades);

                    // Update Examples column in setups based on current trades
                    UpdateSetupsExamplesFromTrades();

                    // Clean up images for deleted trades (only when permanently saving)
                     if (_deletedTrades.Count > 0)
                     {
                         var imageStorageService = new ImageStorageService();
                         foreach (var deletedTrade in _deletedTrades)
                         {
                             if (!string.IsNullOrEmpty(deletedTrade.ChartImagePath))
                             {
                                 if (imageStorageService.DeleteImage(deletedTrade.ChartImagePath))
                                 {
                                     Console.WriteLine($"Permanently deleted image: {deletedTrade.ChartImagePath}");
                                 }
                                 else
                                 {
                                     Console.WriteLine($"Warning: Could not delete image: {deletedTrade.ChartImagePath}");
                                 }
                             }
                         }
                         _deletedTrades.Clear(); // Clear the deleted trades list
                     }

                    // Save setups data
                    await SaveSetupsData();
                    
                    // Save technicals data
                    await SaveTechnicalsData();
                    
                    // Clear deleted items after successful save
                    _deletedTrades.Clear();
                    _deletedSetups.Clear();
                    _deletedTechnicals.Clear();

                    // Clear undo/redo stacks since changes are now permanent
                    _undoStack.Clear();
                    _redoStack.Clear();
                    UpdateUndoRedoMenuItems();

                    // Reset unsaved changes state and reload from storage
                    SetUnsavedChanges(false);
                    
                    // Reload from storage to ensure consistency
                    LoadRecentTrades();
                    await LoadSetupsData(); // Reload setups data after saving

                    MessageBox.Show("Changes saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Cursor = Cursors.Default;
                    btnSaveChanges.Enabled = true;
                    btnSaveChanges.Text = "Save Changes";
                }
            }
        }

        /// <summary>
        /// Updates the Examples column in setups based on the current trades data.
        /// Aggregates all symbols for each strategy and updates the Examples column.
        /// </summary>
        private void UpdateSetupsExamplesFromTrades()
        {
            try
            {
                // Get all trades (including temporary and excluding deleted)
                var allTrades = _temporaryTrades.ToList();
                
                // Group trades by strategy
                var strategyGroups = allTrades
                    .Where(trade => !string.IsNullOrEmpty(trade.Setup))
                    .GroupBy(trade => trade.Setup)
                    .ToDictionary(g => g.Key, g => g.Select(t => t.Symbol).Distinct().ToList());

                // Update each setup's Examples column
                foreach (var setup in _temporarySetups)
                {
                    if (setup.ContainsKey("Strategy") && setup["Strategy"] != null)
                    {
                        string strategyName = setup["Strategy"].ToString() ?? "";
                        
                        if (strategyGroups.ContainsKey(strategyName))
                        {
                            // Join symbols with commas
                            string examples = string.Join(", ", strategyGroups[strategyName]);
                            setup["Examples"] = examples;
                        }
                        else
                        {
                            // No trades for this strategy, clear examples
                            setup["Examples"] = "";
                        }
                    }
                }

                // Refresh the setups display to show updated examples
                RefreshSetupsDisplay();
                
                Console.WriteLine($"Updated Examples column for {strategyGroups.Count} strategies");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating setups examples: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the Examples column in the technicals tab based on trades that use each technical type
        /// </summary>
        private void UpdateTechnicalsExamplesFromTrades()
        {
            try
            {
                // Get all trades (including temporary and excluding deleted)
                var allTrades = _temporaryTrades.ToList();
                
                // Group trades by technical type
                var technicalGroups = allTrades
                    .Where(trade => !string.IsNullOrEmpty(trade.Technicals))
                    .GroupBy(trade => trade.Technicals)
                    .ToDictionary(g => g.Key, g => g.Select(t => t.Symbol).Distinct().ToList());

                // Update each technical's Examples column
                foreach (var technical in _temporaryTechnicals)
                {
                    if (technical.ContainsKey("Type") && technical["Type"] != null)
                    {
                        string technicalType = technical["Type"].ToString() ?? "";
                        
                        if (technicalGroups.ContainsKey(technicalType))
                        {
                            // Join symbols with commas
                            string examples = string.Join(", ", technicalGroups[technicalType]);
                            technical["Examples"] = examples;
                        }
                        else
                        {
                            // No trades for this technical type, clear examples
                            technical["Examples"] = "";
                        }
                    }
                }

                // Refresh the technicals display to show updated examples
                RefreshTechnicalsDisplay();
                
                Console.WriteLine($"Updated Examples column for {technicalGroups.Count} technical types");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating technicals examples: {ex.Message}");
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
            try
            {
                using (var saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                    saveFileDialog.Title = "Export to Excel";
                    saveFileDialog.FileName = $"TradingJournal_{DateTime.Now:yyyy-MM-dd}.xlsx";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportToExcel(saveFileDialog.FileName);
                        MessageBox.Show($"Data exported successfully to {saveFileDialog.FileName}", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exitMenuItem_Click(object? sender, EventArgs e)
        {
            CloseApplication();
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
            // Check which tab is currently active
            if (tabControl.SelectedTab == tabPageTrades)
            {
                DeleteSelectedRow();
            }
            else if (tabControl.SelectedTab == tabPageSetups)
            {
                DeleteSelectedSetupRow();
            }
            else if (tabControl.SelectedTab == tabPageTechnicals)
            {
                DeleteSelectedTechnicalRow();
            }
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

        private void CloseApplication()
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Would you like to save them before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (result)
                {
                    case DialogResult.Yes:
                        // Save changes first, then close
                        SaveChangesAndClose();
                        break;
                    case DialogResult.No:
                        // Close without saving
                        Application.Exit();
                        break;
                    case DialogResult.Cancel:
                        // Cancel the close operation
                        return;
                }
            }
            else
            {
                // No unsaved changes, close directly
                Application.Exit();
            }
        }

        private async void SaveChangesAndClose()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                btnSaveChanges.Enabled = false;
                btnSaveChanges.Text = "Saving...";

                // Save all temporary trades to storage
                await _localStorageService.SaveTrades(_temporaryTrades);

                // Clean up images for deleted trades (only when permanently saving)
                if (_deletedTrades.Count > 0)
                {
                    var imageStorageService = new ImageStorageService();
                    foreach (var deletedTrade in _deletedTrades)
                    {
                        if (!string.IsNullOrEmpty(deletedTrade.ChartImagePath))
                        {
                            if (imageStorageService.DeleteImage(deletedTrade.ChartImagePath))
                            {
                                Console.WriteLine($"Permanently deleted image: {deletedTrade.ChartImagePath}");
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Could not delete image: {deletedTrade.ChartImagePath}");
                            }
                        }
                    }
                    _deletedTrades.Clear(); // Clear the deleted trades list
                }

                // Save setups data
                await SaveSetupsData();

                // Clear deleted setups list after saving
                _deletedSetups.Clear();

                // Clear undo/redo stacks since changes are now permanent
                _undoStack.Clear();
                _redoStack.Clear();
                UpdateUndoRedoMenuItems();

                // Reset unsaved changes state
                SetUnsavedChanges(false);

                MessageBox.Show("Changes saved successfully! Closing application.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Close the application
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving changes: {ex.Message}\n\nApplication will close without saving.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            finally
            {
                Cursor = Cursors.Default;
                btnSaveChanges.Enabled = true;
                btnSaveChanges.Text = "Save Changes";
            }
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // If there are unsaved changes, prompt the user
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Would you like to save them before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                switch (result)
                {
                    case DialogResult.Yes:
                        // Cancel the current close operation and save changes
                        e.Cancel = true;
                        SaveChangesAndClose();
                        break;
                    case DialogResult.No:
                        // Allow the close operation to proceed without saving
                        break;
                    case DialogResult.Cancel:
                        // Cancel the close operation
                        e.Cancel = true;
                        break;
                }
            }
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
                RefreshSetupsDisplay();
                RefreshTechnicalsDisplay();
                UpdateUndoRedoMenuItems();
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
                RefreshSetupsDisplay();
                RefreshTechnicalsDisplay();
                UpdateUndoRedoMenuItems();
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
            csv.AppendLine("Symbol,Date,Trade Seq,Previous Close,High After Volume Surge,Low After Volume Surge,Gap % (Close to High),Gap % (High to Low),Volume (M),Strategy,Float,Catalyst,Technicals");
            
            // Add data rows
            foreach (var row in trades)
            {
                csv.AppendLine($"{row["Symbol"]},{row["Date"]},{row["Trade Seq"]},{row["Previous Close"]},{row["High After Volume Surge"]},{row["Low After Volume Surge"]},{row["Gap % (Close to High)"]},{row["Gap % (High to Low)"]},{row["Volume (M)"]},{row["Strategy"]},{row["Float"]},{row["Catalyst"]},{row["Technicals"]}");
            }
            
            File.WriteAllText(filePath, csv.ToString());
        }

        private void ExportToExcel(string filePath)
        {
            // Set EPPlus license context for non-commercial use
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            
            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                // Export Trades data
                var tradesWorksheet = package.Workbook.Worksheets.Add("Trades");
                
                // Add headers for trades
                tradesWorksheet.Cells[1, 1].Value = "Symbol";
                tradesWorksheet.Cells[1, 2].Value = "Date";
                tradesWorksheet.Cells[1, 3].Value = "Trade Seq";
                tradesWorksheet.Cells[1, 4].Value = "Previous Close";
                tradesWorksheet.Cells[1, 5].Value = "High After Volume Surge";
                tradesWorksheet.Cells[1, 6].Value = "Low After Volume Surge";
                tradesWorksheet.Cells[1, 7].Value = "Gap % (Close to High)";
                tradesWorksheet.Cells[1, 8].Value = "Gap % (High to Low)";
                tradesWorksheet.Cells[1, 9].Value = "Volume (M)";
                tradesWorksheet.Cells[1, 10].Value = "Strategy";
                tradesWorksheet.Cells[1, 11].Value = "Float";
                tradesWorksheet.Cells[1, 12].Value = "Catalyst";
                tradesWorksheet.Cells[1, 13].Value = "Technicals";
                tradesWorksheet.Cells[1, 14].Value = "Chart Image Path";

                // Style the header row
                using (var range = tradesWorksheet.Cells[1, 1, 1, 14])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Add trades data
                int row = 2;
                foreach (var trade in _temporaryTrades)
                {
                    tradesWorksheet.Cells[row, 1].Value = trade.Symbol;
                    tradesWorksheet.Cells[row, 2].Value = trade.Date.ToString("yyyy-MM-dd");
                    tradesWorksheet.Cells[row, 3].Value = trade.TradeSeq;
                    tradesWorksheet.Cells[row, 4].Value = trade.PreviousDayClose;
                    tradesWorksheet.Cells[row, 5].Value = trade.HighAfterVolumeSurge;
                    tradesWorksheet.Cells[row, 6].Value = trade.LowAfterVolumeSurge;
                    tradesWorksheet.Cells[row, 7].Value = trade.GapPercentToHigh;
                    tradesWorksheet.Cells[row, 8].Value = trade.GapPercentHighToLow;
                    tradesWorksheet.Cells[row, 9].Value = trade.Volume / 1000000m;
                    tradesWorksheet.Cells[row, 10].Value = trade.Setup;
                    tradesWorksheet.Cells[row, 11].Value = trade.Float;
                    tradesWorksheet.Cells[row, 12].Value = trade.Catalyst;
                    tradesWorksheet.Cells[row, 13].Value = trade.Technicals;
                    tradesWorksheet.Cells[row, 14].Value = trade.ChartImagePath;
                    row++;
                }

                // Auto-fit columns for trades
                tradesWorksheet.Cells[tradesWorksheet.Dimension.Address].AutoFitColumns();

                // Export Setups data
                var setupsWorksheet = package.Workbook.Worksheets.Add("Setups");
                
                // Add headers for setups
                setupsWorksheet.Cells[1, 1].Value = "Strategy";
                setupsWorksheet.Cells[1, 2].Value = "Direction";
                setupsWorksheet.Cells[1, 3].Value = "Cycle";
                setupsWorksheet.Cells[1, 4].Value = "Meta Grade";
                setupsWorksheet.Cells[1, 5].Value = "Description";
                setupsWorksheet.Cells[1, 6].Value = "Pre-req";
                setupsWorksheet.Cells[1, 7].Value = "Ruin Variables";
                setupsWorksheet.Cells[1, 8].Value = "Entry (s)";
                setupsWorksheet.Cells[1, 9].Value = "Exit 1";
                setupsWorksheet.Cells[1, 10].Value = "Exit 2";
                setupsWorksheet.Cells[1, 11].Value = "Exit 3";
                setupsWorksheet.Cells[1, 12].Value = "Stop";
                setupsWorksheet.Cells[1, 13].Value = "Examples";

                // Style the header row for setups
                using (var range = setupsWorksheet.Cells[1, 1, 1, 13])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Add setups data
                row = 2;
                foreach (var setup in _temporarySetups)
                {
                    setupsWorksheet.Cells[row, 1].Value = setup.ContainsKey("Strategy") ? setup["Strategy"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 2].Value = setup.ContainsKey("Direction") ? setup["Direction"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 3].Value = setup.ContainsKey("Cycle") ? setup["Cycle"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 4].Value = setup.ContainsKey("Meta Grade") ? setup["Meta Grade"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 5].Value = setup.ContainsKey("Description") ? setup["Description"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 6].Value = setup.ContainsKey("Pre-req") ? setup["Pre-req"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 7].Value = setup.ContainsKey("Ruin Variables") ? setup["Ruin Variables"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 8].Value = setup.ContainsKey("Entry (s)") ? setup["Entry (s)"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 9].Value = setup.ContainsKey("Exit 1") ? setup["Exit 1"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 10].Value = setup.ContainsKey("Exit 2") ? setup["Exit 2"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 11].Value = setup.ContainsKey("Exit 3") ? setup["Exit 3"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 12].Value = setup.ContainsKey("Stop") ? setup["Stop"]?.ToString() : "";
                    setupsWorksheet.Cells[row, 13].Value = setup.ContainsKey("Examples") ? setup["Examples"]?.ToString() : "";
                    row++;
                }

                // Auto-fit columns for setups
                setupsWorksheet.Cells[setupsWorksheet.Dimension.Address].AutoFitColumns();

                // Save the Excel file
                package.SaveAs(new FileInfo(filePath));
            }
        }

        #endregion

        private void RefreshTechnicalsDisplay()
        {
            try
            {
                _technicalsDataTable.Clear();
                
                foreach (var technical in _temporaryTechnicals)
                {
                    _technicalsDataTable.Rows.Add(
                        technical.ContainsKey("Type") ? technical["Type"]?.ToString() ?? "" : "",
                        technical.ContainsKey("Description") ? technical["Description"]?.ToString() ?? "" : "",
                        technical.ContainsKey("Examples") ? technical["Examples"]?.ToString() ?? "" : ""
                    );
                }
                
                Console.WriteLine($"RefreshTechnicalsDisplay completed - {_temporaryTechnicals.Count} technicals displayed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing technicals display: {ex.Message}");
            }
        }

        private void CreateTechnicalsContextMenu()
        {
            _technicalsContextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("Delete Technical");
            deleteItem.Click += (sender, e) => DeleteSelectedTechnicalRow();
            _technicalsContextMenu.Items.Add(deleteItem);
        }

        private void DeleteSelectedTechnicalRow()
        {
            if (dataGridViewTechnicals.SelectedRows.Count == 0) return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this technical?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var selectedRow = dataGridViewTechnicals.SelectedRows[0];
                    var rowIndex = selectedRow.Index;
                    
                    // Get the technical data before deletion
                    var technicalToDelete = new Dictionary<string, object>();
                    for (int i = 0; i < _technicalsDataTable.Columns.Count; i++)
                    {
                        var col = _technicalsDataTable.Columns[i];
                        technicalToDelete[col.ColumnName] = selectedRow.Cells[i].Value ?? "";
                    }
                    
                    // Get the technical type name for clearing trades
                    var technicalTypeName = technicalToDelete.ContainsKey("Type") ? technicalToDelete["Type"]?.ToString() ?? "" : "";
                    
                    // Remove from temporary state
                    _temporaryTechnicals.RemoveAt(rowIndex);
                    _deletedTechnicals.Add(technicalToDelete);
                    
                    // Clear trades that reference this technical type
                    if (!string.IsNullOrEmpty(technicalTypeName))
                    {
                        ClearTradesForDeletedTechnical(technicalTypeName);
                    }
                    
                    // Add undo action
                    var deleteAction = new DeleteTechnicalAction(technicalToDelete, _temporaryTechnicals, _deletedTechnicals);
                    AddUndoAction(deleteAction);
                    
                    // Refresh display
                    RefreshTechnicalsDisplay();
                    
                    // Mark as having unsaved changes
                    SetUnsavedChanges(true);

                    MessageBox.Show("Technical deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting technical: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DataGridViewTechnicals_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < _temporaryTechnicals.Count)
                {
                    var technical = _temporaryTechnicals[e.RowIndex];
                    var columnName = dataGridViewTechnicals.Columns[e.ColumnIndex].Name;
                    var newValue = dataGridViewTechnicals.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    var oldValue = technical.ContainsKey(columnName) ? technical[columnName]?.ToString() ?? "" : "";
                    
                    if (technical.ContainsKey(columnName) && technical[columnName]?.ToString() != newValue)
                    {
                        technical[columnName] = newValue;
                        
                        // If Type column was changed, update trades that reference this technical type
                        if (columnName == "Type" && oldValue != newValue)
                        {
                            UpdateTradesForTechnicalChange(oldValue, newValue);
                        }
                        
                        SetUnsavedChanges(true);
                        Console.WriteLine($"Updated technical {columnName} to: {newValue}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DataGridViewTechnicals_CellEndEdit: {ex.Message}");
            }
        }

        private void DataGridViewTechnicals_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedTechnicalRow();
                e.Handled = true;
            }
        }

        private void DataGridViewTechnicals_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            Console.WriteLine($"DataGridViewTechnicals data error: {e.Exception.Message}");
        }

        private void DataGridViewTechnicals_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var hit = dataGridViewTechnicals.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    dataGridViewTechnicals.ClearSelection();
                    dataGridViewTechnicals.Rows[hit.RowIndex].Selected = true;
                    _technicalsContextMenu.Show(dataGridViewTechnicals, e.Location);
                }
            }
        }
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
                // Check if trade already exists in temporary trades to prevent duplicates
                var existingTrade = _temporaryTrades.FirstOrDefault(t =>
                    t.Symbol == _deletedTrade.Symbol &&
                    t.Date.Date == _deletedTrade.Date.Date &&
                    t.TradeSeq == _deletedTrade.TradeSeq);

                if (existingTrade == null)
                {
                    _temporaryTrades.Add(_deletedTrade);
                    _deletedTrades.Remove(_deletedTrade); // Remove from deleted trades list
                    Console.WriteLine($"Undo: Restored trade {_deletedTrade.Symbol} on {_deletedTrade.Date:yyyy-MM-dd} (image file preserved)");
                }
                else
                {
                    Console.WriteLine($"Undo: Trade {_deletedTrade.Symbol} already exists in temporary state, skipping");
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
                var tradeToDelete = _temporaryTrades.FirstOrDefault(t =>
                    t.Symbol == _deletedTrade.Symbol &&
                    t.Date.Date == _deletedTrade.Date.Date &&
                    t.TradeSeq == _deletedTrade.TradeSeq);

                if (tradeToDelete != null)
                {
                    _temporaryTrades.Remove(tradeToDelete);
                    _deletedTrades.Add(tradeToDelete); // Add the actual trade found, not _deletedTrade
                    Console.WriteLine($"Redo: Deleted trade {tradeToDelete.Symbol} on {tradeToDelete.Date:yyyy-MM-dd}");
                }
                else
                {
                    Console.WriteLine($"Redo: Trade {_deletedTrade.Symbol} not found in temporary state");
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
                    tradeToRestore.Setup = _originalTrade.Setup;
                    tradeToRestore.Float = _originalTrade.Float;
                    tradeToRestore.Catalyst = _originalTrade.Catalyst;
                    tradeToRestore.Technicals = _originalTrade.Technicals;

                    Console.WriteLine($"Undo: Restored original values for trade {_originalTrade.Symbol}");
                }
                else
                {
                    Console.WriteLine($"Undo: Trade {_modifiedTrade.Symbol} not found in temporary state");
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
                    tradeToModify.Setup = _modifiedTrade.Setup;
                    tradeToModify.Float = _modifiedTrade.Float;
                    tradeToModify.Catalyst = _modifiedTrade.Catalyst;
                    tradeToModify.Technicals = _modifiedTrade.Technicals;

                    Console.WriteLine($"Redo: Applied modified values for trade {_modifiedTrade.Symbol}");
                }
                else
                {
                    Console.WriteLine($"Redo: Trade {_originalTrade.Symbol} not found in temporary state");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during redo: {ex.Message}");
            }
        }
    }

    public class DeleteSetupAction : UndoRedoAction
    {
        private readonly Dictionary<string, object> _deletedSetup;
        private readonly List<Dictionary<string, object>> _temporarySetups;
        private readonly List<Dictionary<string, object>> _deletedSetups;

        public DeleteSetupAction(Dictionary<string, object> deletedSetup, List<Dictionary<string, object>> temporarySetups, List<Dictionary<string, object>> deletedSetups)
        {
            _deletedSetup = deletedSetup;
            _temporarySetups = temporarySetups;
            _deletedSetups = deletedSetups;
        }

        public override void Undo()
        {
            try
            {
                // Check if setup already exists in temporary setups to prevent duplicates
                var existingSetup = _temporarySetups.FirstOrDefault(s =>
                    s["Strategy"]?.ToString() == _deletedSetup["Strategy"]?.ToString() &&
                    s["Direction"]?.ToString() == _deletedSetup["Direction"]?.ToString() &&
                    s["Cycle"]?.ToString() == _deletedSetup["Cycle"]?.ToString() &&
                    s["Meta Grade"]?.ToString() == _deletedSetup["Meta Grade"]?.ToString() &&
                    s["Description"]?.ToString() == _deletedSetup["Description"]?.ToString() &&
                    s["Pre-req"]?.ToString() == _deletedSetup["Pre-req"]?.ToString() &&
                    s["Ruin Variables"]?.ToString() == _deletedSetup["Ruin Variables"]?.ToString() &&
                    s["Entry (s)"]?.ToString() == _deletedSetup["Entry (s)"]?.ToString() &&
                    s["Exit 1"]?.ToString() == _deletedSetup["Exit 1"]?.ToString() &&
                    s["Exit 2"]?.ToString() == _deletedSetup["Exit 2"]?.ToString() &&
                    s["Exit 3"]?.ToString() == _deletedSetup["Exit 3"]?.ToString() &&
                    s["Stop"]?.ToString() == _deletedSetup["Stop"]?.ToString() &&
                    s["Examples"]?.ToString() == _deletedSetup["Examples"]?.ToString());

                if (existingSetup == null)
                {
                    _temporarySetups.Add(_deletedSetup);
                    _deletedSetups.Remove(_deletedSetup); // Remove from deleted setups list
                    Console.WriteLine($"Undo: Restored setup {_deletedSetup["Strategy"]} (Strategy)");
                }
                else
                {
                    Console.WriteLine($"Undo: Setup {_deletedSetup["Strategy"]} already exists in temporary state, skipping");
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
                var setupToDelete = _temporarySetups.FirstOrDefault(s =>
                    s["Strategy"]?.ToString() == _deletedSetup["Strategy"]?.ToString() &&
                    s["Direction"]?.ToString() == _deletedSetup["Direction"]?.ToString() &&
                    s["Cycle"]?.ToString() == _deletedSetup["Cycle"]?.ToString() &&
                    s["Meta Grade"]?.ToString() == _deletedSetup["Meta Grade"]?.ToString() &&
                    s["Description"]?.ToString() == _deletedSetup["Description"]?.ToString() &&
                    s["Pre-req"]?.ToString() == _deletedSetup["Pre-req"]?.ToString() &&
                    s["Ruin Variables"]?.ToString() == _deletedSetup["Ruin Variables"]?.ToString() &&
                    s["Entry (s)"]?.ToString() == _deletedSetup["Entry (s)"]?.ToString() &&
                    s["Exit 1"]?.ToString() == _deletedSetup["Exit 1"]?.ToString() &&
                    s["Exit 2"]?.ToString() == _deletedSetup["Exit 2"]?.ToString() &&
                    s["Exit 3"]?.ToString() == _deletedSetup["Exit 3"]?.ToString() &&
                    s["Stop"]?.ToString() == _deletedSetup["Stop"]?.ToString() &&
                    s["Examples"]?.ToString() == _deletedSetup["Examples"]?.ToString());

                if (setupToDelete != null)
                {
                    _temporarySetups.Remove(setupToDelete);
                    _deletedSetups.Add(setupToDelete); // Add the actual setup found, not _deletedSetup
                    Console.WriteLine($"Redo: Deleted setup {setupToDelete["Strategy"]} (Strategy)");
                }
                else
                {
                    Console.WriteLine($"Redo: Setup {_deletedSetup["Strategy"]} not found in temporary state");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during redo: {ex.Message}");
            }
        }
    }

    public class DeleteTechnicalAction : UndoRedoAction
    {
        private readonly Dictionary<string, object> _deletedTechnical;
        private readonly List<Dictionary<string, object>> _temporaryTechnicals;
        private readonly List<Dictionary<string, object>> _deletedTechnicals;

        public DeleteTechnicalAction(Dictionary<string, object> deletedTechnical, List<Dictionary<string, object>> temporaryTechnicals, List<Dictionary<string, object>> deletedTechnicals)
        {
            _deletedTechnical = deletedTechnical;
            _temporaryTechnicals = temporaryTechnicals;
            _deletedTechnicals = deletedTechnicals;
        }

        public override void Undo()
        {
            try
            {
                // Check if technical already exists in temporary technicals to prevent duplicates
                var existingTechnical = _temporaryTechnicals.FirstOrDefault(t =>
                    t.ContainsKey("Type") && _deletedTechnical.ContainsKey("Type") &&
                    t["Type"]?.ToString() == _deletedTechnical["Type"]?.ToString());

                if (existingTechnical == null)
                {
                    _temporaryTechnicals.Add(_deletedTechnical);
                    _deletedTechnicals.Remove(_deletedTechnical);
                    Console.WriteLine($"Undo: Restored technical {_deletedTechnical["Type"]}");
                }
                else
                {
                    Console.WriteLine($"Undo: Technical {_deletedTechnical["Type"]} already exists in temporary state, skipping");
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
                var technicalToDelete = _temporaryTechnicals.FirstOrDefault(t =>
                    t.ContainsKey("Type") && _deletedTechnical.ContainsKey("Type") &&
                    t["Type"]?.ToString() == _deletedTechnical["Type"]?.ToString());

                if (technicalToDelete != null)
                {
                    _temporaryTechnicals.Remove(technicalToDelete);
                    _deletedTechnicals.Add(technicalToDelete);
                    Console.WriteLine($"Redo: Deleted technical {technicalToDelete["Type"]}");
                }
                else
                {
                    Console.WriteLine($"Redo: Technical {_deletedTechnical["Type"]} not found in temporary state");
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