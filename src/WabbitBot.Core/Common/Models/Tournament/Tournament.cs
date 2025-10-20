using System;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Models.Tournament
{
    [EntityMetadata(
        tableName: "tournaments",
        archiveTableName: "tournament_archive",
        maxCacheSize: 50,
        cacheExpiryMinutes: 30,
        servicePropertyName: "Tournaments",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public partial class Tournament : Entity, ITournamentEntity
    {
        public string Name { get; set; } = string.Empty;
        public TournamentStatus Status { get; set; } = TournamentStatus.Announced;
        public string Description { get; set; } = string.Empty;
        public TeamSize TeamSize { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int MaxParticipants { get; set; }
        public int BestOf { get; set; } = 1; // Number of games to win each match

        // Foreign key collection for state history
        public ICollection<Guid> StateHistoryIds { get; set; } = [];

        // State Management Properties - StateHistory stored as JSONB
        public virtual ICollection<TournamentStateSnapshot> StateHistory { get; set; } =
            new List<TournamentStateSnapshot>();

        public override Domain Domain => Domain.Tournament;
    }

    public enum TournamentStatus
    {
        Announced,
        RegistrationOpen,
        RegistrationClosed,
        InProgress,
        Completed,
        Cancelled,
    }

    #region # TournamentStateSnapshot
    /// <summary>
    /// Comprehensive tournament state snapshot that captures all possible tournament states
    /// </summary>
    [EntityMetadata(
        tableName: "tournament_state_snapshots",
        archiveTableName: "tournament_state_snapshot_archive",
        maxCacheSize: 200,
        cacheExpiryMinutes: 15,
        servicePropertyName: "TournamentStateSnapshots",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class TournamentStateSnapshot : Entity, ITournamentEntity
    {
        // Tournament state properties
        public Guid TournamentId { get; set; }
        public DateTime Timestamp { get; set; }
        public Guid TriggeredByMashinaUserId { get; set; } = Guid.Empty; // User who triggered this state change
        public virtual MashinaUser? TriggeredByMashinaUser { get; set; } = null;
        public Dictionary<string, object> AdditionalData { get; set; } = [];

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
        public Guid? CancelledByMashinaUserId { get; set; }
        public virtual MashinaUser? CancelledByMashinaUser { get; set; } = null;
        public string? CancellationReason { get; set; }

        // Tournament progression properties
        public List<Guid> RegisteredTeamIds { get; set; } = [];
        public List<Guid> ParticipantTeamIds { get; set; } = [];
        public List<Guid> ActiveMatchIds { get; set; } = [];
        public List<Guid> CompletedMatchIds { get; set; } = [];
        public List<Guid> AllMatchIds { get; set; } = [];
        public List<Guid> FinalRankings { get; set; } = []; // Team IDs in order of placement
        public int CurrentParticipantCount { get; set; }
        public int CurrentRound { get; set; } = 1;

        // Parent navigation
        public virtual Tournament? Tournament { get; set; } = null;

        public override Domain Domain => Domain.Tournament;
    }
    #endregion
}
