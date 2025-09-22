using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.BotCore;

/// <summary>
/// Implementation of the Core event bus that handles events within the Core project
/// and coordinates with the GlobalEventBus for cross-project communication.
/// </summary>
public class CoreEventBus : ICoreEventBus
{
    private static CoreEventBus? _instance;
    private static readonly object _instanceLock = new();

    public static CoreEventBus Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    _instance ??= new CoreEventBus(GlobalEventBusProvider.GetGlobalEventBus());
                }
            }
            return _instance;
        }
    }

    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly Dictionary<Type, List<Delegate>> _requestHandlers = new();
    private readonly object _lock = new();
    private readonly IGlobalEventBus _globalEventBus;
    private bool _isInitialized;

    public CoreEventBus(IGlobalEventBus globalEventBus)
    {
        _globalEventBus = globalEventBus ?? throw new ArgumentNullException(nameof(globalEventBus));
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before publishing events");
        }

        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        var eventType = typeof(TEvent);
        List<Delegate>? handlers;

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out handlers))
            {
                // If no local handlers, forward to global bus only
                handlers = new List<Delegate>();
            }
            else
            {
                // Make a copy to avoid holding the lock during invocation
                handlers = new List<Delegate>(handlers);
            }
        }

        var tasks = new List<Task>();

        // Execute all local handlers
        foreach (var handler in handlers)
        {
            if (handler is Func<TEvent, Task> typedHandler)
            {
                tasks.Add(typedHandler(@event));
            }
        }

        // Only forward to global bus if the event is meant for global routing
        if (@event is IEvent eventWithType && eventWithType.EventBusType == EventBusType.Global)
        {
            tasks.Add(_globalEventBus.PublishAsync(@event));
        }

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before subscribing to events");
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<Delegate>();
            }

            _handlers[eventType].Add(handler);
        }
    }

    /// <inheritdoc />
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before unsubscribing from events");
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (_handlers.ContainsKey(eventType))
            {
                _handlers[eventType].Remove(handler);

                // Remove the event type if no handlers remain
                if (_handlers[eventType].Count == 0)
                {
                    _handlers.Remove(eventType);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class
        where TResponse : class
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before making requests");
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requestType = typeof(TRequest);
        List<Delegate>? handlers;

        lock (_lock)
        {
            if (!_requestHandlers.TryGetValue(requestType, out handlers) || handlers.Count == 0)
            {
                return null; // No handlers registered for this request type
            }
        }

        // Create a TaskCompletionSource to wait for the response
        var tcs = new TaskCompletionSource<TResponse?>();

        // Execute all handlers and take the first non-null response
        var tasks = handlers.Select(async handler =>
        {
            if (handler is Func<TRequest, Task<TResponse?>> typedHandler)
            {
                try
                {
                    var response = await typedHandler(request);
                    if (response != null)
                    {
                        tcs.TrySetResult(response);
                    }
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }
        }).ToList();

        // Wait for either a response or all handlers to complete
        await Task.WhenAll(tasks);

        // If no response was set, return null
        if (!tcs.Task.IsCompleted)
        {
            tcs.TrySetResult(null);
        }

        return await tcs.Task;
    }

    /// <summary>
    /// Subscribes a handler for request-response patterns.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to handle</typeparam>
    /// <typeparam name="TResponse">The type of response to return</typeparam>
    /// <param name="handler">The handler function that processes the request and returns a response</param>
    public void SubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse?>> handler)
        where TRequest : class
        where TResponse : class
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before subscribing to requests");
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var requestType = typeof(TRequest);
        lock (_lock)
        {
            if (!_requestHandlers.ContainsKey(requestType))
            {
                _requestHandlers[requestType] = new List<Delegate>();
            }

            _requestHandlers[requestType].Add(handler);
        }
    }

    /// <summary>
    /// Unsubscribes a handler from request-response patterns.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to stop handling</typeparam>
    /// <typeparam name="TResponse">The type of response that was being returned</typeparam>
    /// <param name="handler">The handler function to remove</param>
    public void UnsubscribeRequest<TRequest, TResponse>(Func<TRequest, Task<TResponse?>> handler)
        where TRequest : class
        where TResponse : class
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before unsubscribing from requests");
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var requestType = typeof(TRequest);
        lock (_lock)
        {
            if (_requestHandlers.ContainsKey(requestType))
            {
                _requestHandlers[requestType].Remove(handler);

                // Remove the request type if no handlers remain
                if (_requestHandlers[requestType].Count == 0)
                {
                    _requestHandlers.Remove(requestType);
                }
            }
        }
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return Task.CompletedTask;
        }

        // Any initialization logic here (e.g., setting up default handlers)
        _isInitialized = true;

        return Task.CompletedTask;
    }
}
