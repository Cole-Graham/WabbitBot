using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    /// <summary>
    /// Defines the contract for caching season data.
    /// This interface provides methods for managing season data in a cache,
    /// supporting both individual seasons and collections.
    /// </summary>
    public interface ISeasonCache : IBaseCache<Season>, ICollectionCache<Season, SeasonListWrapper>
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
        Task<bool> RemoveSeasonAsync(Guid id);

        /// <summary>
        /// Checks if a season exists in the cache.
        /// </summary>
        Task<bool> SeasonExistsAsync(Guid id);

        /// <summary>
        /// Gets the currently active season from the cache.
        /// </summary>
        Task<Season?> GetActiveSeasonAsync();

        /// <summary>
        /// Sets the active season in the cache with optional expiry.
        /// </summary>
        Task SetActiveSeasonAsync(Season season, TimeSpan? expiry = null);

        /// <summary>
        /// Removes the active season from the cache.
        /// </summary>
        Task<bool> RemoveActiveSeasonAsync();

        /// <summary>
        /// Checks if an active season exists in the cache.
        /// </summary>
        Task<bool> ActiveSeasonExistsAsync();

        /// <summary>
        /// Gets all seasons from the cache.
        /// </summary>
        Task<SeasonListWrapper> GetAllSeasonsAsync();
    }
}