using System.Collections.Concurrent;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.Core;
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
    public void Subscribe<TEvent>(Func<TEvent, Task> handler, HandlerType type = HandlerType.Write)
        where TEvent : class, IEvent
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
            handlers.Add(new EventHandlerMetadata { Handler = handler, Type = type });
        }
    }

    /// <inheritdoc />
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : class, IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                var toRemove = handlers.FirstOrDefault(m => m.Handler.Equals(handler));
                if (toRemove is not null)
                {
                    handlers.Remove(toRemove);
                }
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

        // Subscribe to specific Global bus events that should be forwarded to DiscBot
        // Note: We need to subscribe to specific event types, not the generic IEvent interface
        _globalEventBus.Subscribe<ChallengeCreated>(async evt =>
        {
            Console.WriteLine($"🔍 DEBUG: DiscBotEventBus received ChallengeCreated event");
            Console.WriteLine($"   EventBusType: {evt.EventBusType}");
            Console.WriteLine($"   EventId: {evt.EventId}");
            Console.WriteLine($"   ChallengeId: {evt.ChallengeId}");

            // Only handle events targeting Global bus from other boundaries
            if (evt.EventBusType == EventBusType.Global)
            {
                Console.WriteLine($"   Forwarding to local handlers...");
                await PublishLocallyAsync(evt);
            }
            else
            {
                Console.WriteLine($"   Ignoring non-Global event");
            }
        });

        _isInitialized = true;
        return Task.CompletedTask;
    }

    private async ValueTask PublishLocallyAsync<TEvent>(TEvent @event)
        where TEvent : class, IEvent
    {
        var eventType = @event.GetType();
        List<EventHandlerMetadata>? handlersCopy = null;

        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlersCopy = [.. handlers];
            }
        }

        if (handlersCopy is not null)
        {
            // Phase 1: Execute all Write handlers
            var writeHandlers = handlersCopy.Where(m => m.Type == HandlerType.Write).ToList();
            if (writeHandlers.Count > 0)
            {
                var writeTasks = new List<Task>();
                foreach (var metadata in writeHandlers)
                {
                    if (metadata.Handler is Func<TEvent, Task> typedHandler)
                    {
                        writeTasks.Add(typedHandler(@event));
                    }
                }
                await Task.WhenAll(writeTasks);
            }

            // Phase 2: Execute all Read handlers
            var readHandlers = handlersCopy.Where(m => m.Type == HandlerType.Read).ToList();
            if (readHandlers.Count > 0)
            {
                var readTasks = new List<Task>();
                foreach (var metadata in readHandlers)
                {
                    if (metadata.Handler is Func<TEvent, Task> typedHandler)
                    {
                        readTasks.Add(typedHandler(@event));
                    }
                }
                await Task.WhenAll(readTasks);
            }
        }
    }
}
