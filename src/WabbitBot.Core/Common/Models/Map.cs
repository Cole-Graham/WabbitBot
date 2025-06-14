using System;
using System.IO;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models;

/// <summary>
/// Represents a map in the game, including its metadata and pool settings.
/// </summary>
public class Map : BaseEntity
{
    private const string ThumbnailsDirectory = "config/maps/thumbnails";

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
    public string? Size { get; set; }

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
    /// Gets the full path to the thumbnail image.
    /// </summary>
    public string? ThumbnailPath => ThumbnailFilename != null
        ? Path.Combine(ThumbnailsDirectory, ThumbnailFilename)
        : null;

    /// <summary>
    /// Gets the URL to the thumbnail image for Discord embeds.
    /// </summary>
    public string? ThumbnailUrl => ThumbnailFilename != null
        ? $"attachment://{ThumbnailFilename}"
        : null;

    /// <summary>
    /// Checks if the thumbnail file exists on disk.
    /// </summary>
    public bool HasThumbnail => ThumbnailPath != null && File.Exists(ThumbnailPath);
}