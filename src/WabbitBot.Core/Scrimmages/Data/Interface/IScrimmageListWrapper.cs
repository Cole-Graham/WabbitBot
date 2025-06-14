using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models.Interface;

namespace WabbitBot.Core.Scrimmages.Data.Interface
{
    /// <summary>
    /// Defines the contract for managing scrimmage collections.
    /// This interface provides thread-safe operations for managing scrimmages
    /// and their associated matches across different game sizes.
    /// </summary>
    public interface IScrimmageListWrapper : IBaseEntity
    {
        /// <summary>
        /// Gets the last time the scrimmage collection was updated.
        /// </summary>
        DateTime LastUpdated { get; }

        /// <summary>
        /// Gets a read-only dictionary of all scrimmages.
        /// </summary>
        IReadOnlyDictionary<string, Scrimmage> Scrimmages { get; }

        /// <summary>
        /// Adds a scrimmage to the collection.
        /// </summary>
        void AddScrimmage(Scrimmage scrimmage);

        /// <summary>
        /// Attempts to get a scrimmage by its ID.
        /// </summary>
        bool TryGetScrimmage(string scrimmageId, out Scrimmage? scrimmage);

        /// <summary>
        /// Removes a scrimmage from the collection.
        /// </summary>
        bool RemoveScrimmage(string scrimmageId);

        /// <summary>
        /// Gets scrimmages by their status.
        /// </summary>
        IEnumerable<Scrimmage> GetScrimmagesByStatus(ScrimmageStatus status);

        /// <summary>
        /// Gets scrimmages for a specific team.
        /// </summary>
        IEnumerable<Scrimmage> GetScrimmagesByTeam(string teamId);

        /// <summary>
        /// Gets scrimmages for a specific game size.
        /// </summary>
        IEnumerable<Scrimmage> GetScrimmagesByGameSize(GameSize gameSize);
    }
}