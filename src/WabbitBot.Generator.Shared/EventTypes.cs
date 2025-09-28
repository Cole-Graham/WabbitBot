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
    /// Core event bus for business logic events.
    /// </summary>
    Core,

    /// <summary>
    /// Discord event bus for UI/interaction events.
    /// </summary>
    DiscBot,

    /// <summary>
    /// Global event bus for cross-boundary events.
    /// </summary>
    Global
}

/// <summary>
/// Base interface for all events.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// The event bus type this event should be routed to.
    /// </summary>
    EventBusType EventBusType { get; }
}
