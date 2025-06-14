using System;
using System.Collections.Generic;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models.Interface;

namespace WabbitBot.Core.Matches.Data.Interface
{
    /// <summary>
    /// Defines the contract for managing match collections.
    /// This interface provides thread-safe operations for managing matches,
    /// including filtering, searching, and retrieving matches by various criteria.
    /// </summary>
    public interface IMatchListWrapper : IBaseEntity
    {
        /// <summary>
        /// Gets the last time the collection was updated.
        /// </summary>
        DateTime LastUpdated { get; }

        /// <summary>
        /// Gets a read-only dictionary of all matches.
        /// </summary>
        IReadOnlyDictionary<string, Match> Matches { get; }

        /// <summary>
        /// Adds a match to the collection.
        /// </summary>
        void AddMatch(Match match);

        /// <summary>
        /// Attempts to get a match by its ID.
        /// </summary>
        bool TryGetMatch(string matchId, out Match? match);

        /// <summary>
        /// Removes a match from the collection.
        /// </summary>
        bool RemoveMatch(string matchId);

        /// <summary>
        /// Gets matches by their status.
        /// </summary>
        IEnumerable<Match> GetMatchesByStatus(MatchStatus status);

        /// <summary>
        /// Gets matches by team ID.
        /// </summary>
        IEnumerable<Match> GetMatchesByTeam(string teamId);

        /// <summary>
        /// Gets matches by game size.
        /// </summary>
        IEnumerable<Match> GetMatchesByGameSize(GameSize gameSize);

        /// <summary>
        /// Gets matches by parent ID and type.
        /// </summary>
        IEnumerable<Match> GetMatchesByParent(string parentId, string parentType);

        /// <summary>
        /// Gets matches within a date range.
        /// </summary>
        IEnumerable<Match> GetMatchesByDateRange(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets matches by player ID.
        /// </summary>
        IEnumerable<Match> GetMatchesByPlayer(string playerId);

        /// <summary>
        /// Gets active matches (in progress).
        /// </summary>
        IEnumerable<Match> GetActiveMatches();

        /// <summary>
        /// Gets completed matches.
        /// </summary>
        IEnumerable<Match> GetCompletedMatches();

        /// <summary>
        /// Gets matches that are part of a tournament.
        /// </summary>
        IEnumerable<Match> GetTournamentMatches(string tournamentId);

        /// <summary>
        /// Gets matches that are part of a scrimmage.
        /// </summary>
        IEnumerable<Match> GetScrimmageMatches(string scrimmageId);
    }
}