using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Tournaments;

namespace WabbitBot.Core.Tournaments.Data.Interface
{
    /// <summary>
    /// Defines the contract for caching tournament data.
    /// This interface provides methods for managing tournament data in a cache,
    /// supporting both individual tournaments and collections.
    /// </summary>
    public interface ITournamentCache : ICache<Tournament>, ICollectionCache<Tournament, TournamentListWrapper>
    {
        /// <summary>
        /// Gets a tournament by its ID from the cache.
        /// </summary>
        Task<Tournament?> GetTournamentAsync(Guid id);

        /// <summary>
        /// Sets a tournament in the cache with optional expiry.
        /// </summary>
        Task SetTournamentAsync(Tournament tournament, TimeSpan? expiry = null);

        /// <summary>
        /// Removes a tournament from the cache.
        /// </summary>
        Task<bool> RemoveTournamentAsync(Guid tournamentId);

        /// <summary>
        /// Checks if a tournament exists in the cache.
        /// </summary>
        Task<bool> TournamentExistsAsync(Guid tournamentId);

        /// <summary>
        /// Gets the collection of active tournaments from the cache.
        /// </summary>
        Task<TournamentListWrapper> GetActiveTournamentsAsync();

        /// <summary>
        /// Sets the collection of active tournaments in the cache with optional expiry.
        /// </summary>
        Task SetActiveTournamentsAsync(TournamentListWrapper tournaments, TimeSpan? expiry = null);

        /// <summary>
        /// Removes the collection of active tournaments from the cache.
        /// </summary>
        Task<bool> RemoveActiveTournamentsAsync();

        /// <summary>
        /// Checks if active tournaments exist in the cache.
        /// </summary>
        Task<bool> ActiveTournamentsExistAsync();
    }
}