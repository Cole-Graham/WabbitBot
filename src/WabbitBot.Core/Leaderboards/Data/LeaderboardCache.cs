using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards.Data.Interface;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class LeaderboardCache : Cache<Leaderboard>, ILeaderboardCache
    {
        private const string KeyPrefix = "leaderboard:";
        private const string ListKey = "leaderboards:active";
        private const int MaxLeaderboardCacheSize = 500; // Reasonable limit for leaderboards

        public LeaderboardCache() : base(MaxLeaderboardCacheSize)
        {
        }

        public async Task<Leaderboard?> GetLeaderboardAsync(Guid id)
        {
            return await GetAsync($"{KeyPrefix}{id}");
        }

        public async Task SetLeaderboardAsync(Leaderboard leaderboard, TimeSpan? expiry = null)
        {
            await SetAsync($"{KeyPrefix}{leaderboard.Id}", leaderboard, expiry);
        }

        public async Task<bool> RemoveLeaderboardAsync(Guid leaderboardId)
        {
            return await RemoveAsync($"{KeyPrefix}{leaderboardId}");
        }

        public async Task<bool> LeaderboardExistsAsync(Guid leaderboardId)
        {
            return await ExistsAsync($"{KeyPrefix}{leaderboardId}");
        }

        // TODO: Methods that used LeaderboardListWrapper have been removed
        // Functionality moved to CoreService per refactor plan step 5.5

        // TODO: Business logic methods moved to CoreService per refactor plan step 5.5
        // These methods should be implemented in CoreService.Leaderboard.cs

        public async Task<IEnumerable<LeaderboardItem>> GetTopRankingsAsync(EvenTeamFormat evenTeamFormat, int count = 10)
        {
            // TODO: Implement in CoreService
            return new List<LeaderboardItem>();
        }

        public async Task<IEnumerable<LeaderboardItem>> GetTeamRankingsAsync(string teamId, EvenTeamFormat evenTeamFormat)
        {
            // TODO: Implement in CoreService
            return new List<LeaderboardItem>();
        }
    }
}