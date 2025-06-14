using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    /// <summary>
    /// Defines the contract for caching leaderboard data.
    /// This interface provides methods for managing leaderboard data in a cache,
    /// supporting both individual leaderboards and collections.
    /// </summary>
    public interface ILeaderboardCache : IBaseCache<Leaderboard>, ICollectionCache<Leaderboard, LeaderboardListWrapper>
    {
        /// <summary>
        /// Gets a leaderboard by its ID from the cache.
        /// </summary>
        Task<Leaderboard?> GetLeaderboardAsync(Guid id);

        /// <summary>
        /// Sets a leaderboard in the cache with optional expiry.
        /// </summary>
        Task SetLeaderboardAsync(Leaderboard leaderboard, TimeSpan? expiry = null);

        /// <summary>
        /// Removes a leaderboard from the cache.
        /// </summary>
        Task<bool> RemoveLeaderboardAsync(Guid id);

        /// <summary>
        /// Checks if a leaderboard exists in the cache.
        /// </summary>
        Task<bool> LeaderboardExistsAsync(Guid id);

        /// <summary>
        /// Gets the collection of active leaderboards from the cache.
        /// </summary>
        Task<LeaderboardListWrapper> GetActiveLeaderboardsAsync();

        /// <summary>
        /// Sets the collection of active leaderboards in the cache with optional expiry.
        /// </summary>
        Task SetActiveLeaderboardsAsync(LeaderboardListWrapper leaderboards, TimeSpan? expiry = null);

        /// <summary>
        /// Removes the collection of active leaderboards from the cache.
        /// </summary>
        Task<bool> RemoveActiveLeaderboardsAsync();

        /// <summary>
        /// Checks if active leaderboards exist in the cache.
        /// </summary>
        Task<bool> ActiveLeaderboardsExistAsync();
    }
}