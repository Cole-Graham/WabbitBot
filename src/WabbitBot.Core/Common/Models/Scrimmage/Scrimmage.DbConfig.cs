using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for Scrimmage entities
    /// </summary>
    public class ScrimmageDbConfig : EntityConfig<Scrimmage>, IEntityConfig
    {
        public ScrimmageDbConfig() : base(
            tableName: "scrimmages",
            archiveTableName: "scrimmage_archive",
            columns: new[] {
                "id", "team1_id", "team2_id", "team1_roster_ids", "team2_roster_ids",
                "game_size", "started_at", "completed_at", "winner_id", "status",
                "team1_rating", "team2_rating", "team1_rating_change", "team2_rating_change",
                "team1_confidence", "team2_confidence", "team1_score", "team2_score",
                "challenge_expires_at", "is_accepted", "best_of", "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 150,
            defaultCacheExpiry: TimeSpan.FromMinutes(15)
        )
        {
        }
    }
}
