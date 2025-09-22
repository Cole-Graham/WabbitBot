using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for Team entities
    /// </summary>
    public class TeamDbConfig : EntityConfig<Team>, IEntityConfig
    {
        public TeamDbConfig() : base(
            tableName: "teams",
            archiveTableName: "team_archive",
            columns: new[] {
                "id", "name", "team_captain_id", "team_size", "max_roster_size",
                "last_active", "is_archived", "archived_at", "tag", "stats",
                "roster", "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 200,
            defaultCacheExpiry: TimeSpan.FromMinutes(20)
        )
        {
        }
    }

    /// <summary>
    /// Configuration for Stats entities
    /// </summary>
    public class StatsDbConfig : EntityConfig<Stats>, IEntityConfig
    {
        public StatsDbConfig() : base(
            tableName: "stats",
            archiveTableName: "stats_archive",
            columns: new[] {
                "id", "team_id", "game_size", "wins", "losses", "initial_rating",
                "current_rating", "highest_rating", "current_streak", "longest_streak",
                "last_match_at", "last_updated", "opponent_distribution", "recent_matches_count",
                "created_at", "updated_at"
            },
            maxCacheSize: 1000,
            defaultCacheExpiry: TimeSpan.FromMinutes(30)
        )
        { }
    }
}
