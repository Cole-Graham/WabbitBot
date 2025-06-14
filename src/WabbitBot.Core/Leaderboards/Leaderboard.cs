using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Leaderboards
{
    public class LeaderboardEntry
    {
        public string Name { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Rating { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsTeam { get; set; }
        public double WinRate => Wins + Losses == 0 ? 0 : (double)Wins / (Wins + Losses);
    }

    public class Leaderboard : BaseEntity
    {
        public Dictionary<GameSize, Dictionary<string, LeaderboardEntry>> Rankings { get; set; } = new();
        public const int InitialRating = 1500;
        public const int KFactor = 32; // ELO rating system constant

        public Leaderboard()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            foreach (GameSize size in Enum.GetValues(typeof(GameSize)))
            {
                Rankings[size] = new Dictionary<string, LeaderboardEntry>();
            }
        }
    }
}
