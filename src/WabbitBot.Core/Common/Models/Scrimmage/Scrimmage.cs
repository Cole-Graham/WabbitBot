using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    public partial class Scrimmage : Entity
    {
        public Guid Team1Id { get; set; }
        public Guid Team2Id { get; set; }
        public List<Guid> Team1RosterIds { get; set; } = new();
        public List<Guid> Team2RosterIds { get; set; } = new();
        public EvenTeamFormat EvenTeamFormat { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? WinnerId { get; set; }
        public ScrimmageStatus Status { get; set; }
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

        public Scrimmage()
        {
            CreatedAt = DateTime.UtcNow;
            Status = ScrimmageStatus.Created;
            ChallengeExpiresAt = DateTime.UtcNow.AddHours(24); // 24 hour challenge window
        }

        public bool IsTeamMatch => EvenTeamFormat != EvenTeamFormat.OneVOne;
    }

    #region # ScrimmageStatus
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
    #endregion

    #region # ProvenPotentialRecord
    public class ProvenPotentialRecord : Entity
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
        public EvenTeamFormat EvenTeamFormat { get; set; } // Store the game size from the original match
        public DateTime? LastCheckedAt { get; set; }
        public bool IsComplete { get; set; }
    }
    #endregion
}