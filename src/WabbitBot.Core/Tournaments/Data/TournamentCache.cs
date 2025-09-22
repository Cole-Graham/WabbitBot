using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Tournaments.Data.Interface;

namespace WabbitBot.Core.Tournaments.Data
{
    public class TournamentCache : Cache<Tournament, TournamentListWrapper>, ITournamentCache
    {
        private const string KeyPrefix = "tournament:";
        private const string ListKey = "tournaments:active";
        private const int MaxTournamentCacheSize = 1000; // Reasonable limit for tournaments

        public TournamentCache() : base(MaxTournamentCacheSize)
        {
        }

        public async Task<Tournament?> GetTournamentAsync(Guid id)
        {
            return await GetAsync($"{KeyPrefix}{id}");
        }

        public async Task SetTournamentAsync(Tournament tournament, TimeSpan? expiry = null)
        {
            await SetAsync($"{KeyPrefix}{tournament.Id}", tournament, expiry);
        }

        public async Task<bool> RemoveTournamentAsync(Guid id)
        {
            return await RemoveAsync($"{KeyPrefix}{id}");
        }

        public async Task<bool> TournamentExistsAsync(Guid id)
        {
            return await ExistsAsync($"{KeyPrefix}{id}");
        }

        public async Task<TournamentListWrapper> GetActiveTournamentsAsync()
        {
            return await GetCollectionAsync(ListKey) ?? new TournamentListWrapper();
        }

        public async Task SetActiveTournamentsAsync(TournamentListWrapper tournaments, TimeSpan? expiry = null)
        {
            await SetCollectionAsync(ListKey, tournaments, expiry);
        }

        public async Task<bool> RemoveActiveTournamentsAsync()
        {
            return await RemoveCollectionAsync(ListKey);
        }

        public async Task<bool> ActiveTournamentsExistAsync()
        {
            return await CollectionExistsAsync(ListKey);
        }
    }
}