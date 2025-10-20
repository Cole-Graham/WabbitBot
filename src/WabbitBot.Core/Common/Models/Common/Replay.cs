using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models.Common
{
    #region ReplayPlayer
    [EntityMetadata(
        tableName: "replay_players",
        archiveTableName: "replay_player_archive",
        maxCacheSize: 500,
        cacheExpiryMinutes: 60,
        servicePropertyName: "ReplayPlayers",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class ReplayPlayer : Entity, IReplayEntity
    {
        // Navigation properties
        public Guid ReplayId { get; set; }
        public virtual Replay Replay { get; set; } = null!;

        // Player data from replay file
        public string PlayerUserId { get; set; } = string.Empty; // Eugen Systems user ID
        public string PlayerName { get; set; } = string.Empty;
        public string? PlayerElo { get; set; }
        public string? PlayerLevel { get; set; }
        public string PlayerAlliance { get; set; } = string.Empty; // 0 or 1
        public string? PlayerScoreLimit { get; set; }
        public string? PlayerIncomeRate { get; set; }
        public string? PlayerAvatar { get; set; }
        public string? PlayerReady { get; set; }
        public string? PlayerDeckContent { get; set; } // Base64 encoded deck data
        public string? PlayerDeckName { get; set; }

        public override Domain Domain => Domain.Common;
    }
    #endregion

    #region Replay
    [EntityMetadata(
        tableName: "replays",
        archiveTableName: "replay_archive",
        maxCacheSize: 200,
        cacheExpiryMinutes: 60,
        servicePropertyName: "Replays",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class Replay : Entity, IReplayEntity
    {
        // Foreign key collection for players
        public ICollection<Guid> PlayerIds { get; set; } = [];

        // Navigation properties
        public virtual ICollection<ReplayPlayer> Players { get; set; } = [];

        // Optional link to match if this replay was uploaded for a match
        public Guid MatchId { get; set; }
        public Guid GameId { get; set; }

        // Game-level data from replay file
        public string GameMode { get; set; } = string.Empty;
        public string? AllowObservers { get; set; }
        public string? ObserverDelay { get; set; }
        public string? Seed { get; set; }
        public string? Private { get; set; }
        public string? ServerName { get; set; }
        public string? Version { get; set; }
        public string? UniqueSessionId { get; set; }
        public string? ModList { get; set; }
        public string? ModTagList { get; set; }
        public string? EnvironmentSettings { get; set; }
        public string? GameType { get; set; }
        public string Map { get; set; } = string.Empty;
        public string? InitMoney { get; set; }
        public string? TimeLimit { get; set; }
        public string? ScoreLimit { get; set; }
        public string? CombatRule { get; set; }
        public string? IncomeRate { get; set; }
        public string? Upkeep { get; set; }

        // File metadata
        public string? OriginalFilename { get; set; }
        public string? FilePath { get; set; } // Secure filename in the file system (e.g., "guid.rpl3")
        public long? FileSizeBytes { get; set; }

        // Match result data
        public string? VictoryCode { get; set; } // "0"-"2" = Defeat, "4"-"6" = Victory, other = Draw
        public int? DurationSeconds { get; set; } // Actual game duration from replay

        public override Domain Domain => Domain.Common;
    }
    #endregion
}
