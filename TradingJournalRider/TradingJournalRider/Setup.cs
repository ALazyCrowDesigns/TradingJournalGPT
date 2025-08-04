using TradingJournalGPT.Utils;

namespace TradingJournalGPT
{
    public static class Setup
    {
        public static void RunSetup()
        {
            Console.WriteLine("=== Trading Journal GPT Setup ===");
            Console.WriteLine();
            
            // Create default configuration
            ConfigurationManager.CreateDefaultConfiguration();
            
            Console.WriteLine("Configuration file 'appsettings.json' has been created.");
            Console.WriteLine();
            Console.WriteLine("Please update the following settings:");
            Console.WriteLine();
            Console.WriteLine("1. OpenAI API Key:");
            Console.WriteLine("   - Go to https://platform.openai.com/");
            Console.WriteLine("   - Create an account and get your API key");
            Console.WriteLine("   - Replace 'your-openai-api-key-here' in appsettings.json");
            Console.WriteLine();
            Console.WriteLine("2. Google Sheets Setup:");
            Console.WriteLine("   - Go to https://console.cloud.google.com/");
            Console.WriteLine("   - Create a new project");
            Console.WriteLine("   - Enable Google Sheets API");
            Console.WriteLine("   - Create a Service Account and download the JSON file");
            Console.WriteLine("   - Create a Google Spreadsheet and share it with the service account email");
            Console.WriteLine("   - Update the paths and IDs in appsettings.json");
            Console.WriteLine();
            Console.WriteLine("3. Update appsettings.json with:");
            Console.WriteLine("   - Your OpenAI API key");
            Console.WriteLine("   - Path to your Google credentials JSON file");
            Console.WriteLine("   - Your Google Spreadsheet ID");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
} 