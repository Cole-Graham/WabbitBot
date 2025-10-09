using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace WabbitBot.Generator.Shared.Metadata
{
    /// <summary>
    /// Information extracted from an [EventGenerator] attribute on an event class.
    /// </summary>
    public record EventInfo(
        string EventClassName,
        string EventNamespace,
        string? PubTargetClass,
        List<string> SubTargetClasses,
        List<EventParameter> Parameters,
        INamedTypeSymbol EventClassSymbol
    )
    {
        /// <summary>
        /// Whether to generate a publisher method.
        /// </summary>
        public bool ShouldGeneratePublisher => !string.IsNullOrEmpty(PubTargetClass);

        /// <summary>
        /// Whether to generate subscriber registrations.
        /// </summary>
        public bool ShouldGenerateSubscribers => SubTargetClasses?.Any() ?? false;
    }

    /// <summary>
    /// Represents a parameter in an event record.
    /// </summary>
    public record EventParameter(string Name, string Type);
}
