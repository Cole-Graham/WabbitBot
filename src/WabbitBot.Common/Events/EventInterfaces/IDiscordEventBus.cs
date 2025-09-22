namespace WabbitBot.Common.Events.EventInterfaces;

/// <summary>
/// Interface for Discord-specific event bus that handles internal Discord events
/// and coordinates with the GlobalEventBus for cross-project communication.
/// </summary>
public interface IDiscordEventBus
{
    /// <summary>
    /// Publishes an event to Discord event handlers and forwards to GlobalEventBus if it's a global event
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;

    /// <summary>
    /// Subscribes to Discord events
    /// </summary>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;

    /// <summary>
    /// Unsubscribes from Discord events
    /// </summary>
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;

    /// <summary>
    /// Initializes the Discord event bus
    /// </summary>
    Task InitializeAsync();
}
