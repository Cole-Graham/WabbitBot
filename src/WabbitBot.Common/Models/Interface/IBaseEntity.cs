using System;

namespace WabbitBot.Common.Models.Interface
{
    /// <summary>
    /// Defines the contract for all entities in the system.
    /// This interface provides the core properties that all entities must have.
    /// </summary>
    public interface IBaseEntity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        Guid Id { get; set; }

        /// <summary>
        /// When the entity was first created
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the entity was last updated
        /// </summary>
        DateTime UpdatedAt { get; set; }
    }
}