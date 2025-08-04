using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TradingJournalGPT.Services
{
    public class FloatDataService
    {
        private readonly Dictionary<string, decimal> _floatCache = new Dictionary<string, decimal>();
        private readonly string _floatDataDirectory;
        private bool _isInitialized = false;

        public FloatDataService()
        {
            var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            _floatDataDirectory = Path.Combine(appDirectory, "Data", "FloatData");
        }

        /// <summary>
        /// Initializes the float data service by loading the most recent float data file
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                if (!Directory.Exists(_floatDataDirectory))
                {
                    Console.WriteLine($"Float data directory not found: {_floatDataDirectory}");
                    return;
                }

                // Find the most recent float data file by modification date
                var floatFiles = Directory.GetFiles(_floatDataDirectory, "float_*.csv");
                if (!floatFiles.Any())
                {
                    Console.WriteLine("No float data files found");
                    return;
                }

                // Get the most recently modified file
                var latestFile = floatFiles
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .First();
                
                var lastWriteTime = File.GetLastWriteTime(latestFile);
                Console.WriteLine($"Loading float data from: {latestFile} (Last modified: {lastWriteTime:yyyy-MM-dd HH:mm:ss})");

                await LoadFloatDataAsync(latestFile);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing float data service: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads float data from the specified CSV file
        /// </summary>
        private async Task LoadFloatDataAsync(string filePath)
        {
            try
            {
                _floatCache.Clear();
                var lines = await File.ReadAllLinesAsync(filePath);
                
                // Skip header line
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var parts = line.Split(',');
                    
                    if (parts.Length >= 14) // Ensure we have enough columns
                    {
                        var symbol = parts[12].Trim().ToUpper(); // symbol column
                        if (decimal.TryParse(parts[14].Trim(), out decimal floatValue)) // latestFloat column
                        {
                            _floatCache[symbol] = floatValue;
                        }
                    }
                }

                Console.WriteLine($"Loaded float data for {_floatCache.Count} symbols");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading float data: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the float value for a given symbol
        /// </summary>
        /// <param name="symbol">The stock symbol (case-insensitive)</param>
        /// <returns>The float value, or 0 if not found</returns>
        public decimal GetFloat(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return 0;

            var upperSymbol = symbol.Trim().ToUpper();
            return _floatCache.TryGetValue(upperSymbol, out decimal floatValue) ? floatValue : 0;
        }

        /// <summary>
        /// Checks if a symbol has float data available
        /// </summary>
        /// <param name="symbol">The stock symbol (case-insensitive)</param>
        /// <returns>True if float data is available for the symbol</returns>
        public bool HasFloatData(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return false;

            var upperSymbol = symbol.Trim().ToUpper();
            return _floatCache.ContainsKey(upperSymbol);
        }

        /// <summary>
        /// Gets the total number of symbols with float data
        /// </summary>
        public int GetSymbolCount()
        {
            return _floatCache.Count;
        }

        /// <summary>
        /// Gets all available symbols
        /// </summary>
        public IEnumerable<string> GetAllSymbols()
        {
            return _floatCache.Keys.OrderBy(k => k);
        }

        /// <summary>
        /// Refreshes the float data by reloading from the most recent file
        /// </summary>
        public async Task RefreshFloatDataAsync()
        {
            _isInitialized = false;
            await InitializeAsync();
        }

        /// <summary>
        /// Gets information about the currently loaded float data file
        /// </summary>
        public string GetCurrentFileInfo()
        {
            if (!_isInitialized)
                return "Float data not initialized";

            var floatFiles = Directory.GetFiles(_floatDataDirectory, "float_*.csv");
            if (!floatFiles.Any())
                return "No float data files found";

            var latestFile = floatFiles
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .First();
            
            var lastWriteTime = File.GetLastWriteTime(latestFile);
            var fileSize = new FileInfo(latestFile).Length;
            
            return $"File: {Path.GetFileName(latestFile)}, Modified: {lastWriteTime:yyyy-MM-dd HH:mm:ss}, Size: {fileSize:N0} bytes, Symbols: {_floatCache.Count}";
        }
    }
} 