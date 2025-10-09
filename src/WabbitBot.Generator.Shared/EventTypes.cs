namespace WabbitBot.Generator.Shared;

/// <summary>
/// Types needed for event generation that are shared between generators.
/// These are duplicated to avoid circular dependencies.
/// </summary>
/// <summary>
/// Event bus types for routing events.
/// </summary>
public enum EventBusType
{
    /// <summary>
    /// Global event bus for cross-boundary events.
    /// </summary>
    Global,

    /// <summary>
    /// Core event bus for business logic events.
    /// </summary>
    Core,

    /// <summary>
    /// Discord event bus for UI/interaction events.
    /// </summary>
    DiscBot,
}

/// <summary>
/// Base interface for all events.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// The event bus type this event should be routed to.
    /// This replaces the need for marker interfaces like ICoreEvent, IGlobalEvent, etc.
    /// </summary>
    EventBusType EventBusType { get; }

    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    DateTime Timestamp { get; }
}
