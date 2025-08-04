using System.Text.Json;

namespace TradingJournalGPT.Utils
{
    public class AppConfiguration
    {
        public string OpenAiApiKey { get; set; } = string.Empty;
        public string GoogleCredentialsPath { get; set; } = string.Empty;
        public string GoogleSpreadsheetId { get; set; } = string.Empty;
        public string GoogleSheetName { get; set; } = "Trades";
    }

    public static class ConfigurationManager
    {
        private static AppConfiguration? _configuration;
        private static readonly string ConfigFilePath = "appsettings.json";

        public static AppConfiguration GetConfiguration()
        {
            if (_configuration != null)
                return _configuration;

            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var jsonContent = File.ReadAllText(ConfigFilePath);
                    _configuration = JsonSerializer.Deserialize<AppConfiguration>(jsonContent);
                }
                else
                {
                    // Create default configuration
                    _configuration = new AppConfiguration();
                    SaveConfiguration(_configuration);
                    
                    Console.WriteLine("Configuration file created. Please update appsettings.json with your API keys and settings.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    Environment.Exit(0);
                }

                return _configuration ?? new AppConfiguration();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return new AppConfiguration();
            }
        }

        public static void SaveConfiguration(AppConfiguration config)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(ConfigFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        public static void CreateDefaultConfiguration()
        {
            var config = new AppConfiguration
            {
                OpenAiApiKey = "your-openai-api-key-here",
                GoogleCredentialsPath = "path-to-your-google-credentials.json",
                GoogleSpreadsheetId = "your-google-spreadsheet-id",
                GoogleSheetName = "Trades"
            };

            SaveConfiguration(config);
        }

        public static bool ValidateConfiguration(AppConfiguration config)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(config.OpenAiApiKey) || config.OpenAiApiKey == "your-openai-api-key-here")
                errors.Add("OpenAI API key is not configured");

            if (string.IsNullOrEmpty(config.GoogleCredentialsPath) || config.GoogleCredentialsPath == "path-to-your-google-credentials.json")
                errors.Add("Google credentials path is not configured");

            if (string.IsNullOrEmpty(config.GoogleSpreadsheetId) || config.GoogleSpreadsheetId == "your-google-spreadsheet-id")
                errors.Add("Google Spreadsheet ID is not configured");

            if (errors.Any())
            {
                Console.WriteLine("Configuration errors found:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"- {error}");
                }
                return false;
            }

            return true;
        }
    }
} 