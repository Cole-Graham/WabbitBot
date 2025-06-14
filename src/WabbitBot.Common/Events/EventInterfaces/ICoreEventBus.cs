namespace WabbitBot.Common.Events.EventInterfaces;

/// <summary>
/// Interface for the Core event bus that handles events within the Core project
/// and coordinates with the GlobalEventBus for cross-project communication.
/// </summary>
public interface ICoreEventBus
{
    /// <summary>
    /// Publishes an event to all subscribers within Core and optionally to the GlobalEventBus.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish</typeparam>
    /// <param name="event">The event instance to publish</param>
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;

    /// <summary>
    /// Subscribes a handler to a specific type of event within Core.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to</typeparam>
    /// <param name="handler">The handler function to be called when the event occurs</param>
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;

    /// <summary>
    /// Unsubscribes a handler from a specific type of event within Core.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to unsubscribe from</typeparam>
    /// <param name="handler">The handler function to remove</param>
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;

    /// <summary>
    /// Makes a request and waits for a response through the event bus.
    /// This is used for request-response patterns where a response is expected.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to send</typeparam>
    /// <typeparam name="TResponse">The type of response expected</typeparam>
    /// <param name="request">The request instance to send</param>
    /// <returns>A task that completes with the response when received</returns>
    Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Initializes the Core event bus and sets up its connection to the GlobalEventBus.
    /// </summary>
    Task InitializeAsync();
}
