using System.Text.Json;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Services;

public class MapService
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

    public static MapService Instance { get; } = new();

    private MapService()
    {
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
    }

    public async Task ExportMapsAsync(string path)
    {
        var json = JsonSerializer.Serialize(_maps, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task ImportMapsAsync(string json)
    {
        var maps = JsonSerializer.Deserialize<List<Map>>(json, _jsonOptions);
        if (maps == null) throw new JsonException("Invalid maps JSON format");

        _maps = maps;
        await SaveMapsAsync();
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
            _maps.Remove(existingMap);
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
        }
    }
}
