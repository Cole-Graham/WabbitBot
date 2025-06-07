using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Matches;

namespace WabbitBot.Core.Matches.Data
{
    /// <summary>
    /// Defines the contract for match data access operations.
    /// </summary>
    public interface IMatchRepository : IBaseRepository<Match>
    {
        Task<Match?> GetMatchAsync(string matchId);
        Task<IEnumerable<Match>> GetMatchesByStatusAsync(MatchStatus status);
        Task<IEnumerable<Match>> GetMatchesByTeamAsync(string teamId);
        Task<IEnumerable<Match>> GetMatchesByParentAsync(string parentId, string parentType);
        Task<IEnumerable<Match>> GetRecentMatchesAsync(int count);
    }
}