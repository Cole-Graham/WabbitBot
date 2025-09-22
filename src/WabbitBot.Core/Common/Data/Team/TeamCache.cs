using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Cache
{
    public class TeamCache : Cache<Team, TeamListWrapper>, ITeamCache
    {
        private readonly ITeamRepository _repository;
        private const string KeyPrefix = "team:";
        private const string ListKey = "teams:active";
        private const int MaxTeamCacheSize = 2000; // Reasonable limit for teams

        public TeamCache(ITeamRepository repository) : base(MaxTeamCacheSize)
        {
            _repository = repository;
        }

        public async Task SetTeamAsync(Team team, TimeSpan? expiry = null)
        {
            await SetAsync($"{KeyPrefix}{team.Id}", team, expiry);
        }

        public async Task<bool> RemoveTeamAsync(string teamId)
        {
            return await RemoveAsync($"{KeyPrefix}{teamId}");
        }

        public async Task<bool> TeamExistsAsync(string teamId)
        {
            return await ExistsAsync($"{KeyPrefix}{teamId}");
        }

        public async Task<TeamListWrapper> GetActiveTeamsAsync(EvenTeamFormat? evenTeamFormat = null)
        {
            var teams = await GetCollectionAsync(ListKey);
            if (teams != null)
            {
                teams.FilterByEvenTeamFormat = evenTeamFormat;
            }
            return teams ?? new TeamListWrapper();
        }

        public async Task SetActiveTeamsAsync(TeamListWrapper teams, TimeSpan? expiry = null)
        {
            await SetCollectionAsync(ListKey, teams, expiry);
        }

        public async Task<bool> RemoveActiveTeamsAsync()
        {
            return await RemoveCollectionAsync(ListKey);
        }

        public async Task<bool> ActiveTeamsExistAsync()
        {
            return await CollectionExistsAsync(ListKey);
        }
    }
}