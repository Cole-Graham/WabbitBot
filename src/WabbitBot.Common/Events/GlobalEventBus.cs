using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Events;

public interface IGlobalEventBus
{
    Task PublishAsync<TEvent>(TEvent @event)
        where TEvent : class;
    void Subscribe<TEvent>(Func<TEvent, Task> handler, HandlerType type = HandlerType.Write)
        where TEvent : class;
    void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : class;

    /// <summary>
    /// Sends a request and waits for a response event.
    /// Used for cross-boundary queries where direct method calls aren't possible.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to send</typeparam>
    /// <typeparam name="TResponse">The type of response expected</typeparam>
    /// <param name="request">The request instance to send</param>
    /// <param name="timeout">Maximum time to wait for a response (default: 5 seconds)</param>
    /// <returns>A task that completes with the response when received, or null if timeout</returns>
    Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null)
        where TRequest : class
        where TResponse : class;
}

public class GlobalEventBus : IGlobalEventBus
{
    private readonly Dictionary<Type, List<EventHandlerMetadata>> _handlers = new();
    private readonly Dictionary<Guid, TaskCompletionSource<object>> _pendingRequests = new();
    private readonly object _lock = new();

    public async Task PublishAsync<TEvent>(TEvent @event)
        where TEvent : class
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);
        List<EventHandlerMetadata>? handlers;

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out handlers))
                return;
        }

        // Phase 1: Execute all Write handlers and await completion
        var writeHandlers = handlers.Where(h => h.Type == HandlerType.Write).ToList();
        if (writeHandlers.Count > 0)
        {
            var writeTasks = writeHandlers.Select(h => (Task)h.Handler.DynamicInvoke(@event)!);
            await Task.WhenAll(writeTasks);
        }

        // Phase 2: Execute all Read handlers after Write handlers complete
        var readHandlers = handlers.Where(h => h.Type == HandlerType.Read).ToList();
        if (readHandlers.Count > 0)
        {
            var readTasks = readHandlers.Select(h => (Task)h.Handler.DynamicInvoke(@event)!);
            await Task.WhenAll(readTasks);
        }
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler, HandlerType type = HandlerType.Write)
        where TEvent : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (!_handlers.ContainsKey(eventType))
                _handlers[eventType] = new List<EventHandlerMetadata>();

            _handlers[eventType].Add(new EventHandlerMetadata { Handler = handler, Type = type });
        }
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        lock (_lock)
        {
            if (_handlers.ContainsKey(eventType))
            {
                var toRemove = _handlers[eventType].FirstOrDefault(m => m.Handler.Equals(handler));
                if (toRemove is not null)
                {
                    _handlers[eventType].Remove(toRemove);
                }
            }
        }
    }

    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request, TimeSpan? timeout = null)
        where TRequest : class
        where TResponse : class
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        timeout ??= TimeSpan.FromSeconds(5);
        var requestId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<object>();

        // Store the pending request
        lock (_lock)
        {
            _pendingRequests[requestId] = tcs;
        }

        // Subscribe to the response type temporarily
        TaskCompletionSource<object>? responseTcs = null;
        Func<TResponse, Task> responseHandler = async response =>
        {
            // Check if this response correlates to our request
            // Note: Responses should include a CorrelationId that matches the request
            if (TryGetCorrelationId(response, out var correlationId) && correlationId == requestId)
            {
                lock (_lock)
                {
                    if (_pendingRequests.TryGetValue(requestId, out responseTcs))
                    {
                        _pendingRequests.Remove(requestId);
                        responseTcs.TrySetResult(response);
                    }
                }
            }
            await Task.CompletedTask;
        };

        Subscribe(responseHandler);

        try
        {
            // Set the correlation ID on the request if possible
            SetCorrelationId(request, requestId);

            // Publish the request
            await PublishAsync(request);

            // Wait for the response with timeout
            using var cts = new CancellationTokenSource(timeout.Value);
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout.Value, cts.Token));

            if (completedTask == tcs.Task)
            {
                return tcs.Task.Result as TResponse;
            }

            // Timeout occurred
            lock (_lock)
            {
                _pendingRequests.Remove(requestId);
            }
            return null;
        }
        finally
        {
            Unsubscribe(responseHandler);
        }
    }

    private static bool TryGetCorrelationId(object obj, out Guid correlationId)
    {
        correlationId = Guid.Empty;
        var prop = obj.GetType().GetProperty("CorrelationId");
        if (prop?.GetValue(obj) is Guid id)
        {
            correlationId = id;
            return true;
        }
        return false;
    }

    private static void SetCorrelationId(object obj, Guid correlationId)
    {
        var prop = obj.GetType().GetProperty("RequestId");
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(obj, correlationId);
        }
    }
}
