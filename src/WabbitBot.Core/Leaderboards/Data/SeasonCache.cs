using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Leaderboards.Data.Interface;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class SeasonCache : Cache<Season, SeasonListWrapper>, ISeasonCache
    {
        private const string KeyPrefix = "season:";
        private const string ActiveKeyPrefix = "seasons:active:";
        private const string ListKey = "seasons:all";
        private const int MaxSeasonCacheSize = 100; // Reasonable limit for seasons

        public SeasonCache() : base(MaxSeasonCacheSize)
        {
        }

        public async Task<Season?> GetSeasonAsync(Guid id)
        {
            return await GetAsync($"{KeyPrefix}{id}");
        }

        public async Task SetSeasonAsync(Season season, TimeSpan? expiry = null)
        {
            await SetAsync($"{KeyPrefix}{season.Id}", season, expiry);
        }

        public async Task<bool> RemoveSeasonAsync(Guid seasonId)
        {
            return await RemoveAsync($"{KeyPrefix}{seasonId}");
        }

        public async Task<bool> SeasonExistsAsync(Guid seasonId)
        {
            return await ExistsAsync($"{KeyPrefix}{seasonId}");
        }

        public async Task<Season?> GetActiveSeasonAsync(EvenTeamFormat evenTeamFormat)
        {
            return await GetAsync($"{ActiveKeyPrefix}{evenTeamFormat}");
        }

        public async Task SetActiveSeasonAsync(Season season, TimeSpan? expiry = null)
        {
            await SetAsync($"{ActiveKeyPrefix}{season.EvenTeamFormat}", season, expiry);
        }

        public async Task<bool> RemoveActiveSeasonAsync(EvenTeamFormat evenTeamFormat)
        {
            return await RemoveAsync($"{ActiveKeyPrefix}{evenTeamFormat}");
        }

        public async Task<bool> ActiveSeasonExistsAsync(EvenTeamFormat evenTeamFormat)
        {
            return await ExistsAsync($"{ActiveKeyPrefix}{evenTeamFormat}");
        }

        public async Task<SeasonListWrapper> GetActiveSeasonsAsync()
        {
            return await GetCollectionAsync($"{ActiveKeyPrefix}all") ?? new SeasonListWrapper();
        }

        public async Task SetActiveSeasonsAsync(SeasonListWrapper seasons, TimeSpan? expiry = null)
        {
            await SetCollectionAsync($"{ActiveKeyPrefix}all", seasons, expiry);
        }

        public async Task<bool> RemoveActiveSeasonsAsync()
        {
            return await RemoveCollectionAsync($"{ActiveKeyPrefix}all");
        }

        public async Task<bool> ActiveSeasonsExistAsync()
        {
            return await CollectionExistsAsync($"{ActiveKeyPrefix}all");
        }

        public async Task<SeasonListWrapper> GetAllSeasonsAsync()
        {
            return await GetCollectionAsync(ListKey) ?? new SeasonListWrapper();
        }

        public async Task SetAllSeasonsAsync(SeasonListWrapper seasons, TimeSpan? expiry = null)
        {
            await SetCollectionAsync(ListKey, seasons, expiry);
        }

        public async Task<bool> RemoveAllSeasonsAsync()
        {
            return await RemoveCollectionAsync(ListKey);
        }

        public async Task<bool> AllSeasonsExistAsync()
        {
            return await CollectionExistsAsync(ListKey);
        }
    }
}