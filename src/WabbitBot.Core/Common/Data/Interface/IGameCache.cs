using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface;

/// <summary>
/// Interface for Game-specific cache operations
/// </summary>
public interface IGameCache : ICache<Game>
{
    /// <summary>
    /// Gets games by match ID from cache
    /// </summary>
    Task<IEnumerable<Game>?> GetGamesByMatchAsync(string matchId);

    /// <summary>
    /// Sets games by match ID in cache
    /// </summary>
    Task SetGamesByMatchAsync(string matchId, IEnumerable<Game> games, TimeSpan expiry);

    /// <summary>
    /// Removes games by match ID from cache
    /// </summary>
    Task RemoveGamesByMatchAsync(string matchId);
}
