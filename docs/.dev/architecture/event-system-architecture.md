# WabbitBot Event System Architecture

## Overview
The WabbitBot event system provides a standardized, type-safe, and loosely-coupled communication mechanism between different components of the application. Built on the vertical slice architecture, it uses immutable event records and tiered event buses to enable cross-boundary communication without tight coupling.

## Core Principles

### 1. Event-Only Communication
**CRITICAL**: The event system MUST NOT be used for database operations. Events are strictly for communication and signaling between components. CRUD operations should be handled directly via repositories or services, not through events.

### 2. Immutable Events
All events are implemented as immutable C# records implementing the `IEvent` interface:
```csharp
public interface IEvent
{
    EventBusType EventBusType { get; init; }
    Guid EventId { get; init; }
    DateTime Timestamp { get; init; }
}
```

### 3. Tiered Bus Architecture
- **CoreEventBus**: Intra-module communication within WabbitBot.Core
- **DiscBotEventBus**: Intra-module communication within WabbitBot.DiscBot
- **GlobalEventBus**: Cross-boundary communication between modules

## Event Types

### 1. Module Signals
Internal notifications within a module (e.g., `ConfigurationChangedEvent`, `MatchStartedEvent`)

### 2. Integration Facts
Cross-boundary events that notify other modules of state changes (e.g., `TournamentCompletedEvent`, `UserStatusUpdatedEvent`)

### 3. Async Queries (Request-Response)
Events that expect a response, using correlation via `EventId`:
```csharp
// Request
public record GetUserDetailsRequest(string UserId) : IEvent;

// Response
public record GetUserDetailsResponse(UserDetails Details) : IEvent;
```

### 4. Lifecycle Broadcasts
System-wide notifications (e.g., `ApplicationReadyEvent`, `SystemShuttingDownEvent`)

### 5. Error Propagation
Cross-boundary error notifications (e.g., `BoundaryErrorEvent`)

## Event Bus Implementation

### Publishing Events
```csharp
await eventBus.PublishAsync(new UserCreatedEvent(userId, username));
```

### Request-Response Pattern
```csharp
var response = await eventBus.RequestAsync<GetUserDetailsRequest, GetUserDetailsResponse>(
    new GetUserDetailsRequest(userId),
    timeout: TimeSpan.FromSeconds(30));
```

### Forwarding Logic
Local buses automatically forward `EventBusType.Global` events to the GlobalEventBus using fire-and-forget patterns with error handling.

## Source Generation

### Event Records
Events are generated from attributed classes using `[EventBoundary]`:

```csharp
[EventBoundary]
public partial class UserService
{
    [EventTriggeringMethod]
    public ValueTask UserCreatedAsync(string userId, string username)
    {
        // Method body - event will be generated
    }
}
```

Generates:
```csharp
public record UserCreatedEvent(
    string UserId,
    string Username,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default) : IEvent;
```

### Publishers
Partial class extensions provide type-safe event publishing:
```csharp
public partial class UserService
{
    public ValueTask RaiseUserCreatedAsync(string userId, string username)
    {
        var @event = new UserCreatedEvent(userId, username);
        return _eventBus.PublishAsync(@event);
    }
}
```

### Subscribers
Handler classes are automatically wired to event subscriptions based on method signatures.

## Event Bus Types

```csharp
public enum EventBusType
{
    Core,      // WabbitBot.Core internal
    DiscBot,   // WabbitBot.DiscBot internal
    Global     // Cross-boundary
}
```

## Terminology and Roles

### Handler
- **What it is**: A thin orchestrator that subscribes to events and delegates to business logic.
- **Responsibilities**:
  - Subscribe to one or more events on the appropriate bus.
  - Perform light validation, correlation, and orchestration.
  - Delegate to commands/services for actual work (write or read paths).
  - Maintain idempotence and be safe to retry.
- **Anti-responsibilities**:
  - No direct database CRUD.
  - No long-running business workflows or state machines.
  - No cross-slice coupling beyond publishing/subscribing to events.

### Publisher / Subscriber
- **Publisher**: Any code that emits an event (often via generated `RaiseXyzAsync` methods).
- **Subscriber**: A function/method registered with a bus that receives events and typically lives in a Handler.

### Command (Write Path)
- **What it is**: Business logic that mutates state. Lives in vertical slices (e.g., `ScrimmageCommands`).
- **Invocation**: Called directly by handlers or feature entry points, not by events performing CRUD.
- **Rules**: Validate inputs, perform repository operations, raise facts as needed (post-commit), return results/errors.

### Query (Read Path)
- **What it is**: Business logic that reads data with no side effects.
- **Invocation**: Called by handlers, Discord layer, or other read surfaces.
- **Rules**: No mutations; optimized for reads and projection shaping.

### Service / Processor
- **What it is**: Business logic components that encapsulate reusable domain operations (e.g., leaderboard calculations).
- **Relationship to Handlers**: Handlers orchestrate; services/processors execute domain work.

### Event Buses
- **CoreEventBus**: For `WabbitBot.Core` internal events and request/response within Core.
- **DiscBotEventBus**: For `WabbitBot.DiscBot` internal events.
- **GlobalEventBus**: Cross-boundary bus. Local buses forward `EventBusType.Global` events.

### Request–Response (Async Queries over Events)
- **Use**: Cross-boundary queries that need a response, correlated by `EventId`.
- **Guideline**: Prefer direct method calls inside a boundary; use request–response only when the caller and callee are on different buses.

### Error Handler
- **What it is**: Boundary-aware error orchestration (e.g., logging, notifications) that may publish error events.
- **Rule**: Do not bury domain failures; surface them via error contexts and, if cross-boundary, via error events.

### Orchestration vs. Business Logic Boundaries
- **Handlers orchestrate**; **commands/services execute**.
- **Events communicate**; **repositories/services perform CRUD**.
- **Buses route**; **generators wire publishers/subscribers**.

## Best Practices

### 1. Event Naming
- Use past tense for facts: `UserCreated`, `MatchCompleted`
- Use present tense for requests: `GetUserDetails`, `ValidateMatch`
- Include domain context: `TournamentMatchStarted` vs `MatchStarted`

### 2. Event Payloads
- Keep payloads small and focused
- Use primitive types and simple DTOs
- Avoid circular references
- Include correlation IDs for request-response patterns

### 3. Error Handling
- Use `BoundaryErrorEvent` for cross-boundary failures
- Log errors at boundaries
- Avoid throwing exceptions from event handlers

### 4. Performance
- Use `ValueTask` for async operations
- Implement fire-and-forget for non-critical events
- Monitor event bus throughput

## Testing

### Unit Tests
- Test event record immutability
- Test event bus publishing/subscription
- Test request-response correlation

### Integration Tests
- Test cross-boundary event flow
- Test error propagation
- Test timeouts and cancellation

### Generator Tests
- Verify generated code compilation
- Test template output matches expectations
- Test attribute validation

## Migration from Legacy Events

Legacy event classes should be:
1. Converted to records implementing `IEvent`
2. Moved to appropriate modules
3. Updated to use proper `EventBusType`
4. CRUD events should be removed and replaced with direct service calls

## Monitoring and Observability

- Log all event publications and subscriptions
- Track event throughput and latency
- Monitor correlation ID matching for request-response
- Alert on event bus errors or timeouts

## Architecture Benefits

1. **Loose Coupling**: Components communicate through events, not direct references
2. **Testability**: Events can be easily mocked and verified
3. **Scalability**: Async processing prevents blocking
4. **Observability**: Clear event flow for debugging and monitoring
5. **Type Safety**: Source generation ensures compile-time correctness
6. **Maintainability**: Clear separation of concerns and standardized patterns
