using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data
{
    public class PlayerCache : Cache<Player, PlayerListWrapper>, IPlayerCache
    {
        private readonly IPlayerRepository _repository;
        private const string NameKeyPrefix = "name:";
        private const int MaxPlayerCacheSize = 5000; // As specified in data design specification

        public PlayerCache(IPlayerRepository repository, TimeSpan defaultExpiry)
            : base(MaxPlayerCacheSize)
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

        public async Task SetByNameAsync(string name, Player player, TimeSpan expiry)
        {
            var key = $"{NameKeyPrefix}{name}";
            await SetAsync(key, player, expiry);
            await SetAsync(player.Id.ToString(), player, expiry); // Also cache by ID
        }

        public async Task RemoveByNameAsync(string name)
        {
            var key = $"{NameKeyPrefix}{name}";
            await RemoveAsync(key);
        }

        public async Task<IEnumerable<Player>> GetActivePlayersAsync()
        {
            // TODO: Implement active players collection caching
            await Task.CompletedTask;
            return new List<Player>();
        }

        public async Task SetActivePlayersAsync(IEnumerable<Player> players, TimeSpan expiry)
        {
            // TODO: Implement active players collection caching
            await Task.CompletedTask;
        }

        public async Task RemoveActivePlayersAsync()
        {
            // TODO: Implement active players collection cache removal
            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Player>> GetPlayersByTeamIdAsync(string teamId)
        {
            // TODO: Implement team players caching
            await Task.CompletedTask;
            return new List<Player>();
        }

        public async Task SetPlayersByTeamIdAsync(string teamId, IEnumerable<Player> players, TimeSpan expiry)
        {
            // TODO: Implement team players caching
            await Task.CompletedTask;
        }

        public async Task RemovePlayersByTeamIdAsync(string teamId)
        {
            // TODO: Implement team players cache removal
            await Task.CompletedTask;
        }
    }
}