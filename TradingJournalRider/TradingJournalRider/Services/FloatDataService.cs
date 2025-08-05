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
                Console.WriteLine($"FloatDataService: Looking for float data in directory: {_floatDataDirectory}");
                
                if (!Directory.Exists(_floatDataDirectory))
                {
                    Console.WriteLine($"FloatDataService: Directory not found: {_floatDataDirectory}");
                    return;
                }

                // Find the most recent float data file by modification date
                var floatFiles = Directory.GetFiles(_floatDataDirectory, "float_*.csv");
                Console.WriteLine($"FloatDataService: Found {floatFiles.Length} float files");
                
                if (!floatFiles.Any())
                {
                    Console.WriteLine("FloatDataService: No float data files found");
                    return;
                }

                // Get the most recently modified file
                var latestFile = floatFiles
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .First();
                
                var lastWriteTime = File.GetLastWriteTime(latestFile);
                Console.WriteLine($"FloatDataService: Loading float data from: {latestFile} (Last modified: {lastWriteTime:yyyy-MM-dd HH:mm:ss})");

                await LoadFloatDataAsync(latestFile);
                _isInitialized = true;
                Console.WriteLine($"FloatDataService: Successfully initialized with {_floatCache.Count} symbols");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FloatDataService: Error initializing float data service: {ex.Message}");
                Console.WriteLine($"FloatDataService: Stack trace: {ex.StackTrace}");
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
                Console.WriteLine($"FloatDataService: Reading {lines.Length} lines from CSV file");
                
                // Skip header line
                int loadedCount = 0;
                int errorCount = 0;
                
                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var parts = line.Split(',');
                    
                    if (parts.Length >= 15) // Ensure we have enough columns (symbol at 12, latestFloat at 14)
                    {
                        var symbol = parts[12].Trim().ToUpper(); // symbol column
                        if (decimal.TryParse(parts[14].Trim(), out decimal floatValue)) // latestFloat column
                        {
                            _floatCache[symbol] = floatValue;
                            loadedCount++;
                            
                            // Log every 1000 symbols for progress tracking
                            if (loadedCount % 1000 == 0)
                            {
                                Console.WriteLine($"FloatDataService: Loaded {loadedCount} symbols so far...");
                            }
                        }
                        else
                        {
                            errorCount++;
                            if (errorCount <= 10) // Only log first 10 errors to avoid spam
                            {
                                Console.WriteLine($"FloatDataService: Failed to parse float value for symbol {symbol}: '{parts[14]}'");
                            }
                        }
                    }
                    else
                    {
                        errorCount++;
                        if (errorCount <= 10) // Only log first 10 errors to avoid spam
                        {
                            Console.WriteLine($"FloatDataService: Line {i} has insufficient columns: {parts.Length} (expected >= 15)");
                        }
                    }
                }

                Console.WriteLine($"FloatDataService: Successfully loaded float data for {loadedCount} symbols");
                if (errorCount > 0)
                {
                    Console.WriteLine($"FloatDataService: Encountered {errorCount} parsing errors");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FloatDataService: Error loading float data: {ex.Message}");
                Console.WriteLine($"FloatDataService: Stack trace: {ex.StackTrace}");
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
            var found = _floatCache.TryGetValue(upperSymbol, out decimal floatValue);
            
            if (found)
            {
                Console.WriteLine($"FloatDataService: Found float for {upperSymbol}: {floatValue}");
            }
            else
            {
                Console.WriteLine($"FloatDataService: No float data found for symbol: {upperSymbol}");
            }
            
            return found ? floatValue : 0;
        }

        /// <summary>
        /// Tests float access for a specific symbol and provides detailed information
        /// </summary>
        /// <param name="symbol">The stock symbol to test</param>
        /// <returns>Detailed information about the float lookup</returns>
        public string TestFloatAccess(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return "Error: Symbol is null or empty";

            var upperSymbol = symbol.Trim().ToUpper();
            var found = _floatCache.TryGetValue(upperSymbol, out decimal floatValue);
            
            var result = $"FloatDataService Test for '{symbol}':\n";
            result += $"  - Normalized symbol: '{upperSymbol}'\n";
            result += $"  - Found in cache: {found}\n";
            result += $"  - Float value: {floatValue}\n";
            result += $"  - Total symbols in cache: {_floatCache.Count}\n";
            
            if (!found)
            {
                // Show some similar symbols for debugging
                var similarSymbols = _floatCache.Keys
                    .Where(k => k.Contains(upperSymbol) || upperSymbol.Contains(k))
                    .Take(5)
                    .ToList();
                
                if (similarSymbols.Any())
                {
                    result += $"  - Similar symbols found: {string.Join(", ", similarSymbols)}\n";
                }
            }
            
            return result;
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