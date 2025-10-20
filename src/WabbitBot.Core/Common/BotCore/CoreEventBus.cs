using System.Collections.Concurrent;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.Interfaces;

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

    private readonly Dictionary<Type, List<EventHandlerMetadata>> _handlers = [];
    private readonly Dictionary<Type, List<Delegate>> _requestHandlers = [];
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<object?>> _pendingRequests = new();
    private readonly Lock _lock = new();
    private readonly IGlobalEventBus _globalEventBus =
        globalEventBus ?? throw new ArgumentNullException(nameof(globalEventBus));
    private bool _isInitialized;

    /// <inheritdoc />
    public async ValueTask PublishAsync<TEvent>(TEvent @event)
        where TEvent : class, IEvent
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
        List<EventHandlerMetadata>? handlers;

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
                handlers = new List<EventHandlerMetadata>(handlers);
            }
        }

        // Phase 1: Execute all local Write handlers
        var writeHandlers = handlers.Where(h => h.Type == HandlerType.Write).ToList();
        if (writeHandlers.Count > 0)
        {
            var writeTasks = writeHandlers.Select(h => (Task)h.Handler.DynamicInvoke(@event)!);
            await Task.WhenAll(writeTasks);
        }

        // Phase 2: Execute all local Read handlers
        var readHandlers = handlers.Where(h => h.Type == HandlerType.Read).ToList();
        if (readHandlers.Count > 0)
        {
            var readTasks = readHandlers.Select(h => (Task)h.Handler.DynamicInvoke(@event)!);
            await Task.WhenAll(readTasks);
        }

        // Forward to global bus if the event is meant for global routing
        // This happens after local handlers to maintain Write/Read ordering across boundaries
        if (@event.EventBusType == EventBusType.Global)
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
        }
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(Func<TEvent, Task> handler, HandlerType type = HandlerType.Write)
        where TEvent : class, IEvent
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before subscribing to events");
        }

        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out List<EventHandlerMetadata>? handlers))
            {
                handlers = new List<EventHandlerMetadata>();
                _handlers[eventType] = handlers;
            }

            handlers.Add(new EventHandlerMetadata { Handler = handler, Type = type });
        }
    }

    /// <inheritdoc />
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : class, IEvent
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("CoreEventBus must be initialized before unsubscribing from events");
        }

        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out List<EventHandlerMetadata>? handlers))
            {
                var toRemove = handlers.FirstOrDefault(m => m.Handler.Equals(handler));
                if (toRemove is not null)
                {
                    handlers.Remove(toRemove);
                }

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
