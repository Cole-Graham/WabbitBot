using WabbitBot.Common.Configuration;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Common.Events
{
    #region Startup Sequence Events
    /// <summary>
    /// Events for the global startup sequence. These events coordinate startup across all projects.
    /// They are part of the infrastructure layer and therefore live in the Common project.
    /// </summary>
    public record StartupInitiatedEvent(
    BotOptions Configuration,
    IBotConfigurationService ConfigurationService,
    EventBusType EventBusType = EventBusType.Global) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record SystemReadyEvent(
    EventBusType EventBusType = EventBusType.Global) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record ApplicationReadyEvent(
    EventBusType EventBusType = EventBusType.Global) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
    #endregion

    #region Shutdown Events
    public record ApplicationShuttingDownEvent(
    EventBusType EventBusType = EventBusType.Global) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    #endregion

    #region Error Events
    public record GlobalErrorHandlingReadyEvent(
    EventBusType EventBusType = EventBusType.Global) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record CriticalStartupErrorEvent(
    Exception Exception,
    string Component = "Unknown",
    EventBusType EventBusType = EventBusType.Global) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record BoundaryErrorEvent(
    Exception Exception,
    string Boundary,
    EventBusType EventBusType = EventBusType.Global) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
    #endregion
}