using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    public class ProvenPotentialRecord : BaseEntity
    {
        public Guid OriginalMatchId { get; set; }
        public string ChallengerId { get; set; } = string.Empty;
        public string OpponentId { get; set; } = string.Empty;
        public int ChallengerRating { get; set; }
        public int OpponentRating { get; set; }
        public double ChallengerConfidence { get; set; }
        public double OpponentConfidence { get; set; }
        public HashSet<double> AppliedThresholds { get; set; } = new();
        public double ChallengerOriginalRatingChange { get; set; } // Store the original rating change for the challenger
        public double OpponentOriginalRatingChange { get; set; } // Store the original rating change for the opponent
        public double RatingAdjustment { get; set; }
        public GameSize GameSize { get; set; } // Store the game size from the original match
        public DateTime? LastCheckedAt { get; set; }
        public bool IsComplete { get; set; }
    }
}