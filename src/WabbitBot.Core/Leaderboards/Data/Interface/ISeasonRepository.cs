using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    /// <summary>
    /// Defines the contract for season data access operations.
    /// This interface is part of the core domain model and should be implemented
    /// by infrastructure-specific repositories (e.g., SQL, NoSQL).
    /// </summary>
    public interface ISeasonRepository : IRepository<Season>
    {
        Task<Season?> GetSeasonAsync(string seasonId);
        Task<Season?> GetActiveSeasonAsync(EvenTeamFormat evenTeamFormat);
        Task<IEnumerable<Season>> GetSeasonsByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat);
        Task<IEnumerable<Season>> GetSeasonsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Season>> GetCompletedSeasonsAsync(EvenTeamFormat evenTeamFormat);
    }
}