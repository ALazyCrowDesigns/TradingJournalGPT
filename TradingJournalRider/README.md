# Automatic Trading Journal GPT

A C# console application that automatically analyzes trading chart images using ChatGPT and records trade data to Google Sheets.

## Features

- **Chart Image Analysis**: Upload chart images to ChatGPT for automatic trade data extraction
- **Google Sheets Integration**: Automatically record trades to Google Sheets
- **Trade Data Management**: Store and retrieve trade information including entry/exit prices, dates, P&L, etc.
- **Trading Statistics**: Calculate win rates, total P&L, and other trading metrics
- **Future Expansion**: Ready for integration with Dilution Tracker and volume data

## Prerequisites

1. **OpenAI API Key**: Get your API key from [OpenAI Platform](https://platform.openai.com/)
2. **Google Sheets API**: Set up Google Cloud project and enable Google Sheets API
3. **Google Service Account**: Create a service account and download the credentials JSON file
4. **Google Spreadsheet**: Create a Google Spreadsheet and share it with your service account email

## Setup Instructions

### 1. Clone and Build the Project

```bash
git clone <repository-url>
cd TradingJournalRider
dotnet build
```

### 2. Configure API Keys and Settings

The application will create an `appsettings.json` file on first run. Update it with your credentials:

```json
{
  "OpenAiApiKey": "your-openai-api-key-here",
  "GoogleCredentialsPath": "path-to-your-google-credentials.json",
  "GoogleSpreadsheetId": "your-google-spreadsheet-id",
  "GoogleSheetName": "Trades"
}
```

#### Getting Your OpenAI API Key:
1. Go to [OpenAI Platform](https://platform.openai.com/)
2. Sign up or log in
3. Go to API Keys section
4. Create a new API key
5. Copy the key and paste it in the configuration

#### Setting Up Google Sheets API:
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing one
3. Enable Google Sheets API
4. Create a Service Account:
   - Go to IAM & Admin > Service Accounts
   - Click "Create Service Account"
   - Download the JSON credentials file
5. Create a Google Spreadsheet:
   - Go to [Google Sheets](https://sheets.google.com/)
   - Create a new spreadsheet
   - Share it with your service account email (found in the JSON file)
   - Copy the Spreadsheet ID from the URL

### 3. Run the Application

```bash
dotnet run
```

## Usage

### 1. Analyze Chart Image and Record Trade

1. Choose option `1` from the main menu
2. Enter the path to your chart image file
3. The application will:
   - Upload the image to ChatGPT for analysis
   - Extract trade information (symbol, prices, dates, etc.)
   - Display the extracted data
   - Ask for confirmation to record the trade
4. Confirm to save the trade to Google Sheets

### 2. View Recent Trades

1. Choose option `2` from the main menu
2. View your recent trades with P&L and other details

## Chart Image Requirements

For best results, your chart images should include:
- Stock symbol clearly visible
- Entry and exit prices marked
- Entry and exit dates visible
- Position size information
- Clear indication of long/short trade

## Data Structure

The application records the following trade data:
- **Recorded Date**: When the trade was recorded
- **Symbol**: Stock symbol
- **Entry Price**: Entry price of the trade
- **Exit Price**: Exit price of the trade
- **Entry Date**: Date of entry
- **Exit Date**: Date of exit
- **Position Size**: Number of shares
- **Trade Type**: Long or Short
- **P&L**: Profit/Loss calculation
- **Float**: Float information (for future integration)
- **Volume**: Trading volume (for future integration)
- **Analysis**: ChatGPT's analysis of the trade
- **Chart Image Path**: Path to the original chart image
- **Notes**: Additional notes

## Future Enhancements

The application is designed for easy expansion:

1. **Dilution Tracker Integration**: Automatically fetch float data
2. **Volume Data**: Integrate with market data APIs for volume information
3. **Web Interface**: Create a web-based UI
4. **Advanced Analytics**: More detailed trading statistics and charts
5. **Screenshot Management**: Automatic screenshot organization
6. **Trade Alerts**: Set up alerts for specific trading patterns

## Troubleshooting

### Common Issues:

1. **"OpenAI API key is not configured"**
   - Make sure you've added your OpenAI API key to `appsettings.json`

2. **"Google credentials path is not configured"**
   - Ensure the path to your Google service account JSON file is correct

3. **"Google Spreadsheet ID is not configured"**
   - Copy the correct Spreadsheet ID from your Google Sheets URL

4. **"Error analyzing chart image"**
   - Check that the image file exists and is a supported format (JPEG, PNG)
   - Ensure the chart image contains clear, readable information

5. **"Error recording trade to Google Sheets"**
   - Verify that your Google service account has edit permissions on the spreadsheet
   - Check that the Spreadsheet ID is correct

## File Structure

```
TradingJournalRider/
├── Models/
│   └── TradeData.cs          # Trade data model
├── Services/
│   ├── ChatGptService.cs     # OpenAI integration
│   ├── GoogleSheetsService.cs # Google Sheets integration
│   └── TradingJournalService.cs # Main service coordination
├── Utils/
│   └── ConfigurationManager.cs # Configuration management
├── Program.cs                 # Main application entry point
└── README.md                 # This file
```

## Contributing

Feel free to submit issues and enhancement requests!

## License

This project is for educational and personal use. 