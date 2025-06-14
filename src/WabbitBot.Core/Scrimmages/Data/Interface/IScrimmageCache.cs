using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Scrimmages;

namespace WabbitBot.Core.Scrimmages.Data.Interface
{
    /// <summary>
    /// Defines the contract for caching scrimmage data.
    /// This interface provides methods for managing scrimmage data in a cache,
    /// supporting both individual scrimmages and collections.
    /// </summary>
    public interface IScrimmageCache : IBaseCache<Scrimmage>, ICollectionCache<Scrimmage, ScrimmageListWrapper>
    {
        /// <summary>
        /// Gets a scrimmage by its ID from the cache.
        /// </summary>
        Task<Scrimmage?> GetScrimmageAsync(Guid id);

        /// <summary>
        /// Sets a scrimmage in the cache with optional expiry.
        /// </summary>
        Task SetScrimmageAsync(Scrimmage scrimmage, TimeSpan? expiry = null);

        /// <summary>
        /// Removes a scrimmage from the cache.
        /// </summary>
        Task<bool> RemoveScrimmageAsync(Guid id);

        /// <summary>
        /// Checks if a scrimmage exists in the cache.
        /// </summary>
        Task<bool> ScrimmageExistsAsync(Guid id);

        /// <summary>
        /// Gets the collection of active scrimmages from the cache.
        /// </summary>
        Task<ScrimmageListWrapper> GetActiveScrimmagesAsync();

        /// <summary>
        /// Sets the collection of active scrimmages in the cache with optional expiry.
        /// </summary>
        Task SetActiveScrimmagesAsync(ScrimmageListWrapper scrimmages, TimeSpan? expiry = null);

        /// <summary>
        /// Removes the collection of active scrimmages from the cache.
        /// </summary>
        Task<bool> RemoveActiveScrimmagesAsync();

        /// <summary>
        /// Checks if active scrimmages exist in the cache.
        /// </summary>
        Task<bool> ActiveScrimmagesExistAsync();

        /// <summary>
        /// Gets scrimmages for a specific team from the cache.
        /// </summary>
        Task<ScrimmageListWrapper> GetTeamScrimmagesAsync(string teamId);

        /// <summary>
        /// Sets scrimmages for a specific team in the cache with optional expiry.
        /// </summary>
        Task SetTeamScrimmagesAsync(string teamId, ScrimmageListWrapper scrimmages, TimeSpan? expiry = null);

        /// <summary>
        /// Removes scrimmages for a specific team from the cache.
        /// </summary>
        Task<bool> RemoveTeamScrimmagesAsync(string teamId);

        /// <summary>
        /// Checks if scrimmages exist for a specific team in the cache.
        /// </summary>
        Task<bool> TeamScrimmagesExistAsync(string teamId);
    }
}