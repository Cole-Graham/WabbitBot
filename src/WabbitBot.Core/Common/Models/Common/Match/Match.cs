
using WabbitBot.Common.Models;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Models
{
    #region TeamSize
    public enum TeamSize
    {
        OneVOne,
        TwoVTwo,
        ThreeVThree,
        FourVFour
    }
    #endregion
    #region MatchParticipant
    [EntityMetadata(
        tableName: "match_participants",
        archiveTableName: "match_participant_archive",
        maxCacheSize: 1000,
        cacheExpiryMinutes: 30,
        servicePropertyName: "MatchParticipants"
    )]
    public class MatchParticipant : Entity, IMatchEntity
    {
        public Guid MatchId { get; set; }
        public Guid TeamId { get; set; }
        public bool IsWinner { get; set; }
        public int TeamNumber { get; set; } // 1 or 2
        public List<Guid> PlayerIds { get; set; } = new();
        public virtual ICollection<Player> Players { get; set; } = new List<Player>();
        public DateTime JoinedAt { get; set; }

        // Navigation properties
        public virtual Match Match { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;

        public override Domain Domain => Domain.Common;
    }
    #endregion

    #region TeamOpponentEncounter
    [EntityMetadata(
        tableName: "team_opponent_encounters",
        archiveTableName: "team_opponent_encounter_archive",
        maxCacheSize: 2000,
        cacheExpiryMinutes: 60,
        servicePropertyName: "TeamOpponentEncounters"
    )]
    public class TeamOpponentEncounter : Entity, IMatchEntity
    {
        public Guid TeamId { get; set; }
        public Guid OpponentId { get; set; }
        public Guid MatchId { get; set; }
        public int TeamSize { get; set; } // Using int instead of enum for DB compatibility
        public DateTime EncounteredAt { get; set; }
        public bool Won { get; set; }

        // Navigation properties
        public virtual Match Match { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
        public virtual Team Opponent { get; set; } = null!;

        public override Domain Domain => Domain.Common;
    }
    #endregion

    [EntityMetadata(
        tableName: "matches",
        archiveTableName: "match_archive",
        maxCacheSize: 200,
        cacheExpiryMinutes: 10,
        servicePropertyName: "Matches"
    )]
    public class Match : Entity, IMatchEntity
    {
        // Core match data (keep essential fields)
        public Guid Team1Id { get; set; }
        public Guid Team2Id { get; set; }
        public List<Guid> Team1PlayerIds { get; set; } = new();
        public List<Guid> Team2PlayerIds { get; set; } = new();
        public TeamSize TeamSize { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? WinnerId { get; set; }
        public Guid? ParentId { get; set; } // ID of parent Scrimmage or Tournament
        public string? ParentType { get; set; } // "Scrimmage" or "Tournament"
        public virtual ICollection<Game> Games { get; set; } = new List<Game>();
        public int BestOf { get; set; } = 1; // Number of games to win the match
        public bool PlayToCompletion { get; set; } // Whether all games must be played even after winner is determined

        // Navigation properties (following existing patterns)
        public virtual ICollection<MatchParticipant> Participants { get; set; } = new List<MatchParticipant>();
        public virtual ICollection<TeamOpponentEncounter> OpponentEncounters { get; set; } = new List<TeamOpponentEncounter>();

        // Map ban properties
        public List<string> AvailableMaps { get; set; } = new(); // Maps available for the match
        public List<string> Team1MapBans { get; set; } = new(); // Maps banned by team 1
        public List<string> Team2MapBans { get; set; } = new(); // Maps banned by team 2
        public DateTime? Team1MapBansSubmittedAt { get; set; }
        public DateTime? Team2MapBansSubmittedAt { get; set; }


        // Discord Thread Management
        public ulong? ChannelId { get; set; } // Discord channel ID where the match threads are created
        public ulong? Team1ThreadId { get; set; } // Discord thread ID for Team 1 (private thread)
        public ulong? Team2ThreadId { get; set; } // Discord thread ID for Team 2 (private thread)

        // State Management Properties - StateHistory stored as JSONB
        public ICollection<MatchStateSnapshot> StateHistory { get; set; } = new List<MatchStateSnapshot>();

        public override Domain Domain => Domain.Common;
    }

    #region # MatchStateSnapshot
    [EntityMetadata(
        tableName: "match_state_snapshots",
        archiveTableName: "match_state_snapshot_archive",
        maxCacheSize: 500,
        cacheExpiryMinutes: 10,
        servicePropertyName: "MatchStateSnapshots"
    )]
    public class MatchStateSnapshot : Entity, IMatchEntity
    {

        // Match state properties
        public Guid MatchId { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty; // Who triggered this state change
        public string PlayerName { get; set; } = string.Empty; // Player name of the user who triggered this state change
        public Dictionary<string, object> AdditionalData { get; set; } = new();

        // Match lifecycle properties
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
        public ICollection<Game> Games { get; set; } = new List<Game>();
        public Guid? CurrentMapId { get; set; }

        // Final match results
        public string? FinalScore { get; set; }
        public ICollection<Game> FinalGames { get; set; } = new List<Game>();

        // Map ban state properties
        public List<string> AvailableMaps { get; set; } = new();
        public List<string> Team1MapBans { get; set; } = new();
        public List<string> Team2MapBans { get; set; } = new();
        public bool Team1BansSubmitted { get; set; }
        public bool Team2BansSubmitted { get; set; }
        public bool Team1BansConfirmed { get; set; }
        public bool Team2BansConfirmed { get; set; }
        public List<string> FinalMapPool { get; set; } = new();

        // Parent navigation
        public virtual Match Match { get; set; } = null!;

        public override Domain Domain => Domain.Common;
    }
    #endregion

    public enum MatchStatus
    {
        Created,
        InProgress,
        Completed,
        Cancelled,
        Forfeited
    }

    public enum GameStatus
    {
        Pending,
        Created,
        InProgress,
        Completed,
        Cancelled,
        Forfeited,
    }

    #region Game
    [EntityMetadata(
        tableName: "games",
        archiveTableName: "game_archive",
        maxCacheSize: 200,
        cacheExpiryMinutes: 10,
        servicePropertyName: "Games"
    )]
    public class Game : Entity, IMatchEntity
    {
        public Guid MatchId { get; set; }
        public Guid MapId { get; set; }
        public TeamSize TeamSize { get; set; }
        public virtual ICollection<Guid> Team1Players { get; set; } = new List<Guid>();
        public virtual ICollection<Guid> Team2Players { get; set; } = new List<Guid>();
        public List<Guid> Team1PlayerIds { get; set; } = new();
        public List<Guid> Team2PlayerIds { get; set; } = new();
        public int GameNumber { get; set; } // Position in the match (1-based)
        public ICollection<GameStateSnapshot> StateHistory { get; set; } = new List<GameStateSnapshot>();

        // Navigation properties
        public virtual Match Match { get; set; } = null!;
        public virtual Map Map { get; set; } = null!;

        public override Domain Domain => Domain.Common;
    }
    #endregion

    #region GameStateSnapshot
    /// <summary>
    /// Comprehensive game state snapshot that captures all possible game states
    /// </summary>
    [EntityMetadata(
        tableName: "game_state_snapshots",
        archiveTableName: "game_state_snapshot_archive",
        maxCacheSize: 300,
        cacheExpiryMinutes: 5,
        servicePropertyName: "GameStateSnapshots"
    )]
    public class GameStateSnapshot : Entity, IMatchEntity
    {
        // Game state properties
        public Guid GameId { get; set; }
        public virtual Game Game { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public Guid PlayerId { get; set; } // Who triggered this state change
        public virtual Player Player { get; set; } = null!; // Player who triggered this state change
        public string PlayerName { get; set; } = string.Empty; // Player name of the user who triggered this state change
        public Dictionary<string, object> AdditionalData { get; set; } = new();

        // Game lifecycle properties
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? ForfeitedAt { get; set; }

        // Game status properties
        public Guid? WinnerId { get; set; }
        public Guid? CancelledByUserId { get; set; }
        public Guid? ForfeitedByUserId { get; set; }
        public Guid? ForfeitedTeamId { get; set; }
        public string? CancellationReason { get; set; }
        public string? ForfeitReason { get; set; }

        // Deck submission state properties
        public string? Team1DeckCode { get; set; }
        public string? Team2DeckCode { get; set; }
        public DateTime? Team1DeckSubmittedAt { get; set; }
        public DateTime? Team2DeckSubmittedAt { get; set; }
        public bool Team1DeckConfirmed { get; set; }
        public bool Team2DeckConfirmed { get; set; }
        public DateTime? Team1DeckConfirmedAt { get; set; }
        public DateTime? Team2DeckConfirmedAt { get; set; }

        // Game progression properties
        public Guid MatchId { get; set; }
        public Guid MapId { get; set; }
        public TeamSize TeamSize { get; set; }
        public List<Guid> Team1PlayerIds { get; set; } = new();
        public List<Guid> Team2PlayerIds { get; set; } = new();
        public int GameNumber { get; set; } = 1;

        public override Domain Domain => Domain.Common;
    }
    #endregion

}