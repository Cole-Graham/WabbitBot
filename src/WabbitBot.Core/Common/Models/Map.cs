using System;
using System.IO;
using WabbitBot.Common.Models;
using WabbitBot.Common.Configuration;

namespace WabbitBot.Core.Common.Models;

/// <summary>
/// Represents a map in the game, including its metadata and pool settings.
/// </summary>
public class Map : Entity
{

    /// <summary>
    /// The display name of the map.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the map.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the map is currently active in the system.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The size category of the map (e.g., "1v1", "2v2", etc.).
    /// </summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// Whether the map is included in the random map pool.
    /// </summary>
    public bool IsInRandomPool { get; set; }

    /// <summary>
    /// Whether the map is included in the tournament map pool.
    /// </summary>
    public bool IsInTournamentPool { get; set; }

    /// <summary>
    /// The filename of the map's thumbnail image.
    /// Expected to be manually uploaded to the thumbnails directory.
    /// </summary>
    public string? ThumbnailFilename { get; set; }


    /// <summary>
    /// Validation methods for Map model
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Validates if a map name is valid
        /// </summary>
        public static bool IsValidMapName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length <= 50;
        }

        /// <summary>
        /// Validates if a map size is valid
        /// </summary>
        public static bool IsValidMapSize(string size)
        {
            return !string.IsNullOrWhiteSpace(size) &&
                   size.ToLowerInvariant() is "1v1" or "2v2" or "3v3" or "4v4";
        }

        /// <summary>
        /// Validates if a thumbnail filename is valid
        /// </summary>
        public static bool IsValidThumbnailFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return false;

            var extension = Path.GetExtension(filename).ToLowerInvariant();
            return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp";
        }

        /// <summary>
        /// Validates if a map configuration is complete
        /// </summary>
        public static bool IsCompleteMapConfiguration(Map map)
        {
            return IsValidMapName(map.Name) &&
                   IsValidMapSize(map.Size) &&
                   !string.IsNullOrEmpty(map.ThumbnailFilename);
        }
    }
}