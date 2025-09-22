using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface IPlayerRepository : IRepository<Player>
    {
        Task<Player?> GetByNameAsync(string name);
        Task<IEnumerable<Player>> GetInactivePlayersAsync(TimeSpan inactivityThreshold);
        Task UpdateLastActiveAsync(string playerId);
        Task<IEnumerable<Player>> GetPlayersByTeamIdAsync(string teamId);
        Task ArchivePlayerAsync(string playerId);
        Task UnarchivePlayerAsync(string playerId);
        Task<IEnumerable<Player>> GetArchivedPlayersAsync();
        Task<IEnumerable<Player>> GetArchivedPlayersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<PlayerWithUserDetails?> GetPlayerWithUserDetailsAsync(string playerId);
        Task<IEnumerable<PlayerWithUserDetails>> GetPlayersWithUserDetailsByTeamIdAsync(string teamId);
    }
}