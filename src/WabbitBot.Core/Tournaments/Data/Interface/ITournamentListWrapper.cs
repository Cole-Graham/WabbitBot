using System;
using System.Collections.Generic;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Tournaments;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models.Interface;

namespace WabbitBot.Core.Tournaments.Data.Interface
{
    /// <summary>
    /// Defines the contract for managing tournament collections.
    /// This interface provides thread-safe operations for managing tournaments,
    /// including filtering, searching, and retrieving tournaments by various criteria.
    /// </summary>
    public interface ITournamentListWrapper : IBaseEntity
    {
        /// <summary>
        /// Gets the last time the collection was updated.
        /// </summary>
        DateTime LastUpdated { get; }

        /// <summary>
        /// Gets or sets whether to include inactive tournaments in queries.
        /// </summary>
        bool IncludeInactive { get; set; }

        /// <summary>
        /// Gets or sets the game size filter for queries.
        /// </summary>
        GameSize? FilterByGameSize { get; set; }

        /// <summary>
        /// Gets a read-only dictionary of all tournaments.
        /// </summary>
        IReadOnlyDictionary<string, Tournament> Tournaments { get; }

        /// <summary>
        /// Adds a tournament to the collection.
        /// </summary>
        void AddTournament(Tournament tournament);

        /// <summary>
        /// Attempts to get a tournament by its ID.
        /// </summary>
        bool TryGetTournament(string tournamentId, out Tournament? tournament);

        /// <summary>
        /// Removes a tournament from the collection.
        /// </summary>
        bool RemoveTournament(string tournamentId);

        /// <summary>
        /// Gets tournaments by their status.
        /// </summary>
        IEnumerable<Tournament> GetTournamentsByStatus(TournamentStatus status);

        /// <summary>
        /// Gets tournaments by game size.
        /// </summary>
        IEnumerable<Tournament> GetTournamentsByGameSize(GameSize gameSize);

        /// <summary>
        /// Gets upcoming tournaments (in registration status).
        /// </summary>
        IEnumerable<Tournament> GetUpcomingTournaments();

        /// <summary>
        /// Gets active tournaments (in progress).
        /// </summary>
        IEnumerable<Tournament> GetActiveTournaments();

        /// <summary>
        /// Gets filtered tournaments based on current filter settings.
        /// </summary>
        IEnumerable<Tournament> GetFilteredTournaments();

        /// <summary>
        /// Gets tournaments within a date range.
        /// </summary>
        IEnumerable<Tournament> GetTournamentsByDateRange(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets tournaments by maximum participant count.
        /// </summary>
        IEnumerable<Tournament> GetTournamentsByMaxParticipants(int maxParticipants);

        /// <summary>
        /// Searches tournaments by name or description.
        /// </summary>
        IEnumerable<Tournament> SearchTournaments(string searchTerm);
    }
}