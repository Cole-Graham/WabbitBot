using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;
using FluentValidation.Validators;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
        public required Guid ScrimmageChallengeId { get; set; }
        public virtual ScrimmageChallenge ScrimmageChallenge { get; set; } = null!;
        public required Guid ChallengerTeamId { get; set; } // Challenger
        public virtual Team ChallengerTeam { get; set; } = null!;
        public required Guid OpponentTeamId { get; set; } // Opponent
        public virtual Team OpponentTeam { get; set; } = null!;

        // Navigation properties for selected team members (full lists)
        public virtual ICollection<Player> ChallengerTeamPlayers { get; set; } = [];
        public virtual ICollection<Player> OpponentTeamPlayers { get; set; } = [];
        public required Guid IssuedByPlayerId { get; set; } // Player who issued the challenge
        public required Guid AcceptedByPlayerId { get; set; } // Player who accepted the challenge
        public virtual Player IssuedByPlayer { get; set; } = null!; // Player who issued the challenge
        public virtual Player AcceptedByPlayer { get; set; } = null!; // Player who accepted the challenge
        public Guid? MatchId { get; set; }
        public virtual Match? Match { get; set; }
        public virtual ICollection<ScrimmageStateSnapshot> StateHistory { get; set; } = [];

        // Data properties
        public required TeamSize TeamSize { get; set; }
        public required int BestOf { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? WinnerId { get; set; }
        public required double ChallengerTeamRating { get; set; }
        public required double OpponentTeamRating { get; set; }
        public double? ChallengerTeamRatingChange { get; set; }
        public double? OpponentTeamRatingChange { get; set; }
        public required double ChallengerTeamConfidence { get; set; } // Confidence at time of match
        public required double OpponentTeamConfidence { get; set; } // Confidence at time of match
        public int? ChallengerTeamScore { get; set; } // Score at completion
        public int? OpponentTeamScore { get; set; } // Score at completion

        // Rating system
        public double? ChallengerTeamVarietyBonusUsed { get; set; }
        public double? OpponentTeamVarietyBonusUsed { get; set; }
        public double? ChallengerTeamMultiplierUsed { get; set; }
        public double? OpponentTeamMultiplierUsed { get; set; }

        // Gap scaling
        public required Guid HigherRatedTeamId { get; set; }
        public required double RatingRangeAtMatch { get; set; }
        public double? ChallengerTeamGapScalingAppliedValue { get; set; }
        public double? OpponentTeamGapScalingAppliedValue { get; set; }

        // Catch-up bonus
        public double? ChallengerTeamCatchUpBonusUsed { get; set; }
        public double? OpponentTeamCatchUpBonusUsed { get; set; }
        public double? ChallengerTeamAdjustedRatingChange { get; set; }
        public double? OpponentTeamAdjustedRatingChange { get; set; }

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
        public required Guid OriginalMatchId { get; set; }
        public required Guid ChallengerId { get; set; }
        public required Guid OpponentId { get; set; }

        // Data properties for closer parity with Python simulator's batching system
        public required Guid NewPlayerId { get; set; }
        public required Guid EstablishedPlayerId { get; set; }
        public required Guid TriggerMatchId { get; set; }
        public Guid? AppliedAtMatchId { get; set; } // The match when the new player reached 1.0 confidence and PP batch was applied
        public Guid? TrackingEndMatchId { get; set; } // The match when 16-match tracking window closed for the new player
        public required int NewPlayerMatchCountAtCreation { get; set; } // New player's total matches when PP record was created
        public int CrossedThresholds { get; set; } = 0;
        public double? ClosureFraction { get; set; }
        public double? ScalingApplied { get; set; } // The threshold scaling applied to both players' rating changes
        public double? AdjustedNewChange { get; set; } // The adjusted rating change for the new player after PP application
        public double? AdjustedEstablishedChange { get; set; } // The adjusted rating change for the established player after PP application

        // Data properties
        public required double ChallengerRating { get; set; }
        public required double OpponentRating { get; set; }
        public required double ChallengerConfidence { get; set; }
        public required double OpponentConfidence { get; set; }
        public HashSet<double> AppliedThresholds { get; set; } = new();
        public required double ChallengerOriginalRatingChange { get; set; } // Store the original rating change for the challenger
        public required double OpponentOriginalRatingChange { get; set; } // Store the original rating change for the opponent
        public double RatingAdjustment { get; set; } // TODO: Find out what this is meant to track
        public required TeamSize TeamSize { get; set; } // Store the game size from the original match
        public DateTime? LastCheckedAt { get; set; }
        public bool IsComplete { get; set; } = false;

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
        public required Guid ScrimmageId { get; set; } // Navigational property
        public virtual Scrimmage Scrimmage { get; set; } = null!; // Navigational property
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
        // Navigation properties (for EF Core relationships only)
        public virtual Team ChallengerTeam { get; set; } = null!;
        public virtual Team OpponentTeam { get; set; } = null!;
        public required Guid ChallengerTeamId { get; set; }
        public required Guid OpponentTeamId { get; set; }

        // Data properties
        public virtual Player IssuedByPlayer { get; set; } = null!;
        public required Guid IssuedByPlayerId { get; set; }
        public virtual Player? AcceptedByPlayer { get; set; }
        public Guid? AcceptedByPlayerId { get; set; }
        public required ICollection<Guid> ChallengerTeammateIds { get; set; } = [];
        public ICollection<Guid>? OpponentTeammateIds { get; set; }

        public required ScrimmageChallengeStatus ChallengeStatus { get; set; }
        public required TeamSize TeamSize { get; set; }
        public required int BestOf { get; set; }
        public required DateTime ChallengeExpiresAt { get; set; }
        public ulong? ChallengeMessageId { get; set; } // Discord message ID for the challenge container
        public ulong? ChallengeChannelId { get; set; } // Discord channel ID where challenge was posted
        public override Domain Domain => Domain.Scrimmage;
    }

    public enum ScrimmageChallengeStatus
    {
        Pending,
        Accepted,
        Declined,
        Cancelled,
    }
}
