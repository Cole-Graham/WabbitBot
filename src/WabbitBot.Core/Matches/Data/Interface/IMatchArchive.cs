using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Matches.Data.Interface
{
    /// <summary>
    /// Defines the contract for match archive operations.
    /// This interface provides methods for archiving and retrieving archived match data.
    /// </summary>
    public interface IMatchArchive : IArchive<Match>
    {
        /// <summary>
        /// Gets the match history for a specific team
        /// </summary>
        /// <param name="teamId">The team ID to get history for</param>
        /// <param name="limit">Maximum number of matches to return</param>
        /// <returns>Collection of archived matches for the team</returns>
        Task<IEnumerable<Match>> GetTeamHistoryAsync(string teamId, int limit = 10);

        /// <summary>
        /// Gets archived matches within a date range
        /// </summary>
        /// <param name="startDate">Start date for the range</param>
        /// <param name="endDate">End date for the range</param>
        /// <returns>Collection of archived matches within the date range</returns>
        Task<IEnumerable<Match>> GetMatchesByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets archived matches by game size
        /// </summary>
        /// <param name="evenTeamFormat">The game size to filter by</param>
        /// <returns>Collection of archived matches with the specified game size</returns>
        Task<IEnumerable<Match>> GetMatchesByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat);

        /// <summary>
        /// Gets archived matches by tournament ID
        /// </summary>
        /// <param name="tournamentId">The tournament ID to filter by</param>
        /// <returns>Collection of archived matches from the tournament</returns>
        Task<IEnumerable<Match>> GetMatchesByTournamentIdAsync(string tournamentId);
    }
}
