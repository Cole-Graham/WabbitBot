namespace WabbitBot.Common.Events;

public interface IGlobalEventBus
{
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : class;
    void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;
}

public class GlobalEventBus : IGlobalEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();

    public Task PublishAsync<TEvent>(TEvent @event) where TEvent : class
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);
        List<Delegate>? handlers;

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out handlers))
                return Task.CompletedTask;
        }

        var tasks = handlers.Select(h => (Task)h.DynamicInvoke(@event)!);
        return Task.WhenAll(tasks);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (!_handlers.ContainsKey(eventType))
                _handlers[eventType] = new List<Delegate>();

            _handlers[eventType].Add(handler);
        }
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (_handlers.ContainsKey(eventType))
                _handlers[eventType].Remove(handler);
        }
    }
}