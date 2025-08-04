using System;
using System.IO;
using System.Threading.Tasks;

namespace TradingJournalGPT.Services
{
    public class ImageStorageService
    {
        private readonly string _imageStoragePath;

        public ImageStorageService()
        {
            // Store images in a subfolder of the application directory
            var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
            _imageStoragePath = Path.Combine(appDirectory, "Data", "ChartImages");
            
            // Ensure the directory exists
            Directory.CreateDirectory(_imageStoragePath);
        }

        public async Task<string> StoreChartImageAsync(string originalImagePath, string symbol, DateTime date, int tradeSeq)
        {
            try
            {
                // Create a unique filename based on symbol, date, and trade sequence
                var fileName = $"{symbol}_{date:yyyyMMdd}_{tradeSeq:D3}{Path.GetExtension(originalImagePath)}";
                var destinationPath = Path.Combine(_imageStoragePath, fileName);

                // Copy the image to our storage location
                await Task.Run(() => File.Copy(originalImagePath, destinationPath, true));

                return destinationPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error storing chart image: {ex.Message}");
            }
        }

        public int GetNextTradeSeq(string symbol, DateTime date)
        {
            try
            {
                if (!Directory.Exists(_imageStoragePath))
                    return 1;

                // Get all existing images for this symbol and date
                var pattern = $"{symbol}_{date:yyyyMMdd}_*";
                var existingFiles = Directory.GetFiles(_imageStoragePath, pattern)
                    .Where(file => IsImageFile(file))
                    .ToList();

                if (!existingFiles.Any())
                    return 1;

                // Extract sequence numbers from existing files
                var existingSeqs = new List<int>();
                foreach (var file in existingFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var parts = fileName.Split('_');
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var seq))
                    {
                        existingSeqs.Add(seq);
                    }
                }

                // Return the next available sequence number
                return existingSeqs.Any() ? existingSeqs.Max() + 1 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting next trade sequence: {ex.Message}");
                return 1;
            }
        }

        public bool ImageExists(string imagePath)
        {
            return !string.IsNullOrEmpty(imagePath) && File.Exists(imagePath);
        }

        public string GetImageStoragePath()
        {
            return _imageStoragePath;
        }

        public void OpenImageInDefaultViewer(string imagePath)
        {
            if (ImageExists(imagePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = imagePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error opening image: {ex.Message}");
                }
            }
            else
            {
                throw new FileNotFoundException($"Image not found: {imagePath}");
            }
        }

        public bool DeleteImage(string imagePath)
        {
            try
            {
                if (ImageExists(imagePath))
                {
                    File.Delete(imagePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting image {imagePath}: {ex.Message}");
                return false;
            }
        }

        public void CleanupOrphanedImages(List<string> validImagePaths)
        {
            try
            {
                if (!Directory.Exists(_imageStoragePath))
                    return;

                var allImages = Directory.GetFiles(_imageStoragePath, "*.*")
                    .Where(file => IsImageFile(file))
                    .ToList();

                var orphanedImages = allImages.Except(validImagePaths, StringComparer.OrdinalIgnoreCase).ToList();

                foreach (var orphanedImage in orphanedImages)
                {
                    try
                    {
                        File.Delete(orphanedImage);
                        Console.WriteLine($"Cleaned up orphaned image: {orphanedImage}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not delete orphaned image {orphanedImage}: {ex.Message}");
                    }
                }

                if (orphanedImages.Any())
                {
                    Console.WriteLine($"Cleaned up {orphanedImages.Count} orphaned images");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during orphaned image cleanup: {ex.Message}");
            }
        }

        public string GetImagePathFromTradeData(string symbol, DateTime date, int tradeSeq)
        {
            try
            {
                if (!Directory.Exists(_imageStoragePath))
                    return string.Empty;

                // Look for image with the specific naming pattern
                var pattern = $"{symbol}_{date:yyyyMMdd}_{tradeSeq:D3}.*";
                var matchingFiles = Directory.GetFiles(_imageStoragePath, pattern)
                    .Where(file => IsImageFile(file))
                    .ToList();

                return matchingFiles.FirstOrDefault() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting image path from trade data: {ex.Message}");
                return string.Empty;
            }
        }

        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
        }
    }
} 