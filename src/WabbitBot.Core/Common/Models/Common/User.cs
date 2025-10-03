using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models.Common
{
    /// <summary>
    /// Represents a Discord user in the system.
    /// </summary>
    [EntityMetadata(
        tableName: "users",
        archiveTableName: "user_archive",
        maxCacheSize: 1000,
        cacheExpiryMinutes: 60,
        servicePropertyName: "Users",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class User : Entity, IUserEntity
    {
        public string DiscordId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime JoinedAt { get; set; }
        public DateTime LastActive { get; set; }
        public bool IsActive { get; set; }
        public Guid? PlayerId { get; set; } // Reference to associated Player entity

        public override Domain Domain => Domain.Common;
    }
}