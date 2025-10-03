using System.Collections.Concurrent;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.BotCore;

/// <summary>
/// Implementation of the Core event bus that handles events within the Core project
/// and coordinates with the GlobalEventBus for cross-project communication.
/// </summary>
public class CoreEventBus(IGlobalEventBus globalEventBus) : ICoreEventBus
{
    private static CoreEventBus? _instance;
    private static readonly Lock _instanceLock = new();

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

    private readonly Dictionary<Type, List<Delegate>> _handlers = [];
    private readonly Dictionary<Type, List<Delegate>> _requestHandlers = [];
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object?>> _pendingRequests = new();
    private readonly Lock _lock = new();
    private readonly IGlobalEventBus _globalEventBus = globalEventBus ?? throw new ArgumentNullException(nameof(globalEventBus));
    private bool _isInitialized;

    /// <inheritdoc />
    public async ValueTask PublishAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before publishing events");
        }

        ArgumentNullException.ThrowIfNull(@event);

        // Only treat explicit Response types as request completions
        var evtType = @event.GetType();
        if (evtType.Name.EndsWith("Response", StringComparison.Ordinal))
        {
            if (_pendingRequests.TryRemove(@event.EventId, out var tcs))
            {
                tcs.TrySetResult(@event);
                return; // Don't process further for responses
            }
        }

        var eventType = @event.GetType();
        List<Delegate>? handlers;

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out handlers))
            {
                // If no local handlers, forward to global bus only
                handlers = [];
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
            tasks.Add((Task)handler.DynamicInvoke(@event)!);
        }

        // Only forward to global bus if the event is meant for global routing
        if (@event.EventBusType == EventBusType.Global)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var evtType2 = @event.GetType();
                    var method = typeof(IGlobalEventBus).GetMethod("PublishAsync")!;
                    var generic = method.MakeGenericMethod(evtType2);
                    var task = (Task)generic.Invoke(_globalEventBus, new object[] { @event })!;
                    await task;
                }
                catch (Exception ex)
                {
                    var errorEvent = new BoundaryErrorEvent(ex, "Core-to-Global", EventBusType.Global);
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IEvent
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before subscribing to events");
        }

        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out List<Delegate>? handlers))
            {
                handlers = new List<Delegate>();
                _handlers[eventType] = handlers;
            }

            handlers.Add(handler);
        }
    }

    /// <inheritdoc />
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IEvent
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before unsubscribing from events");
        }

        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out List<Delegate>? handlers))
            {
                handlers.Remove(handler);

                // Remove the event type if no handlers remain
                if (handlers.Count == 0)
                {
                    _handlers.Remove(eventType);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class, IEvent
        where TResponse : class, IEvent
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before making requests");
        }

        ArgumentNullException.ThrowIfNull(request);

        var requestId = request.EventId;
        var tcs = new TaskCompletionSource<object?>();
        _pendingRequests[requestId] = tcs;

        try
        {
            // Publish the request
            await PublishAsync(request);

            // Wait for response with timeout
            var responseTask = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
            return responseTask as TResponse;
        }
        catch (TimeoutException)
        {
            return null;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
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
