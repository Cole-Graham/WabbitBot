using System;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    public class Tournament : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public EvenTeamFormat EvenTeamFormat { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public int BestOf { get; set; } = 1; // Number of games to win each match

        // State Management Properties - StateHistory stored as JSONB, CurrentStateSnapshot is computed
        public List<TournamentStateSnapshot> StateHistory { get; set; } = new();
        public TournamentStateSnapshot CurrentStateSnapshot => StateHistory.LastOrDefault() ?? new TournamentStateSnapshot();
        
        public Tournament()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Tournament Clone()
        {
            return new Tournament
            {
                Id = Id,
                Name = Name,
                Description = Description,
                EvenTeamFormat = EvenTeamFormat,
                StartDate = StartDate,
                EndDate = EndDate,
                MaxParticipants = MaxParticipants,
                BestOf = BestOf,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                StateHistory = StateHistory
            };
        }
    }

    #region # TournamentStateSnapshot
    /// <summary>
    /// Comprehensive tournament state snapshot that captures all possible tournament states
    /// </summary>
    public class TournamentStateSnapshot : Entity
    {

        // Tournament state properties
        public Guid TournamentId { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid UserId { get; set; } // Who triggered this state change
        public string PlayerName { get; set; } = string.Empty; // Player name of the user who triggered this state change
        public Dictionary<string, object> AdditionalData { get; set; } = new();

        // Tournament lifecycle properties
        public DateTime? RegistrationOpenedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Tournament configuration properties
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public int MaxParticipants { get; set; }

        // Tournament status properties
        public Guid? WinnerTeamId { get; set; }
        public Guid? CancelledByUserId { get; set; }
        public string? CancellationReason { get; set; }

        // Tournament progression properties
        public List<Guid> RegisteredTeamIds { get; set; } = new();
        public List<Guid> ParticipantTeamIds { get; set; } = new();
        public List<Guid> ActiveMatchIds { get; set; } = new();
        public List<Guid> CompletedMatchIds { get; set; } = new();
        public List<Guid> AllMatchIds { get; set; } = new();
        public List<string> FinalRankings { get; set; } = new(); // Team IDs in order of placement
        public int CurrentParticipantCount { get; set; }
        public int CurrentRound { get; set; } = 1;

        /// <summary>
        /// Constructor to initialize Entity properties
        /// </summary>
        public TournamentStateSnapshot()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
    #endregion
}