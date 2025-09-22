using System.Text.Json;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Handlers;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Commands
{
    /// <summary>
    /// Pure business logic for map commands - no Discord dependencies
    /// </summary>
    [WabbitCommand("Maps")]
    public partial class MapCommands
    {
        private static MapService GetMapService() => new MapService();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        #region Business Logic Methods

        public Result<List<Map>> ListMaps(string size = "all", int inRandomPoolInt = -1)
        {
            bool? inRandomPool = inRandomPoolInt switch
            {
                0 => false,
                1 => true,
                _ => null,
            };

            var maps = GetMapService().GetMaps(size, inRandomPool);
            var metadata = new Dictionary<string, object>
            {
                ["Size"] = size,
                ["InRandomPool"] = inRandomPool?.ToString() ?? "all",
                ["Count"] = maps.Count()
            };

            return Result<List<Map>>.CreateSuccess(maps.ToList(), metadata: metadata);
        }

        public Result<Map> GetRandomMap()
        {
            var map = GetMapService().GetRandomMap();
            if (map == null)
            {
                return Result<Map>.Failure("No maps found in the random pool");
            }
            return Result<Map>.CreateSuccess(map);
        }

        public Result<Map> GetMapByName(string mapName)
        {
            var map = GetMapService().GetMapByName(mapName);
            if (map == null)
            {
                return Result<Map>.Failure($"Map '{mapName}' not found.");
            }
            return Result<Map>.CreateSuccess(map);
        }

        public async Task<Result<Map>> AddOrUpdateMapAsync(string name, string size, string? thumbnail = null, bool inRandomPool = true, bool inTournamentPool = true)
        {
            var map = new Map
            {
                Name = name,
                Size = size,
                ThumbnailFilename = thumbnail,
                IsInRandomPool = inRandomPool,
                IsInTournamentPool = inTournamentPool,
            };

            await GetMapService().AddOrUpdateMapAsync(map);

            return Result<Map>.CreateSuccess(map, $"Map '{name}' has been added/updated successfully.");
        }

        public Result<string> ExportMaps()
        {
            try
            {
                var maps = GetMapService().GetMaps();
                var json = JsonSerializer.Serialize(maps, JsonOptions);
                return Result<string>.CreateSuccess(json);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure($"Error exporting maps: {ex.Message}");
            }
        }

        public async Task<Result> ImportMapsAsync(string json)
        {
            try
            {
                await GetMapService().ImportMapsAsync(json);
                return Result.CreateSuccess("Maps configuration imported successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"Error importing maps: {ex.Message}");
            }
        }

        public Result<Map> RemoveMap(string name)
        {
            var map = GetMapService().GetMapByName(name);
            if (map == null)
            {
                return Result<Map>.Failure($"Map '{name}' not found.");
            }

            GetMapService().RemoveMapAsync(name).Wait();
            return Result<Map>.CreateSuccess(map, $"Map '{name}' has been removed.");
        }

        #endregion
    }

}