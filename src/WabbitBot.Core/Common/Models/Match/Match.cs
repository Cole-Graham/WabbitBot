
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    public partial class Match : Entity
    {
        public Guid Team1Id { get; set; }
        public Guid Team2Id { get; set; }
        public List<Guid> Team1PlayerIds { get; set; } = new();
        public List<Guid> Team2PlayerIds { get; set; } = new();
        public EvenTeamFormat EvenTeamFormat { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? WinnerId { get; set; }
        public Guid? ParentId { get; set; } // ID of parent Scrimmage or Tournament
        public string? ParentType { get; set; } // "Scrimmage" or "Tournament"
        public List<Game> Games { get; set; } = new();
        public int BestOf { get; set; } = 1; // Number of games to win the match
        public bool PlayToCompletion { get; set; } // Whether all games must be played even after winner is determined

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

        // State Management Properties - StateHistory stored as JSONB, CurrentStateSnapshot is computed
        public List<MatchStateSnapshot> StateHistory { get; set; } = new();
        public MatchStateSnapshot CurrentStateSnapshot => StateHistory.LastOrDefault() ?? new MatchStateSnapshot();



        public Match()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public bool IsTeamMatch => EvenTeamFormat != EvenTeamFormat.OneVOne;

        /// <summary>
        /// Gets the current match state
        /// </summary>
        public MatchState CurrentState => CurrentStateSnapshot?.GetCurrentState() ?? MatchState.Created;
        
    }

    #region # MatchStateSnapshot
    public class MatchStateSnapshot : Entity
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
        public List<Game> Games { get; set; } = new();
        public Guid? CurrentMapId { get; set; }

        // Final match results
        public string? FinalScore { get; set; }
        public List<Game> FinalGames { get; set; } = new();

        // Map ban state properties
        public List<string> AvailableMaps { get; set; } = new();
        public List<string> Team1MapBans { get; set; } = new();
        public List<string> Team2MapBans { get; set; } = new();
        public bool Team1BansSubmitted { get; set; }
        public bool Team2BansSubmitted { get; set; }
        public bool Team1BansConfirmed { get; set; }
        public bool Team2BansConfirmed { get; set; }
        public List<string> FinalMapPool { get; set; } = new();

        /// <summary>
        /// Constructor to initialize Entity properties
        /// </summary>
        public MatchStateSnapshot()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Determines the current match state based on property values
        /// </summary>
        public MatchState GetCurrentState()
        {
            if (CompletedAt.HasValue)
                return MatchState.Completed;

            if (CancelledAt.HasValue)
                return MatchState.Cancelled;

            if (ForfeitedAt.HasValue)
                return MatchState.Forfeited;

            if (StartedAt.HasValue)
                return MatchState.InProgress;

            return MatchState.Created;
        }

    }
    #endregion

    #region # MatchState
    /// <summary>
    /// Enum for match states
    /// </summary>
    public enum MatchState
    {
        Created,
        InProgress,
        Completed,
        Cancelled,
        Forfeited
    }
    #endregion

}