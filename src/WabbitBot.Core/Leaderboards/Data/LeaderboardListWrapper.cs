using System.Collections.Generic;
using WabbitBot.Common.Models;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class LeaderboardListWrapper : BaseEntity
    {
        public List<Leaderboard> Leaderboards { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }
}
