using System;

namespace WabbitBot.Common.Models
{
    public interface ITeamEntity { }
    public interface ILeaderboardEntity { }
    public interface IMatchEntity { }
    public interface IScrimmageEntity { }
    public interface ITournamentEntity { }
    public interface IPlayerEntity { }
    public interface IUserEntity { }
    public interface IMapEntity { }
    public interface IDivisionEntity { }

    // Domain definition for entities, if you add a new domain, you must add it to the
    // WabbitBot.SourceGenerators.Attributes definition as well.
    public enum Domain
    {
        Common,
        Leaderboard,
        Scrimmage,
        Tournament
    }

    /// <summary>
    /// Base class for all entities in the system.
    /// Provides common properties that all entities must have, including audit tracking.
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// User who created this entity.
        /// Null for system-generated entities (e.g., maps loaded from configuration).
        /// </summary>
        public Guid? CreatedByUserId { get; set; }

        /// <summary>
        /// When the entity was first created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// User who last updated this entity.
        /// Null for system-generated updates (e.g., automated calculations, scheduled jobs).
        /// </summary>
        public Guid? UpdatedByUserId { get; set; }

        /// <summary>
        /// When the entity was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The domain to which this entity belongs.
        /// </summary>
        public abstract Domain Domain { get; } // Force entities to declare their domain
    }
}