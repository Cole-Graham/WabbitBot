using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class LeaderboardCache : BaseCache<Leaderboard, LeaderboardListWrapper>
    {
        private const string KeyPrefix = "leaderboard:";
        private const string ListKey = "leaderboards:active";

        public LeaderboardCache() : base()
        {
        }

        public async Task<Leaderboard> GetLeaderboardAsync(Guid id)
        {
            return await GetAsync($"{KeyPrefix}{id}");
        }

        public async Task SetLeaderboardAsync(Leaderboard leaderboard, TimeSpan? expiry = null)
        {
            await SetAsync($"{KeyPrefix}{leaderboard.Id}", leaderboard, expiry);
        }

        public async Task<bool> RemoveLeaderboardAsync(Guid id)
        {
            return await RemoveAsync($"{KeyPrefix}{id}");
        }

        public async Task<bool> LeaderboardExistsAsync(Guid id)
        {
            return await ExistsAsync($"{KeyPrefix}{id}");
        }

        public async Task<LeaderboardListWrapper> GetActiveLeaderboardsAsync()
        {
            return await GetCollectionAsync(ListKey);
        }

        public async Task SetActiveLeaderboardsAsync(LeaderboardListWrapper leaderboards, TimeSpan? expiry = null)
        {
            await SetCollectionAsync(ListKey, leaderboards, expiry);
        }

        public async Task<bool> RemoveActiveLeaderboardsAsync()
        {
            return await RemoveCollectionAsync(ListKey);
        }

        public async Task<bool> ActiveLeaderboardsExistAsync()
        {
            return await CollectionExistsAsync(ListKey);
        }
    }
}