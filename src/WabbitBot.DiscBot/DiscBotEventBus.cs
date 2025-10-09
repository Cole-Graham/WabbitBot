using System.Collections.Concurrent;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.DiscBot;

/// <summary>
/// Implementation of the DiscBot event bus that handles events within the DiscBot project
/// and coordinates with the GlobalEventBus for cross-project communication.
/// </summary>
public class DiscBotEventBus(IGlobalEventBus globalEventBus) : IDiscBotEventBus
{
    private static DiscBotEventBus? _instance;
    private static readonly Lock _instanceLock = new();

    public static DiscBotEventBus Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    _instance ??= new DiscBotEventBus(GlobalEventBusProvider.GetGlobalEventBus());
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
            throw new InvalidOperationException("DiscBotEventBus must be initialized before publishing events");
        }

        ArgumentNullException.ThrowIfNull(@event);

        // Only treat explicit Response types as request completions
        if (@event.GetType().Name.EndsWith("Response"))
        {
            // Attempt to complete a pending request using the event's correlation ID
            if (_pendingRequests.TryRemove(@event.EventId, out var tcs))
            {
                tcs.SetResult(@event);
            }
        }

        // Route event based on its EventBusType property
        switch (@event.EventBusType)
        {
            case EventBusType.Global:
                // Publish to Global bus for cross-boundary communication
                await _globalEventBus.PublishAsync(@event);
                break;

            case EventBusType.DiscBot:
                // Publish locally within DiscBot
                await PublishLocallyAsync(@event);
                break;

            default:
                throw new InvalidOperationException($"Unsupported EventBusType: {@event.EventBusType}");
        }
    }

    /// <inheritdoc />
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = [];
                _handlers[eventType] = handlers;
            }
            handlers.Add(handler);
        }
    }

    /// <inheritdoc />
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class, IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
    }

    /// <inheritdoc />
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class, IEvent
        where TResponse : class, IEvent
    {
        ArgumentNullException.ThrowIfNull(request);

        var tcs = new TaskCompletionSource<object?>();
        _pendingRequests[request.EventId] = tcs;

        try
        {
            await PublishAsync(request);

            // Wait for response with timeout
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _pendingRequests.TryRemove(request.EventId, out _);
                return null;
            }

            var result = await tcs.Task;
            return result as TResponse;
        }
        catch
        {
            _pendingRequests.TryRemove(request.EventId, out _);
            throw;
        }
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return Task.CompletedTask;
        }

        // Subscribe to Global bus events that should be forwarded to DiscBot
        _globalEventBus.Subscribe<IEvent>(async evt =>
        {
            // Only handle events targeting Global bus from other boundaries
            if (evt.EventBusType == EventBusType.Global)
            {
                await PublishLocallyAsync(evt);
            }
        });

        _isInitialized = true;
        return Task.CompletedTask;
    }

    private async ValueTask PublishLocallyAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        var eventType = @event.GetType();
        List<Delegate>? handlersCopy = null;

        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlersCopy = [.. handlers,];
            }
        }

        if (handlersCopy is not null)
        {
            foreach (var handler in handlersCopy)
            {
                if (handler is Func<TEvent, Task> typedHandler)
                {
                    await typedHandler(@event);
                }
            }
        }
    }
}

