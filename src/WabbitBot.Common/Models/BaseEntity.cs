using System;
using WabbitBot.Common.Models.Interface;

namespace WabbitBot.Common.Models
{
    /// <summary>
    /// Base class for all entities in the system.
    /// Provides common properties that all entities must have.
    /// </summary>
    public abstract class BaseEntity : IBaseEntity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// When the entity was first created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the entity was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}