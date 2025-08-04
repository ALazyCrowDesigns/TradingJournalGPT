using TradingJournalGPT.Utils;

namespace TradingJournalGPT
{
    public static class SetupGoogleSheets
    {
        public static void RunGoogleSheetsSetup()
        {
            Console.WriteLine("=== Google Sheets Setup Guide ===");
            Console.WriteLine();
            
            Console.WriteLine("Follow these steps to set up Google Sheets:");
            Console.WriteLine();
            
            Console.WriteLine("1. CREATE A GOOGLE SPREADSHEET:");
            Console.WriteLine("   - Go to https://sheets.google.com/");
            Console.WriteLine("   - Click 'Blank' to create a new spreadsheet");
            Console.WriteLine("   - Name it 'Trading Journal' or similar");
            Console.WriteLine();
            
            Console.WriteLine("2. GET THE SPREADSHEET ID:");
            Console.WriteLine("   - Look at the URL in your browser");
            Console.WriteLine("   - It will look like: https://docs.google.com/spreadsheets/d/SPREADSHEET_ID/edit");
            Console.WriteLine("   - Copy the SPREADSHEET_ID (the long string between /d/ and /edit)");
            Console.WriteLine();
            
            Console.WriteLine("3. SHARE THE SPREADSHEET:");
            Console.WriteLine("   - Click the 'Share' button (top right)");
            Console.WriteLine("   - Click 'Add people and groups'");
            Console.WriteLine("   - Add this email: tradingjournalbot@long-equinox-467916-v9.iam.gserviceaccount.com");
            Console.WriteLine("   - Give it 'Editor' permissions");
            Console.WriteLine("   - Click 'Send'");
            Console.WriteLine();
            
            Console.WriteLine("4. UPDATE THE CONFIGURATION:");
            Console.WriteLine("   - Open appsettings.json");
            Console.WriteLine("   - Replace 'your-google-spreadsheet-id' with your actual Spreadsheet ID");
            Console.WriteLine();
            
            Console.WriteLine("Example appsettings.json:");
            Console.WriteLine("{");
            Console.WriteLine("  \"OpenAiApiKey\": \"your-openai-api-key\",");
            Console.WriteLine("  \"GoogleCredentialsPath\": \"long-equinox-467916-v9-3f383fa4f833.json\",");
            Console.WriteLine("  \"GoogleSpreadsheetId\": \"1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms\",");
            Console.WriteLine("  \"GoogleSheetName\": \"Trades\"");
            Console.WriteLine("}");
            Console.WriteLine();
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
} 