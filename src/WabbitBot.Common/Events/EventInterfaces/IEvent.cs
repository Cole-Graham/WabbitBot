namespace WabbitBot.Common.Events.EventInterfaces;

/// <summary>
/// Defines the available event bus types in the system.
/// </summary>
public enum EventBusType
{
    /// <summary>
    /// Global event bus for cross-boundary communication.
    /// </summary>
    Global,

    /// <summary>
    /// Core event bus for internal business logic events.
    /// </summary>
    Core,

    /// <summary>
    /// Discord event bus for Discord-specific events.
    /// </summary>
    DiscBot
}

/// <summary>
/// Base interface for all events in the system.
/// Events must specify their EventBusType for proper routing.
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
    string EventId { get; }

    /// <summary>
    /// Timestamp when the event was created.
    /// </summary>
    DateTime Timestamp { get; }
}
