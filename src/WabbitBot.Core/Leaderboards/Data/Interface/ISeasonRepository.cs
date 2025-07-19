using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data.Interface
{
    /// <summary>
    /// Defines the contract for season data access operations.
    /// This interface is part of the core domain model and should be implemented
    /// by infrastructure-specific repositories (e.g., SQL, NoSQL).
    /// </summary>
    public interface ISeasonRepository : IBaseRepository<Season>
    {
        /// <summary>
        /// Retrieves the currently active season.
        /// </summary>
        Task<Season?> GetActiveSeasonAsync();

        /// <summary>
        /// Retrieves seasons within a specific date range.
        /// </summary>
        Task<IEnumerable<Season>> GetSeasonsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}