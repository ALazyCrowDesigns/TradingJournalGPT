# Trading Journal GPT

A Windows Forms application for automatic trading journal management using ChatGPT for chart analysis and local data storage.

## Features

- **Chart Analysis**: Upload chart images to ChatGPT for automatic data extraction
- **Local Storage**: Store trade data and chart images locally
- **Google Sheets Integration**: Optional upload to Google Sheets
- **Windows UI**: Modern Windows Forms interface with menu system
- **Undo/Redo**: Full undo/redo functionality for data modifications
- **Data Management**: Edit, delete, and view trade data with linked chart images
- **Batch Processing**: Process single images or entire folders

## Extracted Data

The application extracts the following data from chart images:
- Stock Symbol
- Date
- Previous Day Close
- High After Volume Surge
- Low After Volume Surge
- Gap % (Close to High)
- Gap % (High to Low)
- Volume (in millions)
- Trade Sequence Number

## Setup

### Prerequisites
- .NET 6.0 or later
- OpenAI API key
- Google Sheets API credentials (optional)

### Configuration
1. Create `appsettings.json` in the project root with:
```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key"
  },
  "GoogleSheets": {
    "SpreadsheetId": "your-spreadsheet-id",
    "CredentialsPath": "path-to-credentials.json"
  }
}
```

### Build and Run
```bash
cd TradingJournalRider/TradingJournalRider
dotnet build
dotnet run
```

## Usage

1. **Analyze Single Chart**: Click "Analyze Chart Image" and select an image
2. **Analyze Folder**: Click "Analyze Chart Folder" to process multiple images
3. **View Data**: All trades are displayed in the main grid
4. **Edit Data**: Click on editable cells to modify values
5. **Delete Trades**: Right-click and select "Delete Trade"
6. **View Charts**: Click on the "Chart" column to open linked images
7. **Undo/Redo**: Use the Edit menu for undo/redo operations

## File Structure

```
TradingJournalGPT/
├── TradingJournalRider/
│   ├── TradingJournalRider/
│   │   ├── Forms/           # Windows Forms UI
│   │   ├── Models/          # Data models
│   │   ├── Services/        # Business logic
│   │   └── Utils/          # Utilities
│   └── Data/               # Local storage (created at runtime)
│       └── ChartImages/    # Stored chart images
```

## Version Control

This project uses Git for version control. Sensitive files (API keys, credentials) are excluded via `.gitignore`.

## Recent Updates

- Added comprehensive Windows menu system
- Implemented undo/redo functionality
- Enhanced local storage with image management
- Added trade sequencing for multiple trades per symbol/date
- Improved error handling and debugging 