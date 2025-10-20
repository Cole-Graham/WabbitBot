using System.IO;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Events;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Metadata for CDN URLs associated with canonical filenames
/// </summary>
/// <param name="CdnUrl">The Discord CDN URL</param>
/// <param name="MessageId">The Discord message ID where the file was uploaded</param>
/// <param name="ChannelId">The channel where the message was sent</param>
/// <param name="LastUpdated">When this metadata was last updated</param>
public record CdnMetadata(string CdnUrl, ulong? MessageId, ulong? ChannelId, DateTime LastUpdated);

/// <summary>
/// Service for secure file system operations including image validation and thumbnail management
/// Handles file uploads, validation, and secure file operations without database dependencies
/// </summary>
public partial class FileSystemService
{
    private static readonly HashSet<string> AllowedExtensions = new() { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private static readonly HashSet<string> AllowedMimeTypes = new()
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
    };

    public const long MaxThumbnailSize = 5 * 1024 * 1024; // 5MB
    public const long MaxReplayFileSize = 30 * 1024 * 1024; // 30MB

    protected readonly ICoreEventBus EventBus;
    private readonly IErrorService _errorHandler;
    private readonly string _thumbnailsDirectory;
    private readonly string _divisionIconsDirectory;
    private readonly string _discordComponentImagesDirectory;
    private readonly string _defaultDiscordImagesDirectory;
    private readonly string _replaysDirectory;
    private readonly Dictionary<string, CdnMetadata> _cdnMetadataCache;

    /// <summary>
    /// Creates a new FileSystemService instance
    /// </summary>
    /// <param name="storageOptions">Storage configuration options for directory paths</param>
    /// <param name="eventBus">Optional event bus instance, defaults to CoreEventBus.Instance</param>
    /// <param name="errorHandler">Optional error handler instance, defaults to new ErrorService()</param>
    public FileSystemService(
        WabbitBot.Common.Configuration.StorageOptions? storageOptions = null,
        ICoreEventBus? eventBus = null,
        IErrorService? errorHandler = null
    )
    {
        EventBus = eventBus ?? CoreEventBus.Instance;
        _errorHandler = errorHandler ?? new WabbitBot.Common.ErrorService.ErrorService();

        // Use provided storage options or create defaults
        storageOptions ??= new WabbitBot.Common.Configuration.StorageOptions();

        // Resolve paths (converts relative to absolute, leaves absolute as-is)
        _thumbnailsDirectory = storageOptions.ResolvePath(storageOptions.MapsDirectory);
        _divisionIconsDirectory = storageOptions.ResolvePath(storageOptions.DivisionIconsDirectory);
        _discordComponentImagesDirectory = storageOptions.ResolvePath(storageOptions.DiscordComponentImagesDirectory);
        _defaultDiscordImagesDirectory = storageOptions.ResolvePath(storageOptions.DefaultDiscordImagesDirectory);
        _replaysDirectory = storageOptions.ResolvePath(storageOptions.ReplaysDirectory);
        _cdnMetadataCache = new Dictionary<string, CdnMetadata>();

        // Ensure directories exist
        EnsureSecureDirectory();
        EnsureSecureDivisionIconsDirectory();
        EnsureSecureDiscordImagesDirectories();
        EnsureSecureReplaysDirectory();
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
                throw new InvalidOperationException(
                    $"File size {fileStream.Length} exceeds maximum allowed size {MaxThumbnailSize}"
                );
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

            // Publish event for successful upload (infrastructure fact, not database CRUD)
            await EventBus.PublishAsync(
                new ThumbnailUploadedEvent(secureFileName, originalFileName, fileStream.Length)
            );

            return secureFileName;
        }
        catch (Exception ex)
        {
            // Handle error through error handler
            await _errorHandler.CaptureAsync(
                ex,
                $"File upload validation failed for {originalFileName}",
                nameof(ValidateAndSaveImageAsync)
            );
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

                // Publish event for successful deletion (infrastructure fact, not database CRUD)
                await EventBus.PublishAsync(new ThumbnailDeletedEvent(fileName));

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await _errorHandler.CaptureAsync(
                ex,
                $"Failed to delete thumbnail {fileName}",
                nameof(DeleteThumbnailAsync)
            );
            return false;
        }
    }

    #region Division Icon Management

    /// <summary>
    /// Validates and saves an uploaded division icon file with security checks
    /// </summary>
    /// <param name="fileStream">The file stream to validate and save</param>
    /// <param name="originalFileName">Original filename for extension validation</param>
    /// <param name="mimeType">MIME type of the file</param>
    /// <returns>Secure filename if successful, null if validation fails</returns>
    public async Task<string?> ValidateAndSaveDivisionIconAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType
    )
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
                throw new InvalidOperationException(
                    $"File size {fileStream.Length} exceeds maximum allowed size {MaxThumbnailSize}"
                );
            }

            // 4. Validate file content (magic bytes)
            if (!IsValidImageFile(fileStream))
            {
                throw new InvalidOperationException("File content does not match expected image format");
            }

            // 5. Generate secure filename and save
            var secureFileName = GenerateSecureFileName(extension);
            var fullPath = Path.Combine(_divisionIconsDirectory, secureFileName);

            fileStream.Position = 0;
            using var fileStream2 = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStream2);

            // Publish event for successful upload (infrastructure fact, not database CRUD)
            await EventBus.PublishAsync(
                new DivisionIconUploadedEvent(secureFileName, originalFileName, fileStream.Length)
            );

            return secureFileName;
        }
        catch (Exception ex)
        {
            // Handle error through error handler
            await _errorHandler.CaptureAsync(
                ex,
                $"Division icon upload validation failed for {originalFileName}",
                nameof(ValidateAndSaveDivisionIconAsync)
            );
            return null;
        }
    }

    /// <summary>
    /// Deletes a division icon file securely
    /// </summary>
    /// <param name="fileName">The filename to delete</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> DeleteDivisionIconAsync(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_divisionIconsDirectory, fileName);

            // Ensure the path is safe (prevent path traversal)
            if (!IsPathSafe(fullPath))
            {
                throw new InvalidOperationException("Invalid file path");
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);

                // Publish event for successful deletion (infrastructure fact, not database CRUD)
                await EventBus.PublishAsync(new DivisionIconDeletedEvent(fileName));

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await _errorHandler.CaptureAsync(
                ex,
                $"Failed to delete division icon {fileName}",
                nameof(DeleteDivisionIconAsync)
            );
            return false;
        }
    }

    #endregion

    #region Discord Component Images Management

    /// <summary>
    /// Gets the default or custom Discord component image
    /// Returns CDN URL if cached, otherwise returns local file path
    /// </summary>
    /// <param name="imageName">The image filename (e.g., "challenge_banner.jpg")</param>
    /// <returns>CDN URL or file path, null if not found</returns>
    public string? GetDiscordComponentImage(string imageName)
    {
        Console.WriteLine($"🔍 DEBUG: GetDiscordComponentImage called with: {imageName}");
        Console.WriteLine($"🔍 DEBUG: _discordComponentImagesDirectory: {_discordComponentImagesDirectory}");
        Console.WriteLine($"🔍 DEBUG: _defaultDiscordImagesDirectory: {_defaultDiscordImagesDirectory}");

        // Check for custom override first
        var customPath = Path.Combine(_discordComponentImagesDirectory, imageName);
        Console.WriteLine($"🔍 DEBUG: Custom path: {customPath}");
        Console.WriteLine($"🔍 DEBUG: Custom path exists: {File.Exists(customPath)}");

        if (File.Exists(customPath))
        {
            // Check if we have CDN metadata for the custom image
            var cdnMetadata = GetCdnMetadata(imageName);
            if (cdnMetadata is not null)
            {
                Console.WriteLine($"🔍 DEBUG: Found CDN metadata for custom image: {cdnMetadata.CdnUrl}");
                return cdnMetadata.CdnUrl;
            }
            Console.WriteLine($"🔍 DEBUG: Returning custom path: {customPath}");
            return customPath; // Return local path for upload
        }

        // Fall back to default image
        var defaultPath = Path.Combine(_defaultDiscordImagesDirectory, imageName);
        Console.WriteLine($"🔍 DEBUG: Default path: {defaultPath}");
        Console.WriteLine($"🔍 DEBUG: Default path exists: {File.Exists(defaultPath)}");

        if (File.Exists(defaultPath))
        {
            // Check if we have CDN metadata for the default image
            var cdnMetadata = GetCdnMetadata($"default_{imageName}");
            if (cdnMetadata is not null)
            {
                Console.WriteLine($"🔍 DEBUG: Found CDN metadata for default image: {cdnMetadata.CdnUrl}");
                return cdnMetadata.CdnUrl;
            }
            Console.WriteLine($"🔍 DEBUG: Returning default path: {defaultPath}");
            return defaultPath; // Return local path for upload
        }

        Console.WriteLine($"🔍 DEBUG: Image not found, returning null");
        return null;
    }

    /// <summary>
    /// Validates and saves a custom Discord component image
    /// </summary>
    /// <param name="fileStream">The file stream to validate and save</param>
    /// <param name="canonicalFileName">The canonical filename (e.g., "challenge_banner.jpg")</param>
    /// <param name="mimeType">MIME type of the file</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> SaveCustomDiscordImageAsync(Stream fileStream, string canonicalFileName, string mimeType)
    {
        try
        {
            // Validate extension
            var extension = Path.GetExtension(canonicalFileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException($"File extension '{extension}' is not allowed");
            }

            // Validate MIME type
            if (!AllowedMimeTypes.Contains(mimeType.ToLowerInvariant()))
            {
                throw new InvalidOperationException($"MIME type '{mimeType}' is not allowed");
            }

            // Validate file size
            if (fileStream.Length > MaxThumbnailSize)
            {
                throw new InvalidOperationException(
                    $"File size {fileStream.Length} exceeds maximum allowed size {MaxThumbnailSize}"
                );
            }

            // Validate file content
            if (!IsValidImageFile(fileStream))
            {
                throw new InvalidOperationException("File content does not match expected image format");
            }

            // Save with canonical filename
            var fullPath = Path.Combine(_discordComponentImagesDirectory, canonicalFileName);
            fileStream.Position = 0;
            using var fileStream2 = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(fileStream2);

            // Publish event
            await EventBus.PublishAsync(new DiscordComponentImageUploadedEvent(canonicalFileName, fileStream.Length));

            return true;
        }
        catch (Exception ex)
        {
            await _errorHandler.CaptureAsync(
                ex,
                $"Failed to save custom Discord component image: {canonicalFileName}",
                nameof(SaveCustomDiscordImageAsync)
            );
            return false;
        }
    }

    /// <summary>
    /// Deletes a custom Discord component image (reverts to default)
    /// </summary>
    /// <param name="imageName">The image filename</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> DeleteCustomDiscordImageAsync(string imageName)
    {
        try
        {
            var fullPath = Path.Combine(_discordComponentImagesDirectory, imageName);

            if (!IsPathSafe(fullPath))
            {
                throw new InvalidOperationException("Invalid file path");
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);

                // Clear CDN metadata for this custom image
                lock (_cdnMetadataCache)
                {
                    _cdnMetadataCache.Remove(imageName);
                }

                await EventBus.PublishAsync(new DiscordComponentImageDeletedEvent(imageName));
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await _errorHandler.CaptureAsync(
                ex,
                $"Failed to delete custom Discord component image: {imageName}",
                nameof(DeleteCustomDiscordImageAsync)
            );
            return false;
        }
    }

    #endregion

    #region Replay File Management

    /// <summary>
    /// Validates and saves a replay file (.rpl3 or .zip containing replay) with security checks
    /// </summary>
    /// <param name="fileData">The replay file data</param>
    /// <param name="originalFileName">Original filename for validation</param>
    /// <returns>Secure filename if successful, null if validation fails</returns>
    public async Task<string?> SaveReplayFileAsync(byte[] fileData, string? originalFileName = null)
    {
        try
        {
            // 1. Validate file size
            if (fileData.Length > MaxReplayFileSize)
            {
                throw new InvalidOperationException(
                    $"Replay file size {fileData.Length} exceeds maximum allowed size {MaxReplayFileSize}"
                );
            }

            // 2. Validate file extension if original filename provided
            string extension = ".rpl3"; // Default extension
            if (!string.IsNullOrEmpty(originalFileName))
            {
                extension = Path.GetExtension(originalFileName).ToLowerInvariant();
                if (extension != ".rpl3" && extension != ".zip")
                {
                    throw new InvalidOperationException(
                        $"File extension '{extension}' is not allowed. Only .rpl3 and .zip files are supported."
                    );
                }
            }

            // 3. Generate secure filename and save
            var secureFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(_replaysDirectory, secureFileName);

            await File.WriteAllBytesAsync(fullPath, fileData);

            // Publish event for successful upload (infrastructure fact, not database CRUD)
            await EventBus.PublishAsync(new ReplayFileUploadedEvent(secureFileName, originalFileName, fileData.Length));

            return secureFileName;
        }
        catch (Exception ex)
        {
            await _errorHandler.CaptureAsync(
                ex,
                $"Replay file upload failed for {originalFileName}",
                nameof(SaveReplayFileAsync)
            );
            return null;
        }
    }

    /// <summary>
    /// Deletes a replay file securely
    /// </summary>
    /// <param name="fileName">The filename to delete</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> DeleteReplayFileAsync(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_replaysDirectory, fileName);

            // Ensure the path is safe (prevent path traversal)
            if (!IsPathSafe(fullPath))
            {
                throw new InvalidOperationException("Invalid file path");
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);

                // Publish event for successful deletion (infrastructure fact, not database CRUD)
                await EventBus.PublishAsync(new ReplayFileDeletedEvent(fileName));

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            await _errorHandler.CaptureAsync(
                ex,
                $"Failed to delete replay file {fileName}",
                nameof(DeleteReplayFileAsync)
            );
            return false;
        }
    }

    /// <summary>
    /// Reads a replay file's contents
    /// </summary>
    /// <param name="fileName">The filename to read</param>
    /// <returns>File contents if successful, null otherwise</returns>
    public async Task<byte[]?> ReadReplayFileAsync(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_replaysDirectory, fileName);

            if (!IsPathSafe(fullPath) || !File.Exists(fullPath))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(fullPath);
        }
        catch (Exception ex)
        {
            await _errorHandler.CaptureAsync(ex, $"Failed to read replay file {fileName}", nameof(ReadReplayFileAsync));
            return null;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Finds a file in the specified directory by matching a pattern.
    /// Used to locate assets by ID without knowing the exact extension.
    /// </summary>
    /// <param name="directory">The directory to search</param>
    /// <param name="filePattern">The file pattern (e.g., "mapId.*")</param>
    /// <returns>Full path to the first matching file, or null if not found</returns>
    private string? FindFileByPattern(string directory, string filePattern)
    {
        try
        {
            var matchingFiles = Directory.GetFiles(directory, filePattern);
            return matchingFiles.Length > 0 ? matchingFiles[0] : null;
        }
        catch
        {
            return null;
        }
    }

    private void EnsureSecureDirectory()
    {
        if (!Directory.Exists(_thumbnailsDirectory))
        {
            Directory.CreateDirectory(_thumbnailsDirectory);
        }

        // Validate path is safe (no path traversal)
        var normalizedPath = Path.GetFullPath(_thumbnailsDirectory);
        if (!string.Equals(normalizedPath, _thumbnailsDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Invalid thumbnails directory path: {_thumbnailsDirectory}");
        }
    }

    private void EnsureSecureDivisionIconsDirectory()
    {
        if (!Directory.Exists(_divisionIconsDirectory))
        {
            Directory.CreateDirectory(_divisionIconsDirectory);
        }

        // Validate path is safe (no path traversal)
        var normalizedPath = Path.GetFullPath(_divisionIconsDirectory);
        if (!string.Equals(normalizedPath, _divisionIconsDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Invalid division icons directory path: {_divisionIconsDirectory}");
        }
    }

    private void EnsureSecureDiscordImagesDirectories()
    {
        // Custom Discord images directory
        if (!Directory.Exists(_discordComponentImagesDirectory))
        {
            Directory.CreateDirectory(_discordComponentImagesDirectory);
        }

        // Default Discord images should already exist (shipped with deployment)
        // But we'll create it if missing for robustness
        if (!Directory.Exists(_defaultDiscordImagesDirectory))
        {
            Directory.CreateDirectory(_defaultDiscordImagesDirectory);
        }

        // Validate paths are safe (no path traversal)
        var normalizedComponentPath = Path.GetFullPath(_discordComponentImagesDirectory);
        if (
            !string.Equals(
                normalizedComponentPath,
                _discordComponentImagesDirectory,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new InvalidOperationException(
                $"Invalid Discord component images directory path: {_discordComponentImagesDirectory}"
            );
        }

        var normalizedDefaultPath = Path.GetFullPath(_defaultDiscordImagesDirectory);
        if (!string.Equals(normalizedDefaultPath, _defaultDiscordImagesDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Invalid default Discord images directory path: {_defaultDiscordImagesDirectory}"
            );
        }
    }

    private void EnsureSecureReplaysDirectory()
    {
        if (!Directory.Exists(_replaysDirectory))
        {
            Directory.CreateDirectory(_replaysDirectory);
        }

        // Validate path is safe (no path traversal)
        var normalizedPath = Path.GetFullPath(_replaysDirectory);
        if (!string.Equals(normalizedPath, _replaysDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Invalid replays directory path: {_replaysDirectory}");
        }
    }

    /// <summary>
    /// Validates if a stream contains a valid image file by checking magic bytes
    /// Uses Span<T> for zero-allocation header validation
    /// </summary>
    /// <param name="fileStream">The file stream to validate</param>
    /// <returns>True if the stream contains a valid image format</returns>
    private static bool IsValidImageFile(Stream fileStream)
    {
        // Use stackalloc for zero heap allocation - this is a Span<T> optimization!
        Span<byte> header = stackalloc byte[8];
        var bytesRead = fileStream.Read(header);
        fileStream.Position = 0;

        if (bytesRead < 8)
            return false;

        // Pass ReadOnlySpan<byte> to validation methods for type safety
        return IsJpeg(header) || IsPng(header) || IsGif(header) || IsWebp(header);
    }

    /// <summary>
    /// Checks if the header bytes represent a JPEG image
    /// Uses ReadOnlySpan<byte> for efficient, allocation-free validation
    /// </summary>
    private static bool IsJpeg(ReadOnlySpan<byte> header) =>
        header.Length >= 2 && header[0] == 0xFF && header[1] == 0xD8;

    /// <summary>
    /// Checks if the header bytes represent a PNG image
    /// Uses ReadOnlySpan<byte> for efficient, allocation-free validation
    /// </summary>
    private static bool IsPng(ReadOnlySpan<byte> header) =>
        header.Length >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;

    /// <summary>
    /// Checks if the header bytes represent a GIF image
    /// Uses ReadOnlySpan<byte> for efficient, allocation-free validation
    /// </summary>
    private static bool IsGif(ReadOnlySpan<byte> header) =>
        header.Length >= 3 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46;

    /// <summary>
    /// Checks if the header bytes represent a WebP image
    /// Uses ReadOnlySpan<byte> for efficient, allocation-free validation
    /// </summary>
    private static bool IsWebp(ReadOnlySpan<byte> header) =>
        header.Length >= 4 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46;

    private static string GenerateSecureFileName(string extension)
    {
        return $"{Guid.NewGuid()}{extension}";
    }

    /// <summary>
    /// Validates that a file path is safe and doesn't attempt path traversal.
    /// Checks that the resolved path is within one of the configured storage directories.
    /// </summary>
    private bool IsPathSafe(string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(filePath);

            // Check if path is within any of our configured directories
            var allowedDirectories = new[]
            {
                _thumbnailsDirectory,
                _divisionIconsDirectory,
                _discordComponentImagesDirectory,
                _defaultDiscordImagesDirectory,
                _replaysDirectory,
            };

            foreach (var allowedDir in allowedDirectories)
            {
                var normalizedAllowedDir = Path.GetFullPath(allowedDir);
                if (fullPath.StartsWith(normalizedAllowedDir, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false; // Path is not within any allowed directory
        }
        catch
        {
            return false; // Any path resolution error = unsafe
        }
    }

    #endregion
}
