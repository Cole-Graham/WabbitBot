using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class SeasonCache : BaseCache<Season, SeasonListWrapper>
    {
        private const string KeyPrefix = "season:";
        private const string ActiveKey = "seasons:active";
        private const string ListKey = "seasons:all";

        public SeasonCache() : base()
        {
        }

        public async Task<Season> GetSeasonAsync(Guid id)
        {
            return await GetAsync($"{KeyPrefix}{id}");
        }

        public async Task SetSeasonAsync(Season season, TimeSpan? expiry = null)
        {
            await SetAsync($"{KeyPrefix}{season.Id}", season, expiry);
        }

        public async Task<bool> RemoveSeasonAsync(Guid id)
        {
            return await RemoveAsync($"{KeyPrefix}{id}");
        }

        public async Task<bool> SeasonExistsAsync(Guid id)
        {
            return await ExistsAsync($"{KeyPrefix}{id}");
        }

        public async Task<Season> GetActiveSeasonAsync()
        {
            return await GetAsync(ActiveKey);
        }

        public async Task SetActiveSeasonAsync(Season season, TimeSpan? expiry = null)
        {
            await SetAsync(ActiveKey, season, expiry);
        }

        public async Task<bool> RemoveActiveSeasonAsync()
        {
            return await RemoveAsync(ActiveKey);
        }

        public async Task<bool> ActiveSeasonExistsAsync()
        {
            return await ExistsAsync(ActiveKey);
        }

        public async Task<SeasonListWrapper> GetAllSeasonsAsync()
        {
            return await GetCollectionAsync(ListKey);
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