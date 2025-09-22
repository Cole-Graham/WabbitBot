using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Matches;

namespace WabbitBot.Core.Matches.Data.Interface
{
    /// <summary>
    /// Defines the contract for caching match data.
    /// This interface provides methods for managing match data in a cache,
    /// supporting both individual matches and collections.
    /// </summary>
    public interface IMatchCache : ICache<Match>, ICollectionCache<Match, MatchListWrapper>
    {
        /// <summary>
        /// Gets a match by its ID from the cache.
        /// </summary>
        Task<Match?> GetMatchAsync(Guid id);

        /// <summary>
        /// Sets a match in the cache with optional expiry.
        /// </summary>
        Task SetMatchAsync(Match match, TimeSpan? expiry = null);

        /// <summary>
        /// Removes a match from the cache.
        /// </summary>
        Task<bool> RemoveMatchAsync(string matchId);

        /// <summary>
        /// Checks if a match exists in the cache.
        /// </summary>
        Task<bool> MatchExistsAsync(string matchId);

        /// <summary>
        /// Gets the collection of active matches from the cache.
        /// </summary>
        Task<MatchListWrapper> GetActiveMatchesAsync();

        /// <summary>
        /// Sets the collection of active matches in the cache with optional expiry.
        /// </summary>
        Task SetActiveMatchesAsync(MatchListWrapper matches, TimeSpan? expiry = null);

        /// <summary>
        /// Removes the collection of active matches from the cache.
        /// </summary>
        Task<bool> RemoveActiveMatchesAsync();

        /// <summary>
        /// Checks if active matches exist in the cache.
        /// </summary>
        Task<bool> ActiveMatchesExistAsync();
    }
}