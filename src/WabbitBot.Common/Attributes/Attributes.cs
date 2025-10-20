using System;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Attributes
{
    /// <summary>
    /// Defines the target projects for event generation.
    /// </summary>
    [Flags]
    public enum GenerationTargets
    {
        /// <summary>
        /// Publish to the Common project.
        /// </summary>
        Common = 1,

        /// <summary>
        /// Publish to the Core project.
        /// </summary>
        Core = 2,

        /// <summary>
        /// Publish to the DiscBot project.
        /// </summary>
        DiscBot = 3,
    }

    /// <summary>
    /// Defines the target event buses for event publishing.
    /// Used with EventTrigger to specify where events should be published.
    /// </summary>
    [Flags]
    public enum EventTargets
    {
        /// <summary>
        /// Publish to the local/default event bus only.
        /// </summary>
        Local = 1,

        /// <summary>
        /// Publish to the Global event bus only.
        /// </summary>
        Global = 2,

        /// <summary>
        /// Publish to both local and Global event buses (dual-publish).
        /// </summary>
        Both = Local | Global,
    }

    #region Event Generator
    /// <summary>
    /// Marks an event class for automatic publisher and subscriber generation.
    /// Applied to event record/class definitions to generate supporting infrastructure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EventGeneratorAttribute(
        string? pubTargetClass = null,
        string[]? subTargetClasses = null,
        string[]? writeHandlers = null
    ) : Attribute
    {
        /// <summary>
        /// The fully qualified class name where the publisher method should be generated.
        /// Example: "WabbitBot.Core.Scrimmages.ScrimmageCore"
        /// </summary>
        public string? PubTargetClass { get; } = pubTargetClass;

        /// <summary>
        /// Array of fully qualified class names where subscriber registrations should be generated.
        /// Example: ["WabbitBot.DiscBot.App.Handlers.ScrimmageHandler", "WabbitBot.Core.Leaderboards.LeaderboardCore"]
        /// </summary>
        public string[]? SubTargetClasses { get; } = subTargetClasses;

        /// <summary>
        /// Array of fully qualified class names that should be treated as Write handlers (mutate state).
        /// Handlers not in this list will be treated as Read handlers (read state).
        /// Example: ["WabbitBot.Core.Common.Models.Common.GameHandler"]
        /// </summary>
        public string[]? WriteHandlers { get; } = writeHandlers;
    }
    #endregion

    #region Commands
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class WabbitCommandAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
        public string? Group { get; set; }
    }
    #endregion

    #region Suppression
    /// <summary>
    /// Suppresses event generation for a specific declaration (e.g., manual override).
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class SuppressGenerationAttribute(string? reason = null) : Attribute
    {
        public string? Reason { get; } = reason; // Optional for docs
    }
    #endregion
}
