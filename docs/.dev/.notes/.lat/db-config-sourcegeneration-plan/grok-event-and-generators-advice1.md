##### grok-4-fast (beta):

### Foundation level advice
### Adjusted Recommendations for Cross-Boundary Event System

Thanks for the clarification—this shifts the focus from intra-type notifications (like property changes) to inter-module signaling, which is perfect for achieving loose coupling in a modular architecture. Without CRUD or DB ties, we can emphasize **pub-sub patterns** for boundaries like UI-to-business logic, service-to-service, or layer-to-layer communication. This aligns well with your taxonomy: use reference types (classes, records) as event payloads for rich data, delegates for handlers, and generation to automate publisher boilerplate across declarations.

Core principles for this refactor:
- **Decoupling first**: Events as contracts—publishers don't know subscribers. Use interfaces for event definitions to enforce boundaries.
- **Event Sourcing Lite**: Treat events as immutable facts for propagation, using record structs/classes for payloads.
- **Scalability**: Favor async, fire-and-forget invocation to avoid blocking boundaries.
- **Generator Role**: Auto-generate event raisers and handlers for attributed boundaries, keeping the bus manual for orchestration.

I'll refine the previous advice: updated event kinds, generation split, architecture sketch, and generator tips.

### 1. Recommended Kinds of Events

For cross-boundary use, prioritize **integration events** (external signals) over domain events (internal state). Map to delegate subtypes for type safety—e.g., Func for async responses, Action for pure signals. Avoid predicates unless filtering at boundaries (rare here).

| Event Kind | Delegate Type (from Taxonomy) | Purpose & Use Case | When to Use in Your Project | Example Signature |
|------------|-------------------------------|--------------------|-----------------------------|-------------------|
| **Integration Signal** | Custom Delegate or EventHandler<TEventArgs> | Immutable facts crossing modules (e.g., "UserRegistered" from auth to analytics). Typed args carry context without refs. | Core for boundaries: publish from one module, subscribe in another. | `event EventHandler<UserRegisteredEvent> UserRegistered;` |
| **Async Request-Response** | Func<TInput, Task<TOutput>> | Cross-boundary queries with replies (e.g., validate payload before propagation). | When boundaries need coordination, like UI requesting business rules. | `event Func<Payload, Task<bool>> ValidatePayload;` |
| **Void Propagation** | Action<TEventArgs> | Fire-and-forget broadcasts (e.g., "CacheInvalidated" to multiple services). Efficient for one-way comms. | High-volume signals across layers, no replies needed. | `event Action<CacheInvalidatedEvent> CacheInvalidated;` |
| **Filtered Broadcast** | Predicate<T> chained with Action | Conditional routing (e.g., only notify if event matches subscriber criteria). | Edge cases like role-based boundary access. | `event Action<T> NotifyIf(Predicate<T> filter, T args);` |
| **Lifecycle Hook** | Func<Task> | Async entry/exit points for boundaries (e.g., "BoundaryEntered" for tracing). | Manual for framework-level cross-cuts like logging/telemetry. | `event Func<BoundaryContext, Task> OnBoundaryCrossed;` |

- **Why these?** They promote one-way, async flows ideal for boundaries—e.g., `EventHandler<T>` for multicast without tight coupling. Use record classes for event payloads (reference semantics for sharing) or structs for lightweight ones. Skip value-returning unless responses are critical (keep boundaries async to scale).

### 2. Generated vs. Manually Defined Events

With cross-boundary focus, generate per-boundary emitters (e.g., for attributed classes/interfaces) to standardize publishing. Manual for the central bus/routing to allow custom middleware (e.g., serialization, retries).

| Category | Generated (via Source Generator) | Manually Defined (in Core Code) | Rationale |
|----------|----------------------------------|---------------------------------|-----------|
| **Boundary Signals** | Integration events for module exports (e.g., auto-raise "OrderPlaced" from business layer classes). Handler stubs for imports. | N/A | Patterns repeat across declarations; generation ensures consistent payloads from taxonomy types (e.g., records as args). |
| **Async Flows** | Func-based raisers for attributed methods (e.g., [CrossBoundary] on interface methods). | Central async dispatcher (e.g., with cancellation tokens). | Automate per-type wiring; manual for global queuing/resilience. |
| **Broadcasts** | Action emitters for enum/struct-based flags crossing boundaries. | Pub-sub bus (e.g., in-memory or message queue integration). | Generation for declarative boundaries; manual for topology (e.g., topic routing). |
| **Conditional** | Predicate wrappers around signals if attributes specify filters. | Custom middleware for dynamic routing. | Rare generation for simple cases; manual for complex policies. |
| **Framework Hooks** | N/A | Lifecycle events like "ModuleLoaded" for bootstrap. | Purely manual to control app-wide boundaries. |

- **Generation Trigger**: Use attributes like [PublishEvent("TopicName")] on declarations—generator scans and emits publishers. Threshold: Generate if boundary-crossing is implied (e.g., public interfaces).
- **Manual Flexibility**: Expose `[SuppressGeneration]` to opt out, and provide extension points for custom subscribers.

### 3. Sample Architecture Sketch

Adopt a **boundary event bus** pattern: Each module has generated publishers; a manual central bus routes events. This decouples modules while leveraging your taxonomy (e.g., interfaces for contracts, delegates for invocation).

```csharp
// Manual: Central Bus for Cross-Boundary Routing (in framework)
public interface IEventBus
{
    // Manual: Async multicast for integration events
    ValueTask PublishAsync<T>(T @event) where T : class;
    void Subscribe<T>(Action<T> handler) where T : class;
}

public class InMemoryEventBus : IEventBus // Or integrate with MassTransit for prod
{
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    public async ValueTask PublishAsync<T>(T @event) where T : class
    {
        if (_subscribers.TryGetValue(typeof(T), out var handlers))
        {
            foreach (var handler in handlers.OfType<Action<T>>())
            {
                await Task.Run(() => handler(@event)); // Fire-and-forget async
            }
        }
    }

    public void Subscribe<T>(Action<T> handler) where T : class
    {
        if (!_subscribers.TryGetValue(typeof(T), out var list))
            _subscribers[typeof(T)] = list = new();
        list.Add(handler);
    }
}

// Generated Example: For a boundary class/interface
[PublishEvents] // Attribute triggers generation
public partial interface IOrderService // From taxonomy: declaration.interface
{
    Task PlaceOrder(OrderPayload payload); // Triggers async event
}

// Generated partial impl (emitted by generator)
public partial class OrderService : IOrderService
{
    private readonly IEventBus _bus; // Injected

    public async Task PlaceOrder(OrderPayload payload)
    {
        // Business logic...
        await _bus.PublishAsync(new OrderPlacedEvent(payload)); // Generated raise
    }
}

// Event as record class (from taxonomy: record.class) for boundary payload
public record class OrderPlacedEvent(OrderPayload Payload, DateTime Timestamp);
```

- **Flow**: Generator emits `PublishAsync` calls based on method signatures → Bus handles routing → Subscribers (manual or generated) react in their modules.

### 4. Implementation Tips for Your Source Generator

- **Generator Enhancements**:
  - Analyze `InterfaceDeclarationSyntax` or `ClassDeclarationSyntax` for [PublishEvents]; emit bus injections and raises using `IEventBus`.
  - For payloads: Infer from method params, generating record classes/structs (prefer classes for cross-boundary refs).
  - Async Focus: Always wrap raises in `ValueTask` for non-blocking.

- **Testing**:
  - Integration tests: Mock bus, assert events published across mocked boundaries.
  - Boundary Sims: Use xUnit fixtures to simulate modules subscribing/publishing.

- **Evolution Path**:
  - Phase 1: Generate simple publishers.
  - Phase 2: Add Func support for request-response boundaries.
  - Future: Generator emits serializers for event payloads if boundaries go distributed.

This keeps things decoupled, performant, and generator-centric. If you have module examples or boundary specifics (e.g., UI vs. backend), I can tweak the samples further!


##### grok-4-fast (beta):

### Local project level advice.
### Refined Recommendations Integrating Your Existing Event System

Thanks for sharing the code snippets—this gives a clear picture of your foundation. Your setup is solid: a tiered bus architecture (local Core/DiscBot for intra-module efficiency, Global for cross-boundary) with async pub-sub, req-resp support, and routing via `EventBusType`. It aligns well with cross-boundary needs—locals handle module-specific noise, Global propagates signals. The `IEvent` base enforces consistency (ID, timestamp, routing), though I note some events (e.g., in `StartupEvents.cs`) don't implement it yet—more on that below.

For your refactor from a fresh perspective:
- **Strengths to Preserve**: Async-first (ValueTask/Task), multicast with locking for thread-safety, generic handlers (fits `Func<TEvent, Task>` from taxonomy's Func delegate). Req-resp via `RequestAsync` is a nice touch for coordinated boundaries without full RPC.
- **Opportunities**: Standardize event payloads to `IEvent`, auto-forward locals to Global based on `EventBusType`, and leverage your source generator to emit type-safe publishers/subscribers. This keeps manual code lean (e.g., bus impls) while generating boundary-specific wiring.
- **Principles**: Immutable events (use records), loose coupling (inject buses), performance (in-memory dicts are fine; scale to distributed later via extensions).

I'll outline: 1) Quick fixes for consistency, 2) Updated event kinds fitting your buses, 3) Gen vs. manual split, 4) Architecture enhancements with code sketches, 5) Generator integration.

### 1. Quick Consistency Fixes
- **Implement IEvent Everywhere**: Update `StartupEvents` and similar to records implementing `IEvent`. This enables uniform routing without marker interfaces.
  ```csharp
  // Example refactor for StartupInitiatedEvent
  public record StartupInitiatedEvent(
      BotOptions Configuration,
      IBotConfigurationService ConfigurationService,
      EventBusType EventBusType = EventBusType.Global,  // Default to Global for cross-boundary
      Guid EventId = default,  // Generator can set if needed
      DateTime Timestamp = default) : IEvent
  {
      public EventBusType EventBusType { get; init; } = EventBusType;
      public Guid EventId { get; init; } = EventId != default ? EventId : Guid.NewGuid();
      public DateTime Timestamp { get; init; } = Timestamp != default ? Timestamp : DateTime.UtcNow;
  }
  ```
  - Why? Enables `PublishAsync` to inspect `EventBusType` and route (e.g., Core bus publishes locally + forwards if Global).
- **Bus Initialization**: In `CoreEventBus`/`DiscBotEventBus` impls (not shown, but inferred), wire to `GlobalEventBusProvider.GetGlobalEventBus()` in `InitializeAsync()`.
- **Error Handling**: Add global error events to `GlobalErrorHandlingReadyEvent` (implement IEvent) for boundary resilience.

### 2. Recommended Event Kinds (Fitted to Your Buses)
Tailor to your tiers: Use Core/DiscBot for module-local (e.g., Discord message processing), Global for cross (e.g., user action notifying analytics). Leverage `EventHandler<TEvent>`-style via your `Func<TEvent, Task>` for async multicast.

| Event Kind | Bus Tier | Delegate Fit (Taxonomy) | Purpose in Cross-Boundary | Example (Implementing IEvent) |
|------------|----------|-------------------------|---------------------------|-------------------------------|
| **Module Signal** | Core/DiscBot | Func<TEvent, Task> | Intra-module coordination (e.g., Discord parse → local validate). | `record DiscordMessageReceivedEvent(string Content, EventBusType = EventBusType.DiscBot) : IEvent;` |
| **Integration Fact** | Global | Func<TEvent, Task> | Immutable cross-module facts (e.g., "UserActioned" from Core to UI). | `record UserActionedEvent(UserId Id, ActionType Type, EventBusType = EventBusType.Global) : IEvent;` |
| **Async Query** | Core (with Global proxy) | Func<TRequest, Task<TResponse>> (via RequestAsync) | Boundary req-resp (e.g., Core asks DiscBot for validation). | Use your `RequestAsync<UserQuery, ValidationResponse>`; events like `record ValidationRequest(UserData Data) : IEvent;` |
| **Lifecycle Broadcast** | Global | Action<TEvent> (wrapped in Func) | App-wide hooks (e.g., StartupReady to all modules). | Your `SystemReadyEvent` as record : IEvent, EventBusType.Global. |
| **Error Propagation** | Global | Func<ExceptionEvent, Task> | Cross-boundary faults (e.g., Core error → DiscBot cleanup). | `record BoundaryErrorEvent(Exception Ex, string Boundary, EventBusType = EventBusType.Global) : IEvent;` |

- **Routing Logic**: In local `PublishAsync`, if `((IEvent)@event).EventBusType == EventBusType.Global`, forward to Global after local handlers.

### 3. Generated vs. Manually Defined Events/Buses
Keep buses manual (foundation layer). Generate events and wiring for boundaries—e.g., auto-emit publishers in attributed classes.

| Category | Generated (Source Gen) | Manually Defined | Rationale |
|----------|------------------------|------------------|-----------|
| **Events** | Domain/cross events as records : IEvent (e.g., from attributes on methods). | Infrastructure like Startup/Database events. | Gen for repetitive boundary signals; manual for core lifecycle. |
| **Publishers** | Partial methods in classes/interfaces to raise via injected bus (e.g., `PublishUserActioned(this, id);`). | Bus impls (your GlobalEventBus). | Automate per-type emits; manual for thread-safe core. |
| **Subscribers** | Handler stubs in modules (e.g., `Subscribe<UserActionedEvent>(HandleAsync);`). | Global subscriptions (e.g., error handlers). | Gen for module entrypoints; manual for app-wide. |
| **Req-Resp** | Proxy wrappers if boundary-specific. | `RequestAsync` impl in buses. | Gen simple cases; manual for correlation (e.g., via EventId). |

- **Threshold**: Generate if `[EventBoundary]` on a declaration—scan for methods/properties signaling boundaries.

### 4. Architecture Enhancements
Enhance your tiered buses: Locals forward to Global selectively, use `EventId` for tracing/req-resp correlation. Inject buses via DI (e.g., in Startup).

```csharp
// Enhanced CoreEventBus Impl (build on your interface)
public class CoreEventBus : ICoreEventBus
{
    private readonly GlobalEventBus _globalBus = GlobalEventBusProvider.GetGlobalEventBus();
    private readonly Dictionary<Type, List<Delegate>> _localHandlers = new();
    // ... (similar lock as Global)

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        // Local handling
        var localTasks = InvokeHandlersAsync(_localHandlers, @event);
        
        // Forward if Global
        if (@event.EventBusType == EventBusType.Global)
            await _globalBus.PublishAsync(@event);

        await localTasks;  // Or fire-and-forget if perf-critical
    }

    private Task InvokeHandlersAsync<T>(Dictionary<Type, List<Delegate>> handlers, T @event)
    {
        // Similar to your GlobalEventBus.PublishAsync logic
        var eventType = typeof(T);
        if (!handlers.TryGetValue(eventType, out var hs)) return Task.CompletedTask;
        var tasks = hs.Select(h => ((Func<T, Task>)h)(@event));
        return Task.WhenAll(tasks);
    }

    // Subscribe/Unsubscribe: Local only
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class => 
        AddHandler(_localHandlers, handler);

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class => 
        RemoveHandler(_localHandlers, handler);

    // Req-Resp: Local first, fallback to Global if needed
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class, IEvent
        where TResponse : class
    {
        // Impl correlation via EventId, timeout, etc. (e.g., using Channel or correlator dict)
        // For simplicity: Publish request, await response on temp subscription
        var tcs = new TaskCompletionSource<TResponse?>();
        var subId = Guid.NewGuid();  // For unsub
        Subscribe<TResponse>(resp => { tcs.TrySetResult(resp); return Task.CompletedTask; });
        try
        {
            await PublishAsync(request);
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));  // Timeout
        }
        finally { Unsubscribe<TResponse>(/* handler with subId */); }
    }

    public async Task InitializeAsync() => await Task.CompletedTask;  // Wire if needed

    private void AddHandler<T>(Dictionary<Type, List<Delegate>> dict, Func<T, Task> handler)
    {
        var type = typeof(T);
        lock (_lock) { (dict.TryGetValue(type, out var list) ? list : (dict[type] = new())).Add(handler); }
    }

    private void RemoveHandler<T>(Dictionary<Type, List<Delegate>> dict, Func<T, Task> handler) { /* similar */ }
}

// DiscBotEventBus: Mirror above, swap Core → DiscBot in EventBusType checks.

// Usage in a module (e.g., Core service)
public class UserService
{
    private readonly ICoreEventBus _bus;

    public UserService(ICoreEventBus bus) => _bus = bus;

    public async Task ProcessUserAction(UserId id, ActionType type)
    {
        // Business logic...
        await _bus.PublishAsync(new UserActionedEvent(id, type));  // Routes based on EventBusType
    }
}
```

- **Req-Resp Notes**: Your `RequestAsync` is great for boundaries—enhance with correlation (match EventId on response). For cross-bus, proxy via Global.
- **Global Provider**: Good singleton; consider DI registration in Startup for testability.

### 5. Source Generator Integration
Your generator can now target this foundation:
- **Scan**: For `[EventBoundary]` on classes/interfaces, emit records : IEvent (e.g., from method params).
- **Emit**:
  - Event: `public record {EventName}Event({Params}, EventBusType = EventBusType.{Tier}) : IEvent;`
  - Publisher: `partial void Raise{EventName}({Params}) => _bus.PublishAsync(new {EventName}Event({Params}));`
  - Subscriber: In module partials, `Subscribe<{EventName}Event>(async e => await Handle{EventName}Async(e)); partial Task Handle{EventName}Async({EventName}Event e);`
- **Bus Injection**: Emit constructor params for ICoreEventBus/etc. based on namespace (e.g., Core project → ICoreEventBus).
- **Edge**: For req-resp, generate paired request/response events if attributed.

This refactor keeps your foundation intact while making it more consistent and generative. Roll out in phases: Fix IEvent impls → Enhance routing → Generator pilots. If you share a sample module or pain point (e.g., req-resp perf), I can prototype more!


##### grok-4-fast (beta):

### Event-system advice for Core, DiscBot
### Refined Recommendations Integrating Your Existing Event System

Thanks for sharing the code snippets—this gives a clear picture of your foundation. Your setup is solid: a tiered bus architecture (local Core/DiscBot for intra-module efficiency, Global for cross-boundary) with async pub-sub, req-resp support, and routing via `EventBusType`. It aligns well with cross-boundary needs—locals handle module-specific noise, Global propagates signals. The `IEvent` base enforces consistency (ID, timestamp, routing), though I note some events (e.g., in `StartupEvents.cs`) don't implement it yet—more on that below.

For your refactor from a fresh perspective:
- **Strengths to Preserve**: Async-first (ValueTask/Task), multicast with locking for thread-safety, generic handlers (fits `Func<TEvent, Task>` from taxonomy's Func delegate). Req-resp via `RequestAsync` is a nice touch for coordinated boundaries without full RPC.
- **Opportunities**: Standardize event payloads to `IEvent`, auto-forward locals to Global based on `EventBusType`, and leverage your source generator to emit type-safe publishers/subscribers. This keeps manual code lean (e.g., bus impls) while generating boundary-specific wiring.
- **Principles**: Immutable events (use records), loose coupling (inject buses), performance (in-memory dicts are fine; scale to distributed later via extensions).

I'll outline: 1) Quick fixes for consistency, 2) Updated event kinds fitting your buses, 3) Gen vs. manual split, 4) Architecture enhancements with code sketches, 5) Generator integration.

### 1. Quick Consistency Fixes
- **Implement IEvent Everywhere**: Update `StartupEvents` and similar to records implementing `IEvent`. This enables uniform routing without marker interfaces.
  ```csharp
  // Example refactor for StartupInitiatedEvent
  public record StartupInitiatedEvent(
      BotOptions Configuration,
      IBotConfigurationService ConfigurationService,
      EventBusType EventBusType = EventBusType.Global,  // Default to Global for cross-boundary
      Guid EventId = default,  // Generator can set if needed
      DateTime Timestamp = default) : IEvent
  {
      public EventBusType EventBusType { get; init; } = EventBusType;
      public Guid EventId { get; init; } = EventId != default ? EventId : Guid.NewGuid();
      public DateTime Timestamp { get; init; } = Timestamp != default ? Timestamp : DateTime.UtcNow;
  }
  ```
  - Why? Enables `PublishAsync` to inspect `EventBusType` and route (e.g., Core bus publishes locally + forwards if Global).
- **Bus Initialization**: In `CoreEventBus`/`DiscBotEventBus` impls (not shown, but inferred), wire to `GlobalEventBusProvider.GetGlobalEventBus()` in `InitializeAsync()`.
- **Error Handling**: Add global error events to `GlobalErrorHandlingReadyEvent` (implement IEvent) for boundary resilience.

### 2. Recommended Event Kinds (Fitted to Your Buses)
Tailor to your tiers: Use Core/DiscBot for module-local (e.g., Discord message processing), Global for cross (e.g., user action notifying analytics). Leverage `EventHandler<TEvent>`-style via your `Func<TEvent, Task>` for async multicast.

| Event Kind | Bus Tier | Delegate Fit (Taxonomy) | Purpose in Cross-Boundary | Example (Implementing IEvent) |
|------------|----------|-------------------------|---------------------------|-------------------------------|
| **Module Signal** | Core/DiscBot | Func<TEvent, Task> | Intra-module coordination (e.g., Discord parse → local validate). | `record DiscordMessageReceivedEvent(string Content, EventBusType = EventBusType.DiscBot) : IEvent;` |
| **Integration Fact** | Global | Func<TEvent, Task> | Immutable cross-module facts (e.g., "UserActioned" from Core to UI). | `record UserActionedEvent(UserId Id, ActionType Type, EventBusType = EventBusType.Global) : IEvent;` |
| **Async Query** | Core (with Global proxy) | Func<TRequest, Task<TResponse>> (via RequestAsync) | Boundary req-resp (e.g., Core asks DiscBot for validation). | Use your `RequestAsync<UserQuery, ValidationResponse>`; events like `record ValidationRequest(UserData Data) : IEvent;` |
| **Lifecycle Broadcast** | Global | Action<TEvent> (wrapped in Func) | App-wide hooks (e.g., StartupReady to all modules). | Your `SystemReadyEvent` as record : IEvent, EventBusType.Global. |
| **Error Propagation** | Global | Func<ExceptionEvent, Task> | Cross-boundary faults (e.g., Core error → DiscBot cleanup). | `record BoundaryErrorEvent(Exception Ex, string Boundary, EventBusType = EventBusType.Global) : IEvent;` |

- **Routing Logic**: In local `PublishAsync`, if `((IEvent)@event).EventBusType == EventBusType.Global`, forward to Global after local handlers.

### 3. Generated vs. Manually Defined Events/Buses
Keep buses manual (foundation layer). Generate events and wiring for boundaries—e.g., auto-emit publishers in attributed classes.

| Category | Generated (Source Gen) | Manually Defined | Rationale |
|----------|------------------------|------------------|-----------|
| **Events** | Domain/cross events as records : IEvent (e.g., from attributes on methods). | Infrastructure like Startup/Database events. | Gen for repetitive boundary signals; manual for core lifecycle. |
| **Publishers** | Partial methods in classes/interfaces to raise via injected bus (e.g., `PublishUserActioned(this, id);`). | Bus impls (your GlobalEventBus). | Automate per-type emits; manual for thread-safe core. |
| **Subscribers** | Handler stubs in modules (e.g., `Subscribe<UserActionedEvent>(HandleAsync);`). | Global subscriptions (e.g., error handlers). | Gen for module entrypoints; manual for app-wide. |
| **Req-Resp** | Proxy wrappers if boundary-specific. | `RequestAsync` impl in buses. | Gen simple cases; manual for correlation (e.g., via EventId). |

- **Threshold**: Generate if `[EventBoundary]` on a declaration—scan for methods/properties signaling boundaries.

### 4. Architecture Enhancements
Enhance your tiered buses: Locals forward to Global selectively, use `EventId` for tracing/req-resp correlation. Inject buses via DI (e.g., in Startup).

```csharp
// Enhanced CoreEventBus Impl (build on your interface)
public class CoreEventBus : ICoreEventBus
{
    private readonly GlobalEventBus _globalBus = GlobalEventBusProvider.GetGlobalEventBus();
    private readonly Dictionary<Type, List<Delegate>> _localHandlers = new();
    // ... (similar lock as Global)

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        // Local handling
        var localTasks = InvokeHandlersAsync(_localHandlers, @event);
        
        // Forward if Global
        if (@event.EventBusType == EventBusType.Global)
            await _globalBus.PublishAsync(@event);

        await localTasks;  // Or fire-and-forget if perf-critical
    }

    private Task InvokeHandlersAsync<T>(Dictionary<Type, List<Delegate>> handlers, T @event)
    {
        // Similar to your GlobalEventBus.PublishAsync logic
        var eventType = typeof(T);
        if (!handlers.TryGetValue(eventType, out var hs)) return Task.CompletedTask;
        var tasks = hs.Select(h => ((Func<T, Task>)h)(@event));
        return Task.WhenAll(tasks);
    }

    // Subscribe/Unsubscribe: Local only
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class => 
        AddHandler(_localHandlers, handler);

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class => 
        RemoveHandler(_localHandlers, handler);

    // Req-Resp: Local first, fallback to Global if needed
    public async Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest request)
        where TRequest : class, IEvent
        where TResponse : class
    {
        // Impl correlation via EventId, timeout, etc. (e.g., using Channel or correlator dict)
        // For simplicity: Publish request, await response on temp subscription
        var tcs = new TaskCompletionSource<TResponse?>();
        var subId = Guid.NewGuid();  // For unsub
        Subscribe<TResponse>(resp => { tcs.TrySetResult(resp); return Task.CompletedTask; });
        try
        {
            await PublishAsync(request);
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));  // Timeout
        }
        finally { Unsubscribe<TResponse>(/* handler with subId */); }
    }

    public async Task InitializeAsync() => await Task.CompletedTask;  // Wire if needed

    private void AddHandler<T>(Dictionary<Type, List<Delegate>> dict, Func<T, Task> handler)
    {
        var type = typeof(T);
        lock (_lock) { (dict.TryGetValue(type, out var list) ? list : (dict[type] = new())).Add(handler); }
    }

    private void RemoveHandler<T>(Dictionary<Type, List<Delegate>> dict, Func<T, Task> handler) { /* similar */ }
}

// DiscBotEventBus: Mirror above, swap Core → DiscBot in EventBusType checks.

// Usage in a module (e.g., Core service)
public class UserService
{
    private readonly ICoreEventBus _bus;

    public UserService(ICoreEventBus bus) => _bus = bus;

    public async Task ProcessUserAction(UserId id, ActionType type)
    {
        // Business logic...
        await _bus.PublishAsync(new UserActionedEvent(id, type));  // Routes based on EventBusType
    }
}
```

- **Req-Resp Notes**: Your `RequestAsync` is great for boundaries—enhance with correlation (match EventId on response). For cross-bus, proxy via Global.
- **Global Provider**: Good singleton; consider DI registration in Startup for testability.

### 5. Source Generator Integration
Your generator can now target this foundation:
- **Scan**: For `[EventBoundary]` on classes/interfaces, emit records : IEvent (e.g., from method params).
- **Emit**:
  - Event: `public record {EventName}Event({Params}, EventBusType = EventBusType.{Tier}) : IEvent;`
  - Publisher: `partial void Raise{EventName}({Params}) => _bus.PublishAsync(new {EventName}Event({Params}));`
  - Subscriber: In module partials, `Subscribe<{EventName}Event>(async e => await Handle{EventName}Async(e)); partial Task Handle{EventName}Async({EventName}Event e);`
- **Bus Injection**: Emit constructor params for ICoreEventBus/etc. based on namespace (e.g., Core project → ICoreEventBus).
- **Edge**: For req-resp, generate paired request/response events if attributed.

This refactor keeps your foundation intact while making it more consistent and generative. Roll out in phases: Fix IEvent impls → Enhance routing → Generator pilots. If you share a sample module or pain point (e.g., req-resp perf), I can prototype more!