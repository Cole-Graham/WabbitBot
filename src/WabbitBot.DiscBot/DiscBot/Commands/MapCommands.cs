using System.Text.Json;
using WabbitBot.Core.Common.Handlers;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.DiscBot.DiscBot.Commands
{
    /// <summary>
    /// Pure business logic for map commands - no Discord dependencies
    /// </summary>
    public class MapCommands
    {
        private static readonly MapHandler MapHandler = MapHandler.Instance;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        #region Business Logic Methods

        public MapListResult ListMaps(string size = "all", int inRandomPoolInt = -1)
        {
            bool? inRandomPool = inRandomPoolInt switch
            {
                0 => false,
                1 => true,
                _ => null,
            };

            var maps = MapHandler.GetMaps(size, inRandomPool);
            return new MapListResult
            {
                Maps = maps.ToList(),
                Size = size,
                InRandomPool = inRandomPool,
                HasMaps = maps.Any()
            };
        }

        public MapResult GetRandomMap()
        {
            var map = MapHandler.GetRandomMap();
            return new MapResult
            {
                Map = map,
                Success = map != null,
                ErrorMessage = map == null ? "No maps found in the random pool" : null
            };
        }

        public MapResult GetMapByName(string mapName)
        {
            var map = MapHandler.GetMapByName(mapName);
            return new MapResult
            {
                Map = map,
                Success = map != null,
                ErrorMessage = map == null ? $"Map '{mapName}' not found." : null
            };
        }

        public MapResult AddOrUpdateMap(string name, string size, string? thumbnail = null, bool inRandomPool = true, bool inTournamentPool = true)
        {
            var map = new Map
            {
                Name = name,
                Size = size,
                ThumbnailFilename = thumbnail,
                IsInRandomPool = inRandomPool,
                IsInTournamentPool = inTournamentPool,
            };

            MapHandler.AddOrUpdateMapAsync(map).Wait();
            return new MapResult
            {
                Map = map,
                Success = true,
                Message = $"Map '{name}' has been added/updated successfully."
            };
        }

        public string ExportMaps()
        {
            var maps = MapHandler.GetMaps();
            return JsonSerializer.Serialize(maps, JsonOptions);
        }

        public async Task<MapResult> ImportMapsAsync(string json)
        {
            try
            {
                await MapHandler.ImportMapsAsync(json);
                return new MapResult
                {
                    Success = true,
                    Message = "Maps configuration imported successfully."
                };
            }
            catch (Exception ex)
            {
                return new MapResult
                {
                    Success = false,
                    ErrorMessage = $"Error importing maps: {ex.Message}"
                };
            }
        }

        public MapResult RemoveMap(string name)
        {
            var map = MapHandler.GetMapByName(name);
            if (map == null)
            {
                return new MapResult
                {
                    Success = false,
                    ErrorMessage = $"Map '{name}' not found."
                };
            }

            MapHandler.RemoveMapAsync(name).Wait();
            return new MapResult
            {
                Success = true,
                Message = $"Map '{name}' has been removed."
            };
        }

        #endregion
    }

    #region Result Classes

    public class MapListResult
    {
        public List<Map> Maps { get; set; } = new();
        public string Size { get; set; } = string.Empty;
        public bool? InRandomPool { get; set; }
        public bool HasMaps { get; set; }
    }

    public class MapResult
    {
        public Map? Map { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion
}