using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for Player entities
    /// </summary>
    public class PlayerDbConfig : EntityConfig<Player>, IEntityConfig
    {
        public PlayerDbConfig() : base(
            tableName: "players",
            archiveTableName: "player_archive",
            columns: new[] {
                "id", "name", "last_active", "is_archived", "archived_at",
                "team_ids", "previous_user_ids", "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 500,
            defaultCacheExpiry: TimeSpan.FromMinutes(30)
        )
        {
        }
    }
}
