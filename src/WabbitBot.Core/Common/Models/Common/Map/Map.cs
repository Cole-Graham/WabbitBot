using System;
using System.IO;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;
using WabbitBot.Common.Configuration;

namespace WabbitBot.Core.Common.Models;

/// <summary>
/// Represents a map in the game, including its metadata and pool settings.
/// </summary>
[EntityMetadata(
    tableName: "maps",
    archiveTableName: "map_archive",
    maxCacheSize: 100,
    cacheExpiryMinutes: 60,
    servicePropertyName: "Maps"
)]
public class Map : Entity, IMapEntity
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
    public override Domain Domain => Domain.Common;


    // Removed static class Validation.
}