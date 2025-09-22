using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface IUserCache : ICache<User>
    {
        Task<User?> GetByDiscordIdAsync(ulong discordId);
        Task SetByDiscordIdAsync(ulong discordId, User user, TimeSpan expiry);
        Task RemoveByDiscordIdAsync(ulong discordId);
        Task<User?> GetByUsernameAsync(string username);
        Task SetByUsernameAsync(string username, User user, TimeSpan expiry);
        Task RemoveByUsernameAsync(string username);
        Task<IEnumerable<User>> GetActiveUsersAsync();
        Task SetActiveUsersAsync(IEnumerable<User> users, TimeSpan expiry);
        Task RemoveActiveUsersAsync();
        Task<IEnumerable<User>> GetUsersByPlayerIdAsync(string playerId);
        Task SetUsersByPlayerIdAsync(string playerId, IEnumerable<User> users, TimeSpan expiry);
        Task RemoveUsersByPlayerIdAsync(string playerId);
    }
}
