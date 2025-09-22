using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Matches.Data.Interface
{
    /// <summary>
    /// Defines the contract for match data access operations.
    /// </summary>
    public interface IMatchRepository : IRepository<Match>
    {
        Task<Match?> GetMatchAsync(string matchId);
        Task<IEnumerable<Match>> GetMatchesByStatusAsync<T>() where T : MatchStateSnapshot;
        Task<IEnumerable<Match>> GetMatchesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Match>> GetActiveMatchesAsync();
        Task<IEnumerable<Match>> GetCompletedMatchesAsync();
        Task<IEnumerable<Match>> GetMatchesByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat);
        Task<IEnumerable<Match>> GetMatchesByTeamIdAsync(string teamId);
        Task<IEnumerable<Match>> GetMatchesByTournamentIdAsync(string tournamentId);
    }
}