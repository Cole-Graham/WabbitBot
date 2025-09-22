using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface ITeamCache : ICache<Team>
    {
        Task SetTeamAsync(Team team, TimeSpan? expiry = null);
        Task<bool> RemoveTeamAsync(string teamId);
        Task<bool> TeamExistsAsync(string teamId);
        // TODO: Methods using TeamListWrapper moved to CoreService per refactor plan step 5.5
        // Task GetActiveTeamsAsync(EvenTeamFormat? evenTeamFormat = null);
        // Task SetActiveTeamsAsync(IEnumerable<Team> teams, TimeSpan? expiry = null);
        // Task<bool> RemoveActiveTeamsAsync();
        // Task<bool> ActiveTeamsExistAsync();
    }
}
