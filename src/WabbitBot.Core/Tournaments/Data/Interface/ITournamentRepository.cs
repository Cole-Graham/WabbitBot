using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Tournaments;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Tournaments.Data;

namespace WabbitBot.Core.Tournaments.Data.Interface
{
    public interface ITournamentRepository : IRepository<Tournament>
    {
        Task<Tournament?> GetTournamentAsync(string tournamentId);
        Task<IEnumerable<Tournament>> GetTournamentsByStatusAsync<T>() where T : TournamentStateSnapshot;
        Task<IEnumerable<Tournament>> GetTournamentsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Tournament>> GetActiveTournamentsAsync();
        Task<IEnumerable<Tournament>> GetCompletedTournamentsAsync();
        Task<IEnumerable<Tournament>> GetTournamentsByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat);
    }
}