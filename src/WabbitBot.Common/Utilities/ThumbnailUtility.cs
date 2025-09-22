using System.IO;

namespace WabbitBot.Common.Utilities;

/// <summary>
/// Utility class for thumbnail file operations
/// Provides static methods for working with map thumbnails stored on disk
/// </summary>
public static class ThumbnailUtility
{
    private static string? _thumbnailsDirectory;

    /// <summary>
    /// Initialize the thumbnail utility with configuration
    /// Must be called during application startup
    /// </summary>
    public static void Initialize(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _thumbnailsDirectory = configuration["Bot:Maps:ThumbnailsDirectory"]
                             ?? "data/maps/thumbnails";
    }

    /// <summary>
    /// Get the full file system path for a thumbnail
    /// Returns the specific thumbnail if it exists, otherwise falls back to default.jpg
    /// </summary>
    public static string? GetThumbnailPath(string? filename)
    {
        if (string.IsNullOrEmpty(filename) || _thumbnailsDirectory == null)
            return null;

        var specificPath = Path.Combine(_thumbnailsDirectory, filename);
        if (File.Exists(specificPath))
            return specificPath;

        var defaultPath = Path.Combine(_thumbnailsDirectory, "default.jpg");
        return File.Exists(defaultPath) ? defaultPath : specificPath;
    }

    /// <summary>
    /// Get Discord attachment URL for a thumbnail
    /// Returns attachment:// URL for Discord embeds
    /// </summary>
    public static string? GetThumbnailUrl(string? filename)
    {
        if (string.IsNullOrEmpty(filename) || _thumbnailsDirectory == null)
            return null;

        var specificPath = Path.Combine(_thumbnailsDirectory, filename);
        if (File.Exists(specificPath))
            return $"attachment://{filename}";

        var defaultPath = Path.Combine(_thumbnailsDirectory, "default.jpg");
        return File.Exists(defaultPath) ? "attachment://default.jpg" : $"attachment://{filename}";
    }

    /// <summary>
    /// Check if a thumbnail exists (either specific or default)
    /// </summary>
    public static bool HasThumbnail(string? filename)
    {
        if (string.IsNullOrEmpty(filename) || _thumbnailsDirectory == null)
            return false;

        var specificPath = Path.Combine(_thumbnailsDirectory, filename);
        var defaultPath = Path.Combine(_thumbnailsDirectory, "default.jpg");

        return File.Exists(specificPath) || File.Exists(defaultPath);
    }

    /// <summary>
    /// Async version for better performance in UI contexts
    /// </summary>
    public static async Task<bool> ThumbnailExistsAsync(string? filename)
    {
        return await Task.Run(() => HasThumbnail(filename));
    }

    /// <summary>
    /// Get the configured thumbnails directory
    /// </summary>
    public static string? ThumbnailsDirectory => _thumbnailsDirectory;
}
