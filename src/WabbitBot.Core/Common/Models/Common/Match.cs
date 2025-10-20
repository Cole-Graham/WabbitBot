using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models.Common
{
    #region TeamSize
    public enum TeamSize
    {
        OneVOne = 0,
        TwoVTwo = 1,
        ThreeVThree = 2,
        FourVFour = 3,
    }

    public enum TeamSizeRosterGroup
    {
        Solo = 0, // 1v1
        Duo = 1, // 2v2
        Squad = 2, // 3v3 and 4v4
    }

    /// <summary>
    /// Extension methods for TeamSize enum
    /// </summary>
    public static class TeamSizeExtensions
    {
        /// <summary>
        /// Converts TeamSize to string format (e.g., "1v1", "2v2")
        /// Uses the enum's integer value to compute player count
        /// </summary>
        public static string ToSizeString(this TeamSize teamSize)
        {
            var playerCount = (int)teamSize + 1;
            return $"{playerCount}v{playerCount}";
        }

        /// <summary>
        /// Gets the number of players per team for this TeamSize
        /// </summary>
        public static int GetPlayersPerTeam(this TeamSize teamSize)
        {
            return (int)teamSize + 1;
        }

        // Note: For roster group conversion, use TeamCore.TeamSizeRosterGrouping.GetRosterGroup()
    }
    #endregion

    #region Match
    [EntityMetadata(
        tableName: "matches",
        archiveTableName: "match_archive",
        maxCacheSize: 200,
        cacheExpiryMinutes: 10,
        servicePropertyName: "Matches",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class Match : Entity, IMatchEntity
    {
        // Navigation properties
        public Guid Team1Id { get; set; }
        public Guid Team2Id { get; set; }
        public List<Guid> Team1PlayerIds { get; set; } = []; // Selected players for challenger team
        public List<Guid> Team2PlayerIds { get; set; } = []; // Selected players for opponent team
        public virtual Team? Team1 { get; set; }
        public virtual Team? Team2 { get; set; }

        // Foreign key collections
        public ICollection<Guid> StateHistoryIds { get; set; } = [];
        public ICollection<Guid> GameIds { get; set; } = [];

        // Navigation properties
        public virtual ICollection<MatchStateSnapshot> StateHistory { get; set; } = [];
        public virtual ICollection<Game> Games { get; set; } = [];

        // Core match data
        public TeamSize TeamSize { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? WinnerId { get; set; }
        public Guid? ParentId { get; set; } // ID of parent Scrimmage, Tournament, or Casual match
        public MatchParentType? ParentType { get; set; } // Type of parent (Scrimmage, Tournament, or Casual)
        public int BestOf { get; set; } = 1; // Number of games to win the match
        public bool PlayToCompletion { get; set; } // Used for tournament matches.

        // Map ban properties
        public List<string> AvailableMaps { get; set; } = []; // Maps available for the match
        public List<string> FinalMapPool { get; set; } = []; // Maps used in the match
        public List<string> Team1MapBans { get; set; } = []; // Maps banned by team 1
        public List<string> Team2MapBans { get; set; } = []; // Maps banned by team 2
        public DateTime? Team1MapBansConfirmedAt { get; set; }
        public DateTime? Team2MapBansConfirmedAt { get; set; }

        // Discord Thread Management
        public ulong? ChannelId { get; set; } // Discord channel ID where the match threads were created
        public ulong? Team1ThreadId { get; set; } // Discord thread ID for Team (private thread)
        public ulong? Team2ThreadId { get; set; }
        public ulong? Team1OverviewContainerMsgId { get; set; } // Discord message Id for match overview container
        public ulong? Team2OverviewContainerMsgId { get; set; }
        public ulong? Team1MatchResultsMsgId { get; set; } // Discord message Id for match results container
        public ulong? Team2MatchResultsMsgId { get; set; }

        public override Domain Domain => Domain.Common;
    }
    #endregion

    #region # MatchStateSnapshot
    /// <summary>
    /// Immutable snapshot of match state at a point in time.
    ///
    /// Design Principles:
    /// 1. IMMUTABLE - Never modified after creation
    /// 2. COMPLETE - Contains all data needed to reconstruct state
    /// 3. DENORMALIZED - Duplicates data for historical completeness
    /// 4. NO NAVIGATION TO MUTABLE ENTITIES - Only reference by ID
    ///
    /// Responsibilities:
    /// - Match lifecycle tracking (created, started, completed, cancelled, forfeited)
    /// - Map ban process (match-level, happens before games start)
    /// - Overall match progression and score
    /// - Winner determination
    ///
    /// Forfeit Handling:
    /// - When a match is forfeited, forfeit properties are set
    /// - CurrentGameNumber indicates which game was active during forfeit
    /// - The corresponding Game also gets a forfeit snapshot for historical record
    /// </summary>
    [EntityMetadata(
        tableName: "match_state_snapshots",
        archiveTableName: "match_state_snapshot_archive",
        maxCacheSize: 500,
        cacheExpiryMinutes: 10,
        servicePropertyName: "MatchStateSnapshots",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class MatchStateSnapshot : Entity, IMatchEntity
    {
        // Navigation properties
        public Guid MatchId { get; set; }

        // Snapshot metadata
        public Guid TriggeredByUserId { get; set; } = Guid.Empty; // User who triggered this state change
        public string TriggeredByUserName { get; set; } = string.Empty; // Username (denormalized for historical completeness)
        public Dictionary<string, object> AdditionalData { get; set; } = [];

        // Match lifecycle properties
        public MatchStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? ForfeitedAt { get; set; }

        // Match status properties
        public Guid? WinnerId { get; set; }
        public Guid? CancelledByUserId { get; set; }
        public Guid? ForfeitedByUserId { get; set; }
        public Guid? ForfeitedTeamId { get; set; }
        public string? CancellationReason { get; set; }
        public string? ForfeitReason { get; set; }

        // Game progression properties
        public int CurrentGameNumber { get; set; } = 1;
        public Guid? CurrentMapId { get; set; }

        // Final match results
        public string? FinalScore { get; set; }

        // Map ban state properties
        public List<string> AvailableMaps { get; set; } = []; // Maps available after bans and played games
        public List<string> FinalMapPool { get; set; } = []; // Maps available after bans
        public List<string> Team1MapBans { get; set; } = [];
        public List<string> Team2MapBans { get; set; } = [];
        public bool Team1BansSubmitted { get; set; }
        public bool Team2BansSubmitted { get; set; }
        public bool Team1BansConfirmed { get; set; }
        public bool Team2BansConfirmed { get; set; }

        public override Domain Domain => Domain.Common;
    }
    #endregion

    public enum MatchStatus
    {
        Created,
        InProgress,
        Completed,
        Cancelled,
        Forfeited,
    }

    public enum GameStatus
    {
        Created,
        InProgress,
        Completed,
        Cancelled,
        Forfeited,
    }

    /// <summary>
    /// Type of parent entity that owns a match.
    /// If both ParentId and ParentType are null, the match is a casual/standalone match with no parent.
    /// </summary>
    public enum MatchParentType
    {
        Scrimmage, // Match belongs to a Scrimmage entity
        Tournament, // Match belongs to a Tournament entity
    }

    #region Game
    [EntityMetadata(
        tableName: "games",
        archiveTableName: "game_archive",
        maxCacheSize: 200,
        cacheExpiryMinutes: 10,
        servicePropertyName: "Games",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class Game : Entity, IMatchEntity
    {
        // Navigation properties
        public Guid MatchId { get; set; }
        public virtual Match Match { get; set; } = null!;

        public Guid MapId { get; set; }
        public virtual Map Map { get; set; } = null!;

        public Guid? Team1DivisionId { get; set; }
        public virtual Division? Team1Division { get; set; }

        public Guid? Team2DivisionId { get; set; }
        public virtual Division? Team2Division { get; set; }

        // Foreign key collections
        public ICollection<Guid> StateHistoryIds { get; set; } = [];
        public ICollection<Guid> ReplayIds { get; set; } = [];

        // Navigation properties
        public virtual ICollection<GameStateSnapshot> StateHistory { get; set; } = [];
        public virtual ICollection<Replay> Replays { get; set; } = [];

        // Game data
        public TeamSize TeamSize { get; set; }
        public int GameNumber { get; set; } // Position in the match (1-based)
        public List<Guid> Team1PlayerIds { get; set; } = [];
        public List<Guid> Team2PlayerIds { get; set; } = [];

        // Discord Message Management
        public ulong? Team1GameContainerMsgId { get; set; } // Discord message Id for game container in Team1 thread
        public ulong? Team2GameContainerMsgId { get; set; } // Discord message Id for game container in Team2 thread

        public override Domain Domain => Domain.Common;
    }
    #endregion

    #region GameStateSnapshot
    /// <summary>
    /// Immutable snapshot of game state at a point in time.
    ///
    /// Design Principles:
    /// 1. IMMUTABLE - Never modified after creation
    /// 2. COMPLETE - Contains all data needed to reconstruct state without joins
    /// 3. DENORMALIZED - Duplicates parent data (MatchId, MapId, etc.) for historical completeness
    /// 4. NO NAVIGATION TO MUTABLE ENTITIES (except via explicit navigation properties)
    ///
    /// Responsibilities:
    /// - Game lifecycle tracking (created, started, completed, cancelled, forfeited)
    /// - Deck submission tracking (per-game, not per-match)
    /// - Game-specific winner determination
    /// - Historical record of game state changes
    ///
    /// Deck Code Submission:
    /// - Deck codes are GAME-SPECIFIC and submitted before each game starts
    /// - Players may submit different deck codes for each game in a match
    /// - Stored here (not in MatchStateSnapshot) because this is game-level state
    /// - Each game's deck submission is independent of other games
    ///
    /// Forfeit Handling:
    /// - When a match is forfeited during this game, forfeit properties are set
    /// - Forfeiting a game ALWAYS forfeits the entire match
    /// - Game forfeit properties MIRROR the match forfeit for historical record
    /// - These properties show which game was active when the match was forfeited
    /// - Cannot be set independently - always set via match forfeit logic
    ///
    /// Denormalized Data:
    /// - MatchId, MapId, TeamSize, PlayerIds, GameNumber are duplicated from Game entity
    /// - This ensures historical completeness even if Game entity is modified/deleted
    /// - Enables archive queries without requiring joins to reconstruct state
    /// </summary>
    [EntityMetadata(
        tableName: "game_state_snapshots",
        archiveTableName: "game_state_snapshot_archive",
        maxCacheSize: 300,
        cacheExpiryMinutes: 5,
        servicePropertyName: "GameStateSnapshots",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class GameStateSnapshot : Entity, IMatchEntity
    {
        // Navigation properties
        // Direct navigation to Match enables efficient access during state processing
        // Avoids two-hop navigation (GameSnapshot → Game → Match) for better performance
        public Guid GameId { get; set; }
        public virtual Game? Game { get; set; }

        public Guid MatchId { get; set; } // Denormalized FK (also in Game entity)
        public virtual Match? Match { get; set; }

        // Snapshot metadata
        public DateTime Timestamp { get; set; }
        public Guid TriggeredByUserId { get; set; } = Guid.Empty; // User who triggered this state change
        public string TriggeredByUserName { get; set; } = string.Empty; // Username (denormalized for historical completeness)
        public Dictionary<string, object> AdditionalData { get; set; } = [];

        // Game lifecycle properties
        public GameStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? ForfeitedAt { get; set; }

        // Game status properties
        public Guid? WinnerId { get; set; }
        public Guid? CancelledByUserId { get; set; }

        // Forfeit properties - These mirror Match forfeit when this game was active during forfeit
        // Forfeiting a game always forfeits the entire match
        // These create a historical record of which game was active when match was forfeited
        public Guid? ForfeitedByUserId { get; set; }
        public Guid? ForfeitedTeamId { get; set; }
        public string? CancellationReason { get; set; }
        public string? ForfeitReason { get; set; }

        // Deck submission state properties (per-game, not per-match)
        // Players submit NEW deck codes before EACH game starts
        // Dictionary: PlayerId -> DeckCode
        public Dictionary<Guid, string> PlayerDeckCodes { get; set; } = [];
        public Dictionary<Guid, DateTime> PlayerDeckSubmittedAt { get; set; } = [];
        public HashSet<Guid> PlayerDeckConfirmed { get; set; } = [];
        public Dictionary<Guid, DateTime> PlayerDeckConfirmedAt { get; set; } = [];

        // Game progression properties (denormalized from Game entity for historical completeness)
        // These are duplicated to ensure archive queries work without joins
        // and to preserve complete historical record even if Game entity is modified
        public Guid MapId { get; set; }
        public TeamSize TeamSize { get; set; }
        public List<Guid> Team1PlayerIds { get; set; } = [];
        public List<Guid> Team2PlayerIds { get; set; } = [];
        public int GameNumber { get; set; } = 1;

        // Division tracking (denormalized for historical completeness)
        public Guid? Team1DivisionId { get; set; }
        public Guid? Team2DivisionId { get; set; }

        public override Domain Domain => Domain.Common;
    }
    #endregion
}
