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
    /// Provides common properties that all entities must have.
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// When the entity was first created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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