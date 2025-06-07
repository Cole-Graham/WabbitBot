using System.Data;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Core.Matches.Data
{
    public interface IMatchArchive
    {
        Task ArchiveMatchAsync(Match match);
        Task<IEnumerable<Match>> GetArchivedMatchesAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Match>> GetTeamHistoryAsync(string teamId, int limit = 10);
        Task<Match?> GetArchivedMatchAsync(string matchId);
    }
}