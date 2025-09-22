using System.IO;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Service for secure file system operations including image validation and thumbnail management
/// Handles file uploads, validation, and secure file operations without database dependencies
/// </summary>
[GenerateEventPublisher(EventBusType = EventBusType.Core, EnableValidation = true, EnableTimestamps = true)]
public partial class FileSystemService
{
    private static readonly HashSet<string> AllowedExtensions = new()
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    private const long MaxThumbnailSize = 1 * 1024 * 1024; // 1MB

    private readonly ICoreEventBus _eventBus;
    private readonly ICoreErrorHandler _errorHandler;
    private readonly string _thumbnailsDirectory;

    /// <summary>
    /// Creates a new FileSystemService instance
    /// </summary>
    /// <param name="eventBus">Optional event bus instance, defaults to CoreEventBus.Instance</param>
    /// <param name="errorHandler">Optional error handler instance, defaults to CoreErrorHandler.Instance</param>
    public FileSystemService(
        ICoreEventBus? eventBus = null,
        ICoreErrorHandler? errorHandler = null)
    {
        _eventBus = eventBus ?? CoreEventBus.Instance;
        _errorHandler = errorHandler ?? CoreErrorHandler.Instance;
        _thumbnailsDirectory = Path.GetFullPath("data/maps/thumbnails");

        // Ensure directory exists and is secure
        EnsureSecureDirectory();
    }

    /// <summary>
    /// Validates and saves an uploaded image file with security checks
    /// </summary>
    /// <param name="fileStream">The file stream to validate and save</param>
    /// <param name="originalFileName">Original filename for extension validation</param>
    /// <param name="mimeType">MIME type of the file</param>
    /// <returns>Secure filename if successful, null if validation fails</returns>
    public async Task<string?> ValidateAndSaveImageAsync(Stream fileStream, string originalFileName, string mimeType)
    {
        try
        {
            // 1. Validate file extension
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"File extension '{extension}' is not allowed");
            }

            // 2. Validate MIME type
            if (!AllowedMimeTypes.Contains(mimeType.ToLowerInvariant()))
            {
                throw new InvalidOperationException($"MIME type '{mimeType}' is not allowed");
            }

            // 3. Validate file size
            if (fileStream.Length > MaxThumbnailSize)
            {
                throw new InvalidOperationException($"File size {fileStream.Length} exceeds maximum allowed size {MaxThumbnailSize}");
            }

            // 4. Validate file content (magic bytes)
            if (!IsValidImageFile(fileStream))
            {
                throw new InvalidOperationException("File content does not match expected image format");
            }

            // 5. Generate secure filename and save
            var secureFileName = GenerateSecureFileName(extension);
            var fullPath = Path.Combine(_thumbnailsDirectory, secureFileName);

            fileStream.Position = 0;
            using var fileStream2 = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStream2);

            // Publish event for successful upload
            await _eventBus.PublishAsync(new ThumbnailUploadedEvent
            {
                FileName = secureFileName,
                OriginalFileName = originalFileName,
                FileSize = fileStream.Length,
                MimeType = mimeType
            });

            return secureFileName;
        }
        catch (Exception ex)
        {
            // Handle error through error handler
            await _errorHandler.HandleErrorAsync(ex, $"File upload validation failed for {originalFileName}");
            return null;
        }
    }

    /// <summary>
    /// Deletes a thumbnail file securely
    /// </summary>
    /// <param name="fileName">The filename to delete</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> DeleteThumbnailAsync(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_thumbnailsDirectory, fileName);

            // Ensure the path is safe (prevent path traversal)
            if (!IsPathSafe(fullPath))
            {
                throw new InvalidOperationException("Invalid file path");
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);

                // Publish event for successful deletion
                await _eventBus.PublishAsync(new ThumbnailDeletedEvent
                {
                    FileName = fileName
                });

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await _errorHandler.HandleErrorAsync(ex, $"Failed to delete thumbnail {fileName}");
            return false;
        }
    }

    /// <summary>
    /// Gets the full path to a thumbnail file with security validation
    /// </summary>
    /// <param name="fileName">The filename</param>
    /// <returns>Full path if file exists and is safe, null otherwise</returns>
    public string? GetThumbnailPath(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_thumbnailsDirectory, fileName);

            if (!IsPathSafe(fullPath) || !File.Exists(fullPath))
            {
                return null;
            }

            return fullPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a thumbnail file exists
    /// </summary>
    /// <param name="fileName">The filename to check</param>
    /// <returns>True if the file exists and is accessible</returns>
    public bool ThumbnailExists(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_thumbnailsDirectory, fileName);
            return IsPathSafe(fullPath) && File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the configured thumbnails directory
    /// </summary>
    public string ThumbnailsDirectory => _thumbnailsDirectory;

    #region Private Helper Methods

    private void EnsureSecureDirectory()
    {
        if (!Directory.Exists(_thumbnailsDirectory))
        {
            Directory.CreateDirectory(_thumbnailsDirectory);
        }

        // Ensure the directory is within the application directory
        var appDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        if (!_thumbnailsDirectory.StartsWith(appDirectory))
        {
            throw new InvalidOperationException("Thumbnails directory must be within the application directory");
        }
    }

    private static bool IsValidImageFile(Stream fileStream)
    {
        var header = new byte[8];
        var bytesRead = fileStream.Read(header, 0, 8);
        fileStream.Position = 0;

        if (bytesRead < 8)
            return false;

        return IsJpeg(header) || IsPng(header) || IsGif(header) || IsWebp(header);
    }

    private static bool IsJpeg(byte[] header) =>
        header.Length >= 2 && header[0] == 0xFF && header[1] == 0xD8;

    private static bool IsPng(byte[] header) =>
        header.Length >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

    private static bool IsGif(byte[] header) =>
        header.Length >= 3 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46;

    private static bool IsWebp(byte[] header) =>
        header.Length >= 4 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46;

    private static string GenerateSecureFileName(string extension)
    {
        return $"{Guid.NewGuid()}{extension}";
    }

    private static bool IsPathSafe(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var basePath = Path.GetFullPath(AppContext.BaseDirectory);
        return fullPath.StartsWith(basePath);
    }

    #endregion
}
