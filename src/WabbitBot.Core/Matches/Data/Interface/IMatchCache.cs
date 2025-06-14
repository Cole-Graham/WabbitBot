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
    public interface IMatchCache : IBaseCache<Match>, ICollectionCache<Match, MatchListWrapper>
    {
        /// <summary>
        /// Gets a match by its ID from the cache.
        /// </summary>
        Task<Match?> GetMatchAsync(string matchId);

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

        /// <summary>
        /// Gets matches for a specific team from the cache.
        /// </summary>
        Task<MatchListWrapper> GetTeamMatchesAsync(string teamId);

        /// <summary>
        /// Sets matches for a specific team in the cache with optional expiry.
        /// </summary>
        Task SetTeamMatchesAsync(string teamId, MatchListWrapper matches, TimeSpan? expiry = null);

        /// <summary>
        /// Removes matches for a specific team from the cache.
        /// </summary>
        Task<bool> RemoveTeamMatchesAsync(string teamId);

        /// <summary>
        /// Checks if matches exist for a specific team in the cache.
        /// </summary>
        Task<bool> TeamMatchesExistAsync(string teamId);

        /// <summary>
        /// Gets matches for a specific parent (tournament/scrimmage) from the cache.
        /// </summary>
        Task<MatchListWrapper> GetParentMatchesAsync(string parentId, string parentType);

        /// <summary>
        /// Sets matches for a specific parent in the cache with optional expiry.
        /// </summary>
        Task SetParentMatchesAsync(string parentId, string parentType, MatchListWrapper matches, TimeSpan? expiry = null);

        /// <summary>
        /// Removes matches for a specific parent from the cache.
        /// </summary>
        Task<bool> RemoveParentMatchesAsync(string parentId, string parentType);

        /// <summary>
        /// Checks if matches exist for a specific parent in the cache.
        /// </summary>
        Task<bool> ParentMatchesExistAsync(string parentId, string parentType);
    }
}