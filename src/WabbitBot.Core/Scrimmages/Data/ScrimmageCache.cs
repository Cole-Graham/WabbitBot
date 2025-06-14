using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Scrimmages.Data
{
    public class ScrimmageCache : BaseCache<Scrimmage, ScrimmageListWrapper>
    {
        private const string KeyPrefix = "scrimmage:";
        private const string ListKey = "scrimmages:active";
        private const string TeamKeyPrefix = "team:scrimmages:";

        public ScrimmageCache() : base()
        {
        }

        public async Task<Scrimmage?> GetScrimmageAsync(Guid id)
        {
            return await GetAsync($"{KeyPrefix}{id}");
        }

        public async Task SetScrimmageAsync(Scrimmage scrimmage, TimeSpan? expiry = null)
        {
            await SetAsync($"{KeyPrefix}{scrimmage.Id}", scrimmage, expiry);
        }

        public async Task<bool> RemoveScrimmageAsync(Guid id)
        {
            return await RemoveAsync($"{KeyPrefix}{id}");
        }

        public async Task<bool> ScrimmageExistsAsync(Guid id)
        {
            return await ExistsAsync($"{KeyPrefix}{id}");
        }

        public async Task<ScrimmageListWrapper> GetActiveScrimmagesAsync()
        {
            return await GetCollectionAsync(ListKey) ?? new ScrimmageListWrapper();
        }

        public async Task SetActiveScrimmagesAsync(ScrimmageListWrapper scrimmages, TimeSpan? expiry = null)
        {
            await SetCollectionAsync(ListKey, scrimmages, expiry);
        }

        public async Task<bool> RemoveActiveScrimmagesAsync()
        {
            return await RemoveCollectionAsync(ListKey);
        }

        public async Task<bool> ActiveScrimmagesExistAsync()
        {
            return await CollectionExistsAsync(ListKey);
        }

        public async Task<ScrimmageListWrapper> GetTeamScrimmagesAsync(string teamId)
        {
            return await GetCollectionAsync($"{TeamKeyPrefix}{teamId}") ?? new ScrimmageListWrapper();
        }

        public async Task SetTeamScrimmagesAsync(string teamId, ScrimmageListWrapper scrimmages, TimeSpan? expiry = null)
        {
            await SetCollectionAsync($"{TeamKeyPrefix}{teamId}", scrimmages, expiry);
        }

        public async Task<bool> RemoveTeamScrimmagesAsync(string teamId)
        {
            return await RemoveCollectionAsync($"{TeamKeyPrefix}{teamId}");
        }

        public async Task<bool> TeamScrimmagesExistAsync(string teamId)
        {
            return await CollectionExistsAsync($"{TeamKeyPrefix}{teamId}");
        }
    }
}
