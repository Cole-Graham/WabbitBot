using System.Text.Json;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.BotCore;

namespace WabbitBot.Core.Common.Handlers;

public class MapHandler
{
    private static readonly string ConfigDirectory = "config";
    private static readonly string DefaultMapsFile = Path.Combine(ConfigDirectory, "maps.default.json");
    private static readonly string CustomMapsFile = Path.Combine(ConfigDirectory, "maps.json");

    private List<Map> _maps = [];
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ICoreEventBus _eventBus;

    public static MapHandler Instance { get; } = new();

    private MapHandler()
    {
        _eventBus = CoreEventBus.Instance;
        EnsureConfigDirectory();
        LoadMaps();
    }

    private void EnsureConfigDirectory()
    {
        if (!Directory.Exists(ConfigDirectory))
        {
            Directory.CreateDirectory(ConfigDirectory);
        }

        // If no custom maps file exists, copy from default
        if (!File.Exists(CustomMapsFile) && File.Exists(DefaultMapsFile))
        {
            File.Copy(DefaultMapsFile, CustomMapsFile);
        }
    }

    private void LoadMaps()
    {
        var mapsFile = File.Exists(CustomMapsFile) ? CustomMapsFile : DefaultMapsFile;

        if (!File.Exists(mapsFile))
        {
            _maps = [];
            return;
        }

        var json = File.ReadAllText(mapsFile);
        _maps = JsonSerializer.Deserialize<List<Map>>(json, _jsonOptions) ?? [];
    }

    public async Task SaveMapsAsync()
    {
        var json = JsonSerializer.Serialize(_maps, _jsonOptions);
        await File.WriteAllTextAsync(CustomMapsFile, json);
        await _eventBus.PublishAsync(new MapsSavedEvent { Maps = _maps });
    }

    public async Task ExportMapsAsync(string path)
    {
        var json = JsonSerializer.Serialize(_maps, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
        await _eventBus.PublishAsync(new MapsExportedEvent { Path = path });
    }

    public async Task ImportMapsAsync(string json)
    {
        var maps = JsonSerializer.Deserialize<List<Map>>(json, _jsonOptions);
        if (maps == null) throw new JsonException("Invalid maps JSON format");

        _maps = maps;
        await SaveMapsAsync();
        await _eventBus.PublishAsync(new MapsImportedEvent { Maps = maps });
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
            await _eventBus.PublishAsync(new MapUpdatedEvent { Map = map, PreviousMap = existingMap });
        }
        else
        {
            await _eventBus.PublishAsync(new MapAddedEvent { Map = map });
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
            await _eventBus.PublishAsync(new MapRemovedEvent { Map = map });
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
        var fullPath = Path.Combine("config/maps/thumbnails", filename);
        if (!File.Exists(fullPath))
        {
            throw new ArgumentException($"Thumbnail file '{filename}' not found in thumbnails directory", nameof(filename));
        }

        var oldFilename = map.ThumbnailFilename;
        map.ThumbnailFilename = filename;
        await SaveMapsAsync();
        await _eventBus.PublishAsync(new MapThumbnailUpdatedEvent
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
        await _eventBus.PublishAsync(new MapThumbnailRemovedEvent
        {
            Map = map,
            OldFilename = oldFilename
        });
    }
}