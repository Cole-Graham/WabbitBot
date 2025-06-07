using System;

namespace WabbitBot.Common.Models
{
    /// <summary>
    /// Base class for entities that require version tracking.
    /// Extends BaseEntity to add version information.
    /// </summary>
    public abstract class VersionedEntity : BaseEntity
    {
        /// <summary>
        /// The current version of the entity.
        /// Incremented each time the entity is modified.
        /// </summary>
        public int Version { get; set; }
    }
}