using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Attributes;
using WabbitBot.DiscBot.DiscBot.Base;

namespace WabbitBot.DiscBot.DiscBot.Events;

/// <summary>
/// Discord-specific event bus that handles internal Discord events and forwards to GlobalEventBus
/// </summary>
[GenerateEventHandler(EventBusType = EventBusType.DiscBot, EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
public partial class DiscordEventBus : IDiscordEventBus
{
    private static DiscordEventBus? _instance;
    private static readonly object _instanceLock = new();

    public static DiscordEventBus Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    _instance ??= new DiscordEventBus(GlobalEventBusProvider.GetGlobalEventBus());
                }
            }
            return _instance;
        }
    }

    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();
    private readonly IGlobalEventBus _globalEventBus;
    private bool _isInitialized;

    public DiscordEventBus(IGlobalEventBus globalEventBus)
    {
        _globalEventBus = globalEventBus ?? throw new ArgumentNullException(nameof(globalEventBus));
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("DiscordEventBus must be initialized before publishing events");
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
        // Discord-internal events (EventBusType.DiscBot) stay on DiscordEventBus only
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
            throw new InvalidOperationException("DiscordEventBus must be initialized before subscribing to events");
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
            throw new InvalidOperationException("DiscordEventBus must be initialized before unsubscribing from events");
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
            }
        }
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        _isInitialized = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Forwards events to the global event bus for cross-project communication
    /// </summary>
    public async Task PublishToGlobalAsync<T>(T @event) where T : class
    {
        await _globalEventBus.PublishAsync(@event);
    }

    /// <summary>
    /// Register DSharpPlus Discord API event handlers and convert them to business events
    /// This is where DSharpPlus events (message received, user joined, etc.) are handled
    /// and converted to business events that get forwarded to the GlobalEventBus
    /// </summary>
    public void RegisterDSharpPlusEventHandlers(global::DSharpPlus.DiscordClient client)
    {
        // TODO: Add DSharpPlus event handlers after confirming DSharpPlus 5.0 API
        // Examples:
        // client.MessageCreated += async (sender, args) => 
        // {
        //     await PublishToGlobalAsync(new MessageReceivedEvent(args.Message));
        // };
    }
}
