using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface ITeamRepository : IRepository<Team>
    {
        Task<Team?> GetByNameAsync(string name);
        Task<Team?> GetByTagAsync(string tag);
        Task<IEnumerable<Team>> GetTeamsByCaptainAsync(string captainId);
        Task<IEnumerable<Team>> GetTeamsByMemberAsync(string memberId);
        Task<IEnumerable<Team>> GetTeamsByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat);
        Task<IEnumerable<Team>> GetInactiveTeamsAsync(TimeSpan inactivityThreshold);
        Task UpdateLastActiveAsync(string teamId);
        Task ArchiveTeamAsync(string teamId);
        Task UnarchiveTeamAsync(string teamId);
        Task<IEnumerable<Team>> GetArchivedTeamsAsync();
        Task<IEnumerable<Team>> GetArchivedTeamsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Team>> SearchTeamsAsync(string searchTerm, int limit = 25);
        Task<IEnumerable<Team>> SearchTeamsByEvenTeamFormatAsync(string searchTerm, EvenTeamFormat evenTeamFormat, int limit = 25);
    }
}