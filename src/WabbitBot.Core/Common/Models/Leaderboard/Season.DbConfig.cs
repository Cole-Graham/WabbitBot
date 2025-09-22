using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for Season entities
    /// </summary>
    public class SeasonDbConfig : EntityConfig<Season>, IEntityConfig
    {
        public SeasonDbConfig() : base(
            tableName: "seasons",
            archiveTableName: "season_archive",
            columns: new[] {
                "id", "season_group_id", "game_size", "start_date", "end_date",
                "is_active", "participating_teams", "config", "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 25,
            defaultCacheExpiry: TimeSpan.FromMinutes(30)
        )
        {
        }
    }

    /// <summary>
    /// Configuration for SeasonConfig entities (runtime season configuration)
    /// </summary>
    public class SeasonConfigDbConfig : EntityConfig<SeasonConfig>, IEntityConfig
    {
        public SeasonConfigDbConfig() : base(
            tableName: "season_configs",
            archiveTableName: "season_config_archive",
            columns: new[] {
                "id", "season_id", "rating_decay_enabled", "decay_rate_per_week",
                "minimum_rating", "created_at", "updated_at"
            },
            maxCacheSize: 100,
            defaultCacheExpiry: TimeSpan.FromMinutes(60)
        )
        { }
    }

    /// <summary>
    /// Configuration for SeasonGroup entities
    /// </summary>
    public class SeasonGroupDbConfig : EntityConfig<SeasonGroup>, IEntityConfig
    {
        public SeasonGroupDbConfig() : base(
            tableName: "season_groups",
            archiveTableName: "season_group_archive",
            columns: new[] {
                "id", "name", "description", "created_at", "updated_at"
            },
            maxCacheSize: 50,
            defaultCacheExpiry: TimeSpan.FromHours(2)
        )
        { }
    }
}
