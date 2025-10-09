using System;
using WabbitBot.Generator.Shared.Analyzers;

namespace WabbitBot.SourceGenerators.Attributes
{
    #region Event Generator
    /// <summary>
    /// Marks an event class for automatic publisher and subscriber generation.
    /// Applied to event record/class definitions to generate supporting infrastructure.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EventGeneratorAttribute(string? pubTargetClass = null, string[]? subTargetClasses = null) : Attribute
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
