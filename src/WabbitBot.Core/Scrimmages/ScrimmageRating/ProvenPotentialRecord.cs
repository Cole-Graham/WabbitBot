using WabbitBot.Common.Models;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    public class ProvenPotentialRecord : BaseEntity
    {
        public Guid OriginalMatchId { get; set; }
        public string Team1Id { get; set; } = string.Empty;
        public string Team2Id { get; set; } = string.Empty;
        public int Team1Rating { get; set; }
        public int Team2Rating { get; set; }
        public double Team1Confidence { get; set; }
        public double Team2Confidence { get; set; }
        public HashSet<double> AppliedThresholds { get; set; } = new();
        public int RatingAdjustment { get; set; }
        public DateTime? LastCheckedAt { get; set; }
        public bool IsComplete { get; set; }
    }
}