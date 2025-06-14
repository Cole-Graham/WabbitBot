using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Cache
{
    public class PlayerCache : BaseCache<Player, PlayerListWrapper>
    {
        private readonly IPlayerRepository _repository;
        private const string NameKeyPrefix = "name:";

        public PlayerCache(IPlayerRepository repository, TimeSpan defaultExpiry)
            : base()
        {
            _repository = repository;
        }

        public async Task<Player?> GetByNameAsync(string name)
        {
            var key = $"{NameKeyPrefix}{name}";
            var cachedPlayer = await GetAsync(key);
            if (cachedPlayer != null)
            {
                return cachedPlayer;
            }

            var player = await _repository.GetByNameAsync(name);
            if (player != null)
            {
                await SetAsync(key, player);
                await SetAsync(player.Id.ToString(), player); // Also cache by ID
            }
            return player;
        }
    }
}