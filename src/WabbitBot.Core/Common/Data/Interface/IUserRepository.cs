using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByDiscordIdAsync(ulong discordId);
        Task<IEnumerable<User>> GetInactiveUsersAsync(TimeSpan inactivityThreshold);
        Task UpdateLastActiveAsync(string userId);
    }
}