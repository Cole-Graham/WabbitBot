using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for Map entities
    /// </summary>
    public class MapConfig : EntityConfig<Map>, IEntityConfig
    {
        public MapConfig() : base(
            tableName: "maps",
            archiveTableName: "map_archive",
            columns: new[] {
                "id", "name", "description", "is_active", "size",
                "is_in_random_pool", "is_in_tournament_pool", "thumbnail_filename",
                "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 100,
            defaultCacheExpiry: TimeSpan.FromHours(6)
        )
        {
        }
    }
}
