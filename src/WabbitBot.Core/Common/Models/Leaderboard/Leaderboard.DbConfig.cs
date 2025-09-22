using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for Leaderboard entities
    /// </summary>
    public class LeaderboardDbConfig : EntityConfig<Leaderboard>, IEntityConfig
    {
        public LeaderboardDbConfig() : base(
            tableName: "leaderboards",
            archiveTableName: "leaderboard_archive",
            columns: new[] {
                "id", "rankings", "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 10,
            defaultCacheExpiry: TimeSpan.FromHours(1)
        )
        {
        }
    }

    /// <summary>
    /// Configuration for LeaderboardItem entities
    /// </summary>
    public class LeaderboardItemDbConfig : EntityConfig<LeaderboardItem>, IEntityConfig
    {
        public LeaderboardItemDbConfig() : base(
            tableName: "leaderboard_items",
            archiveTableName: "leaderboard_item_archive",
            columns: new[] {
                "id", "leaderboard_id", "player_ids", "team_id", "name", "wins",
                "losses", "rating", "last_updated", "is_team", "created_at", "updated_at"
            },
            maxCacheSize: 5000,
            defaultCacheExpiry: TimeSpan.FromMinutes(5)
        )
        { }
    }
}
