using System.Security.Cryptography.X509Certificates;
using FluentValidation.Validators;
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
        // Navigation properties
        public Guid? ChallengeId { get; set; }
        public ScrimmageChallenge? ScrimmageChallenge { get; set; }
        public Guid ChallengerTeamId { get; set; } // Challenger
        public Guid OpponentTeamId { get; set; } // Opponent
        public List<Guid> ChallengerTeamPlayerIds { get; set; } = new(); // Selected team members
        public List<Guid> OpponentTeamPlayerIds { get; set; } = new(); // Selected team members
        public Player? IssuedByPlayer { get; set; } // Player who issued the challenge
        public Player? AcceptedByPlayer { get; set; } // Player who accepted the challenge
        public Match? Match { get; set; }
        public List<ScrimmageStateSnapshot> StateHistory { get; set; } = new();

        // Data properties
        public TeamSize TeamSize { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid WinnerId { get; set; }
        public double ChallengerTeamRating { get; set; }
        public double OpponentTeamRating { get; set; }
        public double ChallengerTeamRatingChange { get; set; }
        public double OpponentTeamRatingChange { get; set; }
        public double ChallengerTeamConfidence { get; set; } // Confidence at time of match
        public double OpponentTeamConfidence { get; set; } // Confidence at time of match
        public int ChallengerTeamScore { get; set; } // Score at completion
        public int OpponentTeamScore { get; set; } // Score at completion
        public int BestOf { get; set; } = 1;

        // Rating system
        public double ChallengerTeamVarietyBonusUsed { get; set; }
        public double OpponentTeamVarietyBonusUsed { get; set; }
        public double ChallengerTeamMultiplierUsed { get; set; }
        public double OpponentTeamMultiplierUsed { get; set; }

        // Gap scaling
        public Guid HigherRatedTeamId { get; set; }
        public double ChallengerTeamRatingRangeAtMatch { get; set; }
        public double OpponentTeamRatingRangeAtMatch { get; set; }
        public double ChallengerTeamGapScalingAppliedValue { get; set; }
        public double OpponentTeamGapScalingAppliedValue { get; set; }

        // Catch-up bonus
        public double ChallengerTeamCatchUpBonusUsed { get; set; }
        public double OpponentTeamCatchUpBonusUsed { get; set; }
        public double ChallengerTeamAdjustedRatingChange { get; set; }
        public double OpponentTeamAdjustedRatingChange { get; set; }

        // Proven Potential
        public bool ChallengerTeamProvenPotentialApplied { get; set; } = false;
        public bool OpponentTeamProvenPotentialApplied { get; set; } = false;
        public DateTime? ChallengerTeamProvenPotentialAppliedAt { get; set; }
        public DateTime? OpponentTeamProvenPotentialAppliedAt { get; set; }

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
        // Navigation properties
        public Guid? BatchId { get; set; } // Id for batch application of rating adjustments
        public Guid OriginalMatchId { get; set; }
        public Guid ChallengerId { get; set; }
        public Guid OpponentId { get; set; }

        // Data properties for closer parity with Python simulator's batching system
        public Guid NewPlayerId { get; set; }
        public Guid EstablishedPlayerId { get; set; }
        public Guid TriggerMatchId { get; set; }
        public Guid AppliedAtMatchId { get; set; }
        public Guid TrackingEndMatchId { get; set; }
        public int CrossedThresholds { get; set; }
        public double ClosureFraction { get; set; }
        public double ScalingApplied { get; set; }
        public double AdjustedNewChange { get; set; }
        public double AdjustedEstablishedChange { get; set; }

        // Data properties
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
        Accepted,
        Declined,
        InProgress,
        Completed,
        Cancelled,
        Forfeited,
    }

    [EntityMetadata(
        tableName: "scrimmage_challenges",
        archiveTableName: "scrimmage_challenge_archive",
        maxCacheSize: 500,
        cacheExpiryMinutes: 60,
        servicePropertyName: "ScrimmageChallenges",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class ScrimmageChallenge : Entity
    {
        // Navigation properties
        public Team? ChallengerTeam { get; set; }
        public Team? OpponentTeam { get; set; }
        public Guid ChallengerTeamId { get; set; }
        public Guid OpponentTeamId { get; set; }

        // Data properties
        public Player? IssuedByPlayer { get; set; }
        public Guid IssuedByPlayerId { get; set; }
        public Player? AcceptedByPlayer { get; set; }
        public Guid? AcceptedByPlayerId { get; set; }
        public Player[] ChallengerTeamPlayers { get; set; } = []; // Selected team members
        public Player[]? OpponentTeamPlayers { get; set; } // Selected team members
        public ScrimmageChallengeStatus? ChallengeStatus { get; set; }
        public TeamSize TeamSize { get; set; }
        public int BestOf { get; set; }
        public DateTime ChallengeExpiresAt { get; set; }
        public override Domain Domain => Domain.Scrimmage;
    }

    public enum ScrimmageChallengeStatus
    {
        Pending,
        Accepted,
        Declined,
    }
}
