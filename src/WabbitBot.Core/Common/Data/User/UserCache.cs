using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Common.Data.Cache
{
    public class UserCache : Cache<User, User>, IUserCache
    {
        private readonly IUserRepository _repository;
        private const int MaxUserCacheSize = 10000; // As specified in data design specification

        public UserCache(IUserRepository repository, TimeSpan defaultExpiry)
            : base(MaxUserCacheSize)
        {
            _repository = repository;
        }

        public async Task<User?> GetByDiscordIdAsync(ulong discordId)
        {
            var key = discordId.ToString();
            var cachedUser = await GetAsync(key);
            if (cachedUser != null)
            {
                return cachedUser;
            }

            var user = await _repository.GetByDiscordIdAsync(discordId);
            if (user != null)
            {
                await SetAsync(key, user);
            }
            return user;
        }

        public async Task SetByDiscordIdAsync(ulong discordId, User user, TimeSpan expiry)
        {
            var key = discordId.ToString();
            await SetAsync(key, user, expiry);
        }

        public async Task RemoveByDiscordIdAsync(ulong discordId)
        {
            var key = discordId.ToString();
            await RemoveAsync(key);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            // TODO: Implement username-based caching
            await Task.CompletedTask;
            return null;
        }

        public async Task SetByUsernameAsync(string username, User user, TimeSpan expiry)
        {
            // TODO: Implement username-based caching
            await Task.CompletedTask;
        }

        public async Task RemoveByUsernameAsync(string username)
        {
            // TODO: Implement username-based cache removal
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<User>> GetActiveUsersAsync()
        {
            // TODO: Implement active users collection caching
            await Task.CompletedTask;
            return new List<User>();
        }

        public async Task SetActiveUsersAsync(IEnumerable<User> users, TimeSpan expiry)
        {
            // TODO: Implement active users collection caching
            await Task.CompletedTask;
        }

        public async Task RemoveActiveUsersAsync()
        {
            // TODO: Implement active users collection cache removal
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<User>> GetUsersByPlayerIdAsync(string playerId)
        {
            // TODO: Implement player users caching
            await Task.CompletedTask;
            return new List<User>();
        }

        public async Task SetUsersByPlayerIdAsync(string playerId, IEnumerable<User> users, TimeSpan expiry)
        {
            // TODO: Implement player users caching
            await Task.CompletedTask;
        }

        public async Task RemoveUsersByPlayerIdAsync(string playerId)
        {
            // TODO: Implement player users cache removal
            await Task.CompletedTask;
        }

        public new void Clear()
        {
            base.Clear();
        }
    }
}