using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Common.Data.Cache
{
    public class UserCache : BaseCache<User, User>, IBaseCache<User>
    {
        private readonly IUserRepository _repository;

        public UserCache(IUserRepository repository, TimeSpan defaultExpiry)
            : base()
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

        public void Clear()
        {
            CleanExpiredEntries();
        }
    }
}