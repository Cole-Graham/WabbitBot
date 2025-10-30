using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models.Common
{
    [EntityMetadata(
        tableName: "discord_threads",
        archiveTableName: "discord_thread_archive",
        maxCacheSize: 1000,
        cacheExpiryMinutes: 60,
        servicePropertyName: "DiscordThreads",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class DiscordThreadTracking : Entity, IMatchEntity
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong ThreadId { get; set; }
        public ulong MessageId { get; set; }
        public ulong CreatorDiscordUserId { get; set; }
        public string Feature { get; set; } = string.Empty;
        public DateTime LastActivityAt { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public override Domain Domain => Domain.Common;
    }
}
