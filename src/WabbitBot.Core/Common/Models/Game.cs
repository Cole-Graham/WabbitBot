using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    public enum GameMode
    {
        EvenTeams, FreeForAll
    }

    public enum EvenTeamFormat
    {
        OneVOne,
        TwoVTwo,
        ThreeVThree,
        FourVFour
    }

    public readonly record struct PlayerCount(int TotalPlayers)
    {
        public static implicit operator PlayerCount(EvenTeamFormat format) => format switch
        {
            EvenTeamFormat.OneVOne => new(2),
            EvenTeamFormat.TwoVTwo => new(4),
            EvenTeamFormat.ThreeVThree => new(6),
            EvenTeamFormat.FourVFour => new(8),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        public static explicit operator int(PlayerCount count) => count.TotalPlayers;

        public static PlayerCount FromInt(int totalPlayers)
        {
            if (totalPlayers < 2)
                throw new ArgumentOutOfRangeException(nameof(totalPlayers), "Must be at least 2 players.");
            return new(totalPlayers);
        }

        public override string ToString() => $"{TotalPlayers}-Player";
    }

    public record GameFormat(GameMode Mode, EvenTeamFormat? EvenTeamFormat = null, PlayerCount? CustomCount = null)
    {
        // ✅ No explicit constructor = no conflicts
        // Validation happens in factory methods below

        public PlayerCount TotalPlayers => Mode switch
        {
            GameMode.EvenTeams when EvenTeamFormat.HasValue => (PlayerCount)EvenTeamFormat.Value,
            GameMode.FreeForAll when CustomCount.HasValue => CustomCount.Value,
            _ => throw new InvalidOperationException("Invalid GameFormat configuration")
        };

        // Factory methods ensure valid combinations
        public static GameFormat EvenTeams(EvenTeamFormat format)
        {
            return new(GameMode.EvenTeams, EvenTeamFormat: format, CustomCount: null);
        }

        public static GameFormat FreeForAll(int playerCount)
        {
            var count = PlayerCount.FromInt(playerCount);
            return new(GameMode.FreeForAll, EvenTeamFormat: null, CustomCount: count);
        }
    }

    public class Game : Entity
    {
        public Guid MatchId { get; set; }
        public Guid MapId { get; set; }
        public EvenTeamFormat EvenTeamFormat { get; set; }
        public List<Guid> Team1PlayerIds { get; set; } = new();
        public List<Guid> Team2PlayerIds { get; set; } = new();
        public int GameNumber { get; set; } // Position in the match (1-based)

        // State management - StateHistory stored as JSONB, CurrentState is computed
        public List<GameStateSnapshot> StateHistory { get; set; } = new();
        public GameStateSnapshot CurrentState => StateHistory.LastOrDefault() ?? new GameStateSnapshot();

        public Game()
        {
            CreatedAt = DateTime.UtcNow;
            InitializeState();
        }

        /// <summary>
        /// Initializes the game state
        /// </summary>
        private void InitializeState()
        {
            var initialState = new GameStateSnapshot
            {
                GameId = Id,
                MatchId = MatchId,
                MapId = MapId,
                EvenTeamFormat = EvenTeamFormat,
                Team1PlayerIds = Team1PlayerIds,
                Team2PlayerIds = Team2PlayerIds,
                GameNumber = GameNumber,
                Timestamp = DateTime.UtcNow
            };
            StateHistory.Add(initialState);
        }

        /// <summary>
        /// Gets the current game status from the state snapshot
        /// </summary>
        public GameStatus Status => CurrentState.GetCurrentState();

        /// <summary>
        /// Gets the winner ID from the state snapshot
        /// </summary>
        public Guid? WinnerId => CurrentState.WinnerId;

        /// <summary>
        /// Gets the started time from the state snapshot
        /// </summary>
        public DateTime StartedAt => CurrentState.StartedAt ?? CreatedAt;

        /// <summary>
        /// Gets the completed time from the state snapshot
        /// </summary>
        public DateTime? CompletedAt => CurrentState.CompletedAt;

        /// <summary>
        /// Gets deck submission properties from the state snapshot
        /// </summary>
        public string? Team1DeckCode => CurrentState.Team1DeckCode;
        public string? Team2DeckCode => CurrentState.Team2DeckCode;
        public DateTime? Team1DeckSubmittedAt => CurrentState.Team1DeckSubmittedAt;
        public DateTime? Team2DeckSubmittedAt => CurrentState.Team2DeckSubmittedAt;

        /// <summary>
        /// Validation methods for Game model
        /// </summary>
        public static class Validation
        {
            /// <summary>
            /// Attempts to parse a string into a EvenTeamFormat enum value
            /// </summary>
            public static bool TryParseEvenTeamFormat(string size, out EvenTeamFormat evenTeamFormat)
            {
                evenTeamFormat = size.ToLowerInvariant() switch
                {
                    "1v1" => EvenTeamFormat.OneVOne,
                    "2v2" => EvenTeamFormat.TwoVTwo,
                    "3v3" => EvenTeamFormat.ThreeVThree,
                    "4v4" => EvenTeamFormat.FourVFour,
                    _ => EvenTeamFormat.OneVOne
                };

                return size.ToLowerInvariant() is "1v1" or "2v2" or "3v3" or "4v4";
            }

            /// <summary>
            /// Validates if a game size string is valid
            /// </summary>
            public static bool IsValidEvenTeamFormat(string size)
            {
                return size.ToLowerInvariant() is "1v1" or "2v2" or "3v3" or "4v4";
            }

            /// <summary>
            /// Validates if a game size enum is valid
            /// </summary>
            public static bool IsValidEvenTeamFormat(EvenTeamFormat evenTeamFormat)
            {
                return Enum.IsDefined(typeof(EvenTeamFormat), evenTeamFormat);
            }
        }

        /// <summary>
        /// Helper methods for Game model
        /// </summary>
        public static class Helpers
        {
            /// <summary>
            /// Converts a EvenTeamFormat enum value to its display string
            /// </summary>
            public static string GetEvenTeamFormatDisplay(EvenTeamFormat evenTeamFormat)
            {
                return evenTeamFormat switch
                {
                    EvenTeamFormat.OneVOne => "1v1",
                    EvenTeamFormat.TwoVTwo => "2v2",
                    EvenTeamFormat.ThreeVThree => "3v3",
                    EvenTeamFormat.FourVFour => "4v4",
                    _ => evenTeamFormat.ToString()
                };
            }
        }
    }

    #region GameStatus
    public enum GameStatus
    {
        Pending,
        Created,
        InProgress,
        Completed,
        Cancelled,
        Forfeited
    }
    #endregion

    #region GameStateSnapshot
    /// <summary>
    /// Comprehensive game state snapshot that captures all possible game states
    /// </summary>
    public class GameStateSnapshot : Entity
    {
        // Game state properties
        public Guid GameId { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid UserId { get; set; } // Who triggered this state change
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
        public EvenTeamFormat EvenTeamFormat { get; set; }
        public List<Guid> Team1PlayerIds { get; set; } = new();
        public List<Guid> Team2PlayerIds { get; set; } = new();
        public int GameNumber { get; set; } = 1;

        /// <summary>
        /// Constructor to initialize Entity properties
        /// </summary>
        public GameStateSnapshot()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Copy constructor for creating a new snapshot based on an existing one
        /// </summary>
        public GameStateSnapshot(GameStateSnapshot other)
        {
            // Copy all properties from the other snapshot
            GameId = other.GameId;
            Timestamp = DateTime.UtcNow; // Always use current time for new snapshot
            UserId = other.UserId;
            PlayerName = other.PlayerName;
            AdditionalData = new Dictionary<string, object>(other.AdditionalData);

            // Copy lifecycle properties
            StartedAt = other.StartedAt;
            CompletedAt = other.CompletedAt;
            CancelledAt = other.CancelledAt;
            ForfeitedAt = other.ForfeitedAt;

            // Copy status properties
            WinnerId = other.WinnerId;
            CancelledByUserId = other.CancelledByUserId;
            ForfeitedByUserId = other.ForfeitedByUserId;
            ForfeitedTeamId = other.ForfeitedTeamId;
            CancellationReason = other.CancellationReason;
            ForfeitReason = other.ForfeitReason;

            // Copy deck code properties
            Team1DeckCode = other.Team1DeckCode;
            Team2DeckCode = other.Team2DeckCode;
            Team1DeckSubmittedAt = other.Team1DeckSubmittedAt;
            Team2DeckSubmittedAt = other.Team2DeckSubmittedAt;
            Team1DeckConfirmed = other.Team1DeckConfirmed;
            Team2DeckConfirmed = other.Team2DeckConfirmed;
            Team1DeckConfirmedAt = other.Team1DeckConfirmedAt;
            Team2DeckConfirmedAt = other.Team2DeckConfirmedAt;

            // Copy game properties
            MatchId = other.MatchId;
            MapId = other.MapId;
            EvenTeamFormat = other.EvenTeamFormat;
            Team1PlayerIds = other.Team1PlayerIds;
            Team2PlayerIds = other.Team2PlayerIds;
            GameNumber = other.GameNumber;
        }

        /// <summary>
        /// Determines the current game state based on property values
        /// </summary>
        public GameStatus GetCurrentState()
        {
            if (CompletedAt.HasValue)
                return GameStatus.Completed;

            if (CancelledAt.HasValue)
                return GameStatus.Cancelled;

            if (ForfeitedAt.HasValue)
                return GameStatus.Forfeited;

            if (StartedAt.HasValue)
                return GameStatus.InProgress;

            return GameStatus.Created;
        }

        /// <summary>
        /// Determines what action is needed based on current state
        /// </summary>
        public string GetRequiredAction()
        {
            var state = GetCurrentState();

            return state switch
            {
                GameStatus.Created => "Waiting for deck submissions",
                GameStatus.InProgress => "Game in progress",
                GameStatus.Completed => "Game completed",
                GameStatus.Cancelled => "Game cancelled",
                GameStatus.Forfeited => "Game forfeited",
                _ => "Unknown state"
            };
        }

        /// <summary>
        /// Checks if both teams have submitted their deck codes
        /// </summary>
        public bool AreDeckCodesSubmitted()
        {
            return !string.IsNullOrEmpty(Team1DeckCode) && !string.IsNullOrEmpty(Team2DeckCode);
        }

        /// <summary>
        /// Checks if both teams have confirmed their deck codes
        /// </summary>
        public bool AreDeckCodesConfirmed()
        {
            return Team1DeckConfirmed && Team2DeckConfirmed;
        }

        /// <summary>
        /// Checks if the game is ready to start (all deck codes submitted and confirmed)
        /// </summary>
        public bool IsReadyToStart()
        {
            return AreDeckCodesSubmitted() && AreDeckCodesConfirmed();
        }
    }
    #endregion

    #region GameStateMachine
    /// <summary>
    /// State machine for managing game state transitions
    /// </summary>
    public class GameStateMachine
    {
        private readonly Dictionary<GameStatus, List<GameStatus>> _validTransitions;

        public GameStateMachine()
        {
            _validTransitions = new Dictionary<GameStatus, List<GameStatus>>
            {
                [GameStatus.Created] = new() { GameStatus.InProgress, GameStatus.Cancelled },
                [GameStatus.InProgress] = new() { GameStatus.Completed, GameStatus.Cancelled, GameStatus.Forfeited },
                [GameStatus.Completed] = new(), // Terminal state
                [GameStatus.Cancelled] = new(), // Terminal state
                [GameStatus.Forfeited] = new()  // Terminal state
            };
        }

        /// <summary>
        /// Validates if a state transition is allowed
        /// </summary>
        public bool CanTransition(GameStatus from, GameStatus to)
        {
            return _validTransitions.ContainsKey(from) &&
                _validTransitions[from].Contains(to);
        }

        /// <summary>
        /// Gets all valid transitions from a given state
        /// </summary>
        public List<GameStatus> GetValidTransitions(GameStatus from)
        {
            return _validTransitions.ContainsKey(from)
                ? new List<GameStatus>(_validTransitions[from])
                : new List<GameStatus>();
        }

        /// <summary>
        /// Transitions the game state if valid
        /// </summary>
        public bool TryTransition(GameStateSnapshot snapshot, GameStatus toState, Guid userId, string playerName, string? reason = null)
        {
            var currentState = snapshot.GetCurrentState();

            if (!CanTransition(currentState, toState))
                return false;

            // Update state-specific properties
            switch (toState)
            {
                case GameStatus.InProgress:
                    snapshot.StartedAt = DateTime.UtcNow;
                    break;
                case GameStatus.Completed:
                    snapshot.CompletedAt = DateTime.UtcNow;
                    break;
                case GameStatus.Cancelled:
                    snapshot.CancelledAt = DateTime.UtcNow;
                    snapshot.CancelledByUserId = userId;
                    snapshot.CancellationReason = reason;
                    break;
                case GameStatus.Forfeited:
                    snapshot.ForfeitedAt = DateTime.UtcNow;
                    snapshot.ForfeitedByUserId = userId;
                    snapshot.ForfeitReason = reason;
                    break;
            }

            // Update common properties
            snapshot.UserId = userId;
            snapshot.PlayerName = playerName;
            snapshot.Timestamp = DateTime.UtcNow;

            return true;
        }
    }
    #endregion
}
