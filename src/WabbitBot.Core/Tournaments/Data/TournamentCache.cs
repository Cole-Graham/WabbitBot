using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Core.Tournaments.Data
{
    public class TournamentCache : BaseCache<Tournament, TournamentListWrapper>
    {
        private const string KeyPrefix = "tournament:";
        private const string ListKey = "tournaments:active";

        public TournamentCache() : base()
        {
        }

        public async Task<Tournament> GetTournamentAsync(Guid id)
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
            return await GetCollectionAsync(ListKey);
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