using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models.Common
{
    /// <summary>
    /// Represents a Discord user in the system.
    /// </summary>
    [EntityMetadata(
        tableName: "mashina_users",
        archiveTableName: "mashina_user_archive",
        maxCacheSize: 1000,
        cacheExpiryMinutes: 60,
        servicePropertyName: "MashinaUsers",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class MashinaUser : Entity, IMashinaUserEntity
    {
        // Navigation properties
        public Guid? PlayerId { get; set; } // Reference to associated Player entity
        public Player? Player { get; set; } // Reference to associated Player entity

        // Discord properties
        public ulong DiscordUserId { get; set; }
        public string? DiscordUsername { get; set; }
        public List<string> PreviousDiscordUsernames { get; set; } = [];
        public string? DiscordGlobalname { get; set; }
        public List<string> PreviousDiscordGlobalnames { get; set; } = [];
        public string? DiscordMention { get; set; }
        public List<string> PreviousDiscordMentions { get; set; } = [];
        public string? DiscordAvatarUrl { get; set; }

        // Data properties
        public DateTime JoinedAt { get; set; }
        public DateTime LastActive { get; set; }
        public bool IsActive { get; set; }

        public override Domain Domain => Domain.Common;
    }
}
