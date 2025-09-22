using System;
using WabbitBot.Core.Common.Config;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Configuration for User entities
    /// </summary>
    public class UserDbConfig : EntityConfig<User>, IEntityConfig
    {
        public UserDbConfig() : base(
            tableName: "users",
            archiveTableName: "user_archive",
            columns: new[] {
                "id", "discord_id", "username", "nickname", "avatar_url",
                "joined_at", "last_active", "is_active", "player_id",
                "created_at", "updated_at"
            },
            idColumn: "id",
            maxCacheSize: 1000,
            defaultCacheExpiry: TimeSpan.FromMinutes(15)
        )
        {
        }
    }
}
