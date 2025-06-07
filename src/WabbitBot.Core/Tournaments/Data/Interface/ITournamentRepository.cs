using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Tournaments;

namespace WabbitBot.Core.Tournaments.Data
{
    public interface ITournamentRepository : IBaseRepository<Tournament>
    {
        Task<Tournament?> GetTournamentAsync(string tournamentId);
        Task<IEnumerable<Tournament>> GetTournamentsByStatusAsync(TournamentStatus status);
        Task<IEnumerable<Tournament>> GetActiveTournamentsAsync();
        Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync();
    }
}