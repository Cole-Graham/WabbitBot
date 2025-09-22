using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    /// <summary>
    /// Defines the contract for caching season data.
    /// This interface provides methods for managing season data in a cache.
    /// </summary>
    public interface ISeasonCache : ICache<Season>
    {
        /// <summary>
        /// Gets a season by its ID from the cache.
        /// </summary>
        Task<Season?> GetSeasonAsync(Guid id);

        /// <summary>
        /// Sets a season in the cache with optional expiry.
        /// </summary>
        Task SetSeasonAsync(Season season, TimeSpan? expiry = null);

        /// <summary>
        /// Removes a season from the cache.
        /// </summary>
        Task<bool> RemoveSeasonAsync(Guid seasonId);

        /// <summary>
        /// Checks if a season exists in the cache.
        /// </summary>
        Task<bool> SeasonExistsAsync(Guid seasonId);

        /// <summary>
        /// Gets the currently active season for a specific game size from the cache.
        /// </summary>
        Task<Season?> GetActiveSeasonAsync(EvenTeamFormat evenTeamFormat);

        /// <summary>
        /// Sets the active season in the cache with optional expiry.
        /// </summary>
        Task SetActiveSeasonAsync(Season season, TimeSpan? expiry = null);

        /// <summary>
        /// Removes the active season for a specific game size from the cache.
        /// </summary>
        Task<bool> RemoveActiveSeasonAsync(EvenTeamFormat evenTeamFormat);

        /// <summary>
        /// Checks if an active season exists in the cache for a specific game size.
        /// </summary>
        Task<bool> ActiveSeasonExistsAsync(EvenTeamFormat evenTeamFormat);
    }
}