using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Scrimmages.Data.Interface
{
    /// <summary>
    /// Defines the contract for scrimmage data access operations.
    /// This interface is part of the core domain model and should be implemented
    /// by infrastructure-specific repositories (e.g., SQL, NoSQL).
    /// </summary>
    public interface IScrimmageRepository : IRepository<Scrimmage>
    {
        /// <summary>
        /// Retrieves a scrimmage by its ID.
        /// </summary>
        Task<Scrimmage?> GetScrimmageAsync(string scrimmageId);

        /// <summary>
        /// Retrieves all scrimmages for a specific team.
        /// </summary>
        Task<IEnumerable<Scrimmage>> GetScrimmagesByStatusAsync(ScrimmageStatus status);

        /// <summary>
        /// Retrieves scrimmages by their status.
        /// </summary>
        Task<IEnumerable<Scrimmage>> GetScrimmagesByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Retrieves the most recent scrimmages.
        /// </summary>
        Task<IEnumerable<Scrimmage>> GetActiveScrimmagesAsync();

        /// <summary>
        /// Retrieves the most recent scrimmages.
        /// </summary>
        Task<IEnumerable<Scrimmage>> GetCompletedScrimmagesAsync();

        /// <summary>
        /// Retrieves the most recent scrimmages.
        /// </summary>
        Task<IEnumerable<Scrimmage>> GetScrimmagesByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat);

        /// <summary>
        /// Retrieves the most recent scrimmages.
        /// </summary>
        Task<IEnumerable<Scrimmage>> GetScrimmagesByTeamIdAsync(string teamId);

        #region Hybrid Data Access Methods

        /// <summary>
        /// Gets a scrimmage using hybrid approach: state machine first, then database
        /// </summary>
        Task<Scrimmage?> GetScrimmageHybridAsync(string scrimmageId);

        /// <summary>
        /// Creates a scrimmage and adds it to the state machine
        /// </summary>
        Task<Scrimmage> CreateScrimmageHybridAsync(Scrimmage scrimmage);

        /// <summary>
        /// Updates a scrimmage and synchronizes with state machine
        /// </summary>
        Task<bool> UpdateScrimmageHybridAsync(Scrimmage scrimmage);

        /// <summary>
        /// Loads all active scrimmages from database into state machine
        /// </summary>
        Task LoadActiveScrimmagesAsync();

        #endregion
    }
}