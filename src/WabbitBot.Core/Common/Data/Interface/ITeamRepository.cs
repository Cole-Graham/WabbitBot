using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface ITeamRepository : IBaseRepository<Team>
    {
        Task<Team?> GetByNameAsync(string name);
        Task<Team?> GetByTagAsync(string tag);
        Task<IEnumerable<Team>> GetTeamsByCaptainAsync(string captainId);
        Task<IEnumerable<Team>> GetTeamsByMemberAsync(string memberId);
        Task<IEnumerable<Team>> GetTeamsByGameSizeAsync(GameSize gameSize);
        Task<IEnumerable<Team>> GetInactiveTeamsAsync(TimeSpan inactivityThreshold);
        Task UpdateLastActiveAsync(string teamId);
        Task<IEnumerable<Team>> SearchTeamsAsync(string searchTerm, int limit = 25);
        Task<IEnumerable<Team>> SearchTeamsByGameSizeAsync(string searchTerm, GameSize gameSize, int limit = 25);
    }
}