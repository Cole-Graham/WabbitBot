using System;
using System.IO;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models.Common;

public enum MapDensity
{
    Low,
    Medium,
    High,
}

/// <summary>
/// Represents a map in the game, including its metadata and pool settings.
/// </summary>
[EntityMetadata(
    tableName: "maps",
    archiveTableName: "map_archive",
    maxCacheSize: 100,
    cacheExpiryMinutes: 60,
    servicePropertyName: "Maps",
    emitArchiveRegistration: true
)]
public class Map : Entity, IMapEntity
{
    /// <summary>
    /// The display name of the map.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The scenario name of the map.
    /// This is the name in the replay files and MapPack.ndf
    /// </summary>
    public string ScenarioName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the map.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the map is currently active in the system.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The size category of the map (e.g., "1v1", "2v2", etc.) in terms of the number of players.
    /// </summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>
    /// The density in terms of number of players (and in turn, units) relative to the physical size of the map.
    /// The overall density of units significantly impacts the balance of the game and optimal deck building.
    /// </summary>
    public MapDensity Density { get; set; }

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
