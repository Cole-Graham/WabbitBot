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

    private const long MaxThumbnailSize = 1 * 1024 * 1024; // 1MB

    protected readonly ICoreEventBus EventBus;
    private readonly IErrorService _errorHandler;
    private readonly string _thumbnailsDirectory;
    private readonly string _divisionIconsDirectory;
    private readonly string _discordComponentImagesDirectory;
    private readonly string _defaultDiscordImagesDirectory;
    private readonly Dictionary<string, CdnMetadata> _cdnMetadataCache;

    /// <summary>
    /// Creates a new FileSystemService instance
    /// </summary>
    /// <param name="eventBus">Optional event bus instance, defaults to CoreEventBus.Instance</param>
    /// <param name="errorHandler">Optional error handler instance, defaults to new ErrorService()</param>
    public FileSystemService(ICoreEventBus? eventBus = null, IErrorService? errorHandler = null)
    {
        EventBus = eventBus ?? CoreEventBus.Instance;
        _errorHandler = errorHandler ?? new WabbitBot.Common.ErrorService.ErrorService(); // TODO: Use a shared instance
        _thumbnailsDirectory = Path.GetFullPath("data/maps/thumbnails");
        _divisionIconsDirectory = Path.GetFullPath("data/divisions/icons");
        _discordComponentImagesDirectory = Path.GetFullPath("data/images/discord");
        _defaultDiscordImagesDirectory = Path.GetFullPath("data/images/default/discord");
        _cdnMetadataCache = new Dictionary<string, CdnMetadata>();

        // Ensure directories exist and are secure
        EnsureSecureDirectory();
        EnsureSecureDivisionIconsDirectory();
        EnsureSecureDiscordImagesDirectories();
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

    /// <summary>
    /// Gets the full path to a division icon file with security validation
    /// </summary>
    /// <param name="fileName">The filename</param>
    /// <returns>Full path if file exists and is safe, null otherwise</returns>
    public string? GetDivisionIconPath(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_divisionIconsDirectory, fileName);

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
    /// Checks if a division icon file exists
    /// </summary>
    /// <param name="fileName">The filename to check</param>
    /// <returns>True if the file exists and is accessible</returns>
    public bool DivisionIconExists(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_divisionIconsDirectory, fileName);
            return IsPathSafe(fullPath) && File.Exists(fullPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the configured division icons directory
    /// </summary>
    public string DivisionIconsDirectory => _divisionIconsDirectory;

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
        // Check for custom override first
        var customPath = Path.Combine(_discordComponentImagesDirectory, imageName);
        if (File.Exists(customPath))
        {
            // Check if we have CDN metadata for the custom image
            var cdnMetadata = GetCdnMetadata(imageName);
            if (cdnMetadata is not null)
            {
                return cdnMetadata.CdnUrl;
            }
            return customPath; // Return local path for upload
        }

        // Fall back to default image
        var defaultPath = Path.Combine(_defaultDiscordImagesDirectory, imageName);
        if (File.Exists(defaultPath))
        {
            // Check if we have CDN metadata for the default image
            var cdnMetadata = GetCdnMetadata($"default_{imageName}");
            if (cdnMetadata is not null)
            {
                return cdnMetadata.CdnUrl;
            }
            return defaultPath; // Return local path for upload
        }

        return null;
    }

    /// <summary>
    /// Checks if a Discord component image is using the default or has been customized
    /// </summary>
    /// <param name="imageName">The image filename</param>
    /// <returns>True if using default, false if customized</returns>
    public bool IsUsingDefaultDiscordImage(string imageName)
    {
        var customPath = Path.Combine(_discordComponentImagesDirectory, imageName);
        return !File.Exists(customPath);
    }

    /// <summary>
    /// Gets all available default Discord component image names
    /// </summary>
    public List<string> GetDefaultDiscordImageNames()
    {
        if (!Directory.Exists(_defaultDiscordImagesDirectory))
        {
            return new List<string>();
        }

        return Directory
            .GetFiles(_defaultDiscordImagesDirectory)
            .Where(f => !f.EndsWith(".pdn", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Cast<string>()
            .OrderBy(name => name)
            .ToList();
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

    #region Private Helper Methods

    /// <summary>
    /// Finds a file in the specified directory by matching a pattern.
    /// Used to locate assets by ID without knowing the exact extension.
    /// </summary>
    /// <param name="directory">The directory to search</param>
    /// <param name="filePattern">The file pattern (e.g., "mapId.*")</param>
    /// <returns>Full path to the first matching file, or null if not found</returns>
    private static string? FindFileByPattern(string directory, string filePattern)
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

        // Ensure the directory is within the application directory
        var appDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        if (!_thumbnailsDirectory.StartsWith(appDirectory))
        {
            throw new InvalidOperationException("Thumbnails directory must be within the application directory");
        }
    }

    private void EnsureSecureDivisionIconsDirectory()
    {
        if (!Directory.Exists(_divisionIconsDirectory))
        {
            Directory.CreateDirectory(_divisionIconsDirectory);
        }

        // Ensure the directory is within the application directory
        var appDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        if (!_divisionIconsDirectory.StartsWith(appDirectory))
        {
            throw new InvalidOperationException("Division icons directory must be within the application directory");
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

        // Ensure directories are within the application directory
        var appDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        if (!_discordComponentImagesDirectory.StartsWith(appDirectory))
        {
            throw new InvalidOperationException(
                "Discord component images directory must be within the application directory"
            );
        }
        if (!_defaultDiscordImagesDirectory.StartsWith(appDirectory))
        {
            throw new InvalidOperationException(
                "Default Discord images directory must be within the application directory"
            );
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

    private static bool IsPathSafe(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var basePath = Path.GetFullPath(AppContext.BaseDirectory);
        return fullPath.StartsWith(basePath);
    }

    #endregion
}
