using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface
{
    public interface IPlayerCache : ICache<Player>
    {
        Task<Player?> GetByNameAsync(string name);
        Task SetByNameAsync(string name, Player player, TimeSpan expiry);
        Task RemoveByNameAsync(string name);
        Task<IEnumerable<Player>> GetActivePlayersAsync();
        Task SetActivePlayersAsync(IEnumerable<Player> players, TimeSpan expiry);
        Task RemoveActivePlayersAsync();
        Task<IEnumerable<Player>> GetPlayersByTeamIdAsync(string teamId);
        Task SetPlayersByTeamIdAsync(string teamId, IEnumerable<Player> players, TimeSpan expiry);
        Task RemovePlayersByTeamIdAsync(string teamId);
    }
}
