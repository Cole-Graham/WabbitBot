using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Models.Scrimmage
{
    [EntityMetadata(
        tableName: "scrimmages",
        archiveTableName: "scrimmage_archive",
        maxCacheSize: 200,
        cacheExpiryMinutes: 20,
        servicePropertyName: "Scrimmages",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public partial class Scrimmage : Entity, IScrimmageEntity
    {
        public Guid Team1Id { get; set; }
        public Guid Team2Id { get; set; }
        public List<Guid> Team1RosterIds { get; set; } = new();
        public List<Guid> Team2RosterIds { get; set; } = new();
        public TeamSize TeamSize { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? WinnerId { get; set; }
        public double Team1Rating { get; set; }
        public double Team2Rating { get; set; }
        public double Team1RatingChange { get; set; }
        public double Team2RatingChange { get; set; }
        public double Team1Confidence { get; set; } = 0.0; // Confidence at time of match
        public double Team2Confidence { get; set; } = 0.0; // Confidence at time of match
        public int Team1Score { get; set; } = 0; // Score at completion
        public int Team2Score { get; set; } = 0; // Score at completion
        public DateTime? ChallengeExpiresAt { get; set; }
        public bool IsAccepted { get; set; }
        public Match? Match { get; private set; }
        public int BestOf { get; set; } = 1;
        public List<ScrimmageStateSnapshot> StateHistory { get; set; } = new();

        public override Domain Domain => Domain.Scrimmage;

    }

    [EntityMetadata(
        tableName: "proven_potential_records",
        archiveTableName: "proven_potential_record_archive",
        maxCacheSize: 500,
        cacheExpiryMinutes: 60,
        servicePropertyName: "ProvenPotentialRecords",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class ProvenPotentialRecord : Entity, IScrimmageEntity
    {
        public Guid OriginalMatchId { get; set; }
        public Guid ChallengerId { get; set; }
        public Guid OpponentId { get; set; }
        public double ChallengerRating { get; set; }
        public double OpponentRating { get; set; }
        public double ChallengerConfidence { get; set; }
        public double OpponentConfidence { get; set; }
        public HashSet<double> AppliedThresholds { get; set; } = new();
        public double ChallengerOriginalRatingChange { get; set; } // Store the original rating change for the challenger
        public double OpponentOriginalRatingChange { get; set; } // Store the original rating change for the opponent
        public double RatingAdjustment { get; set; }
        public TeamSize TeamSize { get; set; } // Store the game size from the original match
        public DateTime? LastCheckedAt { get; set; }
        public bool IsComplete { get; set; }
        public override Domain Domain => Domain.Scrimmage;
    }

    [EntityMetadata(
        tableName: "scrimmage_state_snapshots",
        archiveTableName: "scrimmage_state_snapshot_archive",
        maxCacheSize: 300,
        cacheExpiryMinutes: 15,
        servicePropertyName: "ScrimmageStateSnapshots",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class ScrimmageStateSnapshot : Entity, IScrimmageEntity
    {
        // TODO: Implement/Design ScrimmageStateSnapshot
        public Guid ScrimmageId { get; set; } // Navigational property
        public Scrimmage? Scrimmage { get; set; } // Navigational property
        public ScrimmageStatus Status { get; set; }
        public override Domain Domain => Domain.Scrimmage;
    }

    public enum ScrimmageStatus
    {
        Created,
        Accepted,
        Declined,
        InProgress,
        Completed,
        Cancelled,
        Forfeited
    }
}
