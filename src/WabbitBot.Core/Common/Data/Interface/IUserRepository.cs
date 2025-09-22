using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByDiscordIdAsync(ulong discordId);
        Task<User?> GetByUsernameAsync(string username);
        Task<IEnumerable<User>> GetInactiveUsersAsync(TimeSpan inactivityThreshold);
        Task UpdateLastActiveAsync(string userId);
        Task<IEnumerable<User>> GetUsersByPlayerIdAsync(string playerId);
        Task ArchiveUserAsync(string userId);
        Task UnarchiveUserAsync(string userId);
        Task<IEnumerable<User>> GetArchivedUsersAsync();
        Task<IEnumerable<User>> GetArchivedUsersByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}