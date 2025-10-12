using System;
using System.Collections.Generic;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Models.Common
{
    /// <summary>
    /// Represents a player in the game system, independent of Discord users.
    /// Players are always part of a team, even in 1v1 matches.
    /// </summary>
    [EntityMetadata(
        tableName: "players",
        archiveTableName: "player_archive",
        maxCacheSize: 500,
        cacheExpiryMinutes: 30,
        servicePropertyName: "Players",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class Player : Entity, IPlayerEntity
    {
        public Guid MashinaUserId { get; set; }
        public MashinaUser MashinaUser { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public DateTime LastActive { get; set; }

        public int TeamJoinLimit { get; set; }
        public Dictionary<Guid, DateTime> TeamJoinCooldowns { get; set; } = [];
        public List<Guid> TeamIds { get; set; } = [];

        /// <summary>
        /// Key: Platform name (e.g., "Discord", "Steam"), Value: List of user IDs from that platform.
        /// </summary>
        public Dictionary<string, List<string>> PreviousPlatformIds { get; set; } = [];

        // Game usernames parsed from submitted replay data
        public string? GameUsername { get; set; }
        public List<string> PreviousGameUsernames { get; set; } = [];

        public override Domain Domain => Domain.Common;
    }
}
