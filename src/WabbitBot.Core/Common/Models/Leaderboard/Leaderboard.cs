using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    public class Leaderboard : Entity
    {
        public Dictionary<EvenTeamFormat, Dictionary<string, LeaderboardItem>> Rankings { get; set; } = new();
        public const double InitialRating = 1000.0; // Starting rating changed to 1000
        public const double KFactor = 40.0; // ELO rating system constant

        public Leaderboard()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            foreach (EvenTeamFormat size in Enum.GetValues(typeof(EvenTeamFormat)))
            {
                Rankings[size] = new Dictionary<string, LeaderboardItem>();
            }
        }
    }

    public class LeaderboardItem : Entity
    {
        public List<Guid> PlayerIds { get; set; } = new();
        public Guid TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double Rating { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsTeam { get; set; }
        public double WinRate => Wins + Losses == 0 ? 0 : (double)Wins / (Wins + Losses);
    }
}
