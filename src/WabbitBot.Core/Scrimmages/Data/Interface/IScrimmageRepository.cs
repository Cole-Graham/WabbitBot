using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Scrimmages.Data
{
    /// <summary>
    /// Defines the contract for scrimmage data access operations.
    /// This interface is part of the core domain model and should be implemented
    /// by infrastructure-specific repositories (e.g., SQL, NoSQL).
    /// </summary>
    public interface IScrimmageRepository : IBaseRepository<Scrimmage>
    {
        /// <summary>
        /// Retrieves a scrimmage by its ID.
        /// </summary>
        Task<Scrimmage?> GetScrimmageAsync(string scrimmageId);

        /// <summary>
        /// Retrieves all scrimmages for a specific team.
        /// </summary>
        Task<IEnumerable<Scrimmage>> GetScrimmagesByTeamAsync(string teamId);

        /// <summary>
        /// Retrieves scrimmages by their status.
        /// </summary>
        Task<IEnumerable<Scrimmage>> GetScrimmagesByStatusAsync(ScrimmageStatus status);

        /// <summary>
        /// Retrieves the most recent scrimmages.
        /// </summary>
        Task<IEnumerable<Scrimmage>> GetRecentScrimmagesAsync(int count);
    }
}