using System.Text.Json;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Models;
using WabbitBot.Common.ErrorHandling;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Service for managing map operations and business logic
/// </summary>
public class MapService : CoreService
{
    private List<Map> _maps = [];
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private MapsOptions _mapsConfig;

    public MapService() : base(CoreEventBus.Instance, CoreErrorHandler.Instance)
    {
        _mapsConfig = ConfigurationProvider.GetSection<MapsOptions>(MapsOptions.SectionName);
        EnsureThumbnailsDirectory();
        LoadMaps();
    }

    private void EnsureThumbnailsDirectory()
    {
        const string thumbnailsDirectory = "data/maps/thumbnails";
        if (!Directory.Exists(thumbnailsDirectory))
        {
            Directory.CreateDirectory(thumbnailsDirectory);
        }
    }

    private void LoadMaps()
    {
        // Load maps directly from configuration
        _maps = _mapsConfig.Maps.Select(config => new Map
        {
            Name = config.Name,
            Size = config.Size,
            ThumbnailFilename = config.ThumbnailFilename,
            IsInRandomPool = config.IsInRandomPool,
            IsInTournamentPool = config.IsInTournamentPool
        }).ToList();
    }

    public async Task SaveMapsAsync()
    {
        // Update configuration with current maps
        _mapsConfig.Maps = _maps.Select(map => new MapConfiguration
        {
            Name = map.Name,
            Size = map.Size,
            ThumbnailFilename = map.ThumbnailFilename,
            IsInRandomPool = map.IsInRandomPool,
            IsInTournamentPool = map.IsInTournamentPool
        }).ToList();

        // Save configuration back to appsettings.json
        var success = await ConfigurationService.Persistence.SaveMapsConfigurationAsync(_mapsConfig);
        if (!success)
        {
            Console.WriteLine("Warning: Failed to save maps configuration to appsettings.json");
        }

        await EventBus.PublishAsync(new MapsSavedEvent { Maps = _maps });
    }

    public async Task ExportMapsAsync(string path)
    {
        var json = JsonSerializer.Serialize(_maps, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
        await EventBus.PublishAsync(new MapsExportedEvent { Path = path });
    }

    public async Task ImportMapsAsync(string json)
    {
        var maps = JsonSerializer.Deserialize<List<Map>>(json, _jsonOptions);
        if (maps == null) throw new JsonException("Invalid maps JSON format");

        _maps = maps;
        await SaveMapsAsync();
        await EventBus.PublishAsync(new MapsImportedEvent { Maps = maps });
    }

    public Map? GetRandomMap()
    {
        var randomMaps = _maps.Where(m => m.IsInRandomPool).ToList();
        if (randomMaps.Count == 0) return null;

        var random = new Random();
        return randomMaps[random.Next(randomMaps.Count)];
    }

    public Map? GetMapByName(string name)
    {
        return _maps.FirstOrDefault(m =>
            string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<Map> GetMaps(string size = "all", bool? inRandomPool = null)
    {
        var query = _maps.AsEnumerable();

        if (size != "all")
        {
            query = query.Where(m =>
                string.Equals(m.Size, size, StringComparison.OrdinalIgnoreCase));
        }

        if (inRandomPool.HasValue)
        {
            query = query.Where(m => m.IsInRandomPool == inRandomPool.Value);
        }

        return query.ToList();
    }

    public IEnumerable<string> GetAvailableSizes()
    {
        return _maps
            .Select(m => m.Size)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList()!;
    }

    public async Task AddOrUpdateMapAsync(Map map)
    {
        var existingMap = GetMapByName(map.Name);
        if (existingMap != null)
        {
            // If updating an existing map, preserve its thumbnail if not changed
            if (map.ThumbnailFilename == null && existingMap.ThumbnailFilename != null)
            {
                map.ThumbnailFilename = existingMap.ThumbnailFilename;
            }
            _maps.Remove(existingMap);
            await EventBus.PublishAsync(new MapUpdatedEvent { Map = map, PreviousMap = existingMap });
        }
        else
        {
            await EventBus.PublishAsync(new MapAddedEvent { Map = map });
        }

        _maps.Add(map);
        await SaveMapsAsync();
    }

    public async Task RemoveMapAsync(string name)
    {
        var map = GetMapByName(name);
        if (map != null)
        {
            _maps.Remove(map);
            await SaveMapsAsync();
            await EventBus.PublishAsync(new MapRemovedEvent { Map = map });
        }
    }

    /// <summary>
    /// Updates the thumbnail filename for a map.
    /// Note: The actual file must be manually uploaded to the thumbnails directory.
    /// </summary>
    /// <param name="mapName">The name of the map to update.</param>
    /// <param name="filename">The filename of the thumbnail (must exist in thumbnails directory).</param>
    public async Task UpdateMapThumbnailFilenameAsync(string mapName, string filename)
    {
        var map = GetMapByName(mapName);
        if (map == null)
        {
            throw new ArgumentException($"Map '{mapName}' not found", nameof(mapName));
        }

        // Verify the file exists
        var fullPath = Path.Combine("data/maps/thumbnails", filename);
        if (!File.Exists(fullPath))
        {
            throw new ArgumentException($"Thumbnail file '{filename}' not found in thumbnails directory", nameof(filename));
        }

        var oldFilename = map.ThumbnailFilename;
        map.ThumbnailFilename = filename;
        await SaveMapsAsync();
        await EventBus.PublishAsync(new MapThumbnailUpdatedEvent
        {
            Map = map,
            OldFilename = oldFilename,
            NewFilename = filename
        });
    }

    /// <summary>
    /// Removes the thumbnail filename from a map.
    /// Note: The actual file must be manually deleted from the thumbnails directory.
    /// </summary>
    /// <param name="mapName">The name of the map to update.</param>
    public async Task RemoveMapThumbnailAsync(string mapName)
    {
        var map = GetMapByName(mapName);
        if (map == null)
        {
            throw new ArgumentException($"Map '{mapName}' not found", nameof(mapName));
        }

        var oldFilename = map.ThumbnailFilename;
        map.ThumbnailFilename = null;
        await SaveMapsAsync();
        await EventBus.PublishAsync(new MapThumbnailRemovedEvent
        {
            Map = map,
            OldFilename = oldFilename
        });
    }
}
