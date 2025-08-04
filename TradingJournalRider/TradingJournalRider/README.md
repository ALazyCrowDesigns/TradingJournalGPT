# Trading Journal GPT - Windows Application

A Windows Forms application for automatically analyzing trading chart images and recording trade data.

## Features

### Main Interface
- **Data Table**: Displays all recorded trades with columns for Symbol, Date, Previous Close, High/Low after volume surge, Gap percentages, Volume, and Chart details
- **Analyze Chart Image**: Click to select and analyze a single chart image
- **Analyze Chart Folder**: Click to select a folder containing multiple chart images for batch processing
- **Refresh Data**: Updates the data table with the latest trades

### Analysis Results
When analyzing a chart image, the application will:
1. Extract trading data using ChatGPT vision analysis
2. Display the results in a confirmation dialog
3. Allow you to choose between saving to Google Sheets or local storage
4. Update the main data table with the new trade

### Batch Processing
When analyzing a folder:
1. Select your storage preference (Google Sheets or local)
2. The application processes all supported images (PNG, JPG, JPEG)
3. Shows progress and automatically records all trades
4. Displays a summary when complete

## Setup Requirements

### Configuration
Make sure you have a valid `appsettings.json` file with:
- OpenAI API key
- Google Sheets credentials path
- Google Spreadsheet ID
- Google Sheet name

### First Run
If this is your first time running the application:
1. The application will guide you through setup
2. You'll need to create a Google Cloud project and enable Google Sheets API
3. Download and place your Google service account credentials file
4. Create a Google Sheet and share it with your service account

## Usage

1. **Launch the Application**: Run `dotnet run` from the project directory
2. **Analyze Single Chart**: Click "Analyze Chart Image" and select your chart image
3. **Batch Process**: Click "Analyze Chart Folder" and select a folder with multiple chart images
4. **View Results**: The data table shows all recorded trades
5. **Refresh**: Click "Refresh Data" to update the table with latest data

## Supported Image Formats
- PNG
- JPG/JPEG

## Data Storage Options
- **Google Sheets**: Saves to your configured Google Spreadsheet
- **Local Storage**: Saves to a JSON file in the application's Data folder

## Chart Column
Click on the "Chart" column in any row to view additional details about that trade (future enhancement for showing the original chart image).

## Troubleshooting

### Common Issues
1. **Configuration Errors**: Check your `appsettings.json` file
2. **Google Sheets Access**: Ensure your service account has editor permissions
3. **OpenAI API**: Verify your API key and credits
4. **Image Format**: Use only PNG, JPG, or JPEG files

### Getting Help
- Check the console output for detailed error messages
- Verify all API keys and credentials are correct
- Ensure your Google Sheet exists and is properly shared

## Technical Details

- Built with .NET 6.0 Windows Forms
- Uses OpenAI GPT-4 Vision for chart analysis
- Integrates with Google Sheets API
- Supports local JSON storage as backup
- Modern Windows UI with intuitive controls 