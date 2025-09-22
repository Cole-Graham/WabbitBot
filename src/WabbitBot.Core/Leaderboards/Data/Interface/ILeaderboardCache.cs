using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    /// <summary>
    /// Defines the contract for caching leaderboard data.
    /// This interface provides methods for managing leaderboard data in a cache.
    /// </summary>
    public interface ILeaderboardCache : ICache<Leaderboard>
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
        Task<bool> RemoveLeaderboardAsync(Guid leaderboardId);

        /// <summary>
        /// Checks if a leaderboard exists in the cache.
        /// </summary>
        Task<bool> LeaderboardExistsAsync(Guid leaderboardId);

        /// <summary>
        /// Gets the top rankings for a specific game size from cached data.
        /// </summary>
        Task<IEnumerable<LeaderboardItem>> GetTopRankingsAsync(EvenTeamFormat evenTeamFormat, int count = 10);

        /// <summary>
        /// Gets team rankings for a specific team and game size from cached data.
        /// </summary>
        Task<IEnumerable<LeaderboardItem>> GetTeamRankingsAsync(string teamId, EvenTeamFormat evenTeamFormat);
    }
}