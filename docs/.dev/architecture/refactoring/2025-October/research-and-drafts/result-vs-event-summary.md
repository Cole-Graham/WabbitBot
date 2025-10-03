# Result vs Event Pattern - Quick Reference Guide

**Created:** October 3, 2025  
**Purpose:** Clarify when to use Result<T> pattern vs Events in WabbitBot architecture

---

## The Two Patterns

### Result Pattern
```csharp
public async Task<Result<Match>> CreateMatchAsync(...)
{
    // Returns immediate outcome to caller
    if (validation fails)
        return Result.Failure("error message");
    
    return Result.CreateSuccess(match);
}
```

**Purpose**: Immediate feedback for the direct caller  
**Scope**: Method return values  
**Flow**: Synchronous response (even if async method)  
**Audience**: The calling code only

### Event Pattern
```csharp
public async Task ProcessMatchAsync(...)
{
    // ... do work ...
    
    // Broadcast fact to interested parties
    await EventBus.PublishAsync(new MatchCompletedEvent(matchId, winnerId));
}
```

**Purpose**: Notify interested subscribers about state changes  
**Scope**: One-to-many broadcast  
**Flow**: Fire-and-forget (or request-response)  
**Audience**: Multiple subscribers across boundaries

---

## Decision Matrix

### Use Result When:
✅ Calling within same project boundary (Core → Core, DiscBot → DiscBot)  
✅ Caller needs immediate success/failure feedback  
✅ Caller makes decisions based on outcome  
✅ Operation needs return value/data  

**Example:**
```csharp
var result = await MatchCore.StartMatchAsync(matchId);
if (!result.Success)
{
    return Result.Failure(result.ErrorMessage); // Handle error locally
}
// Continue with result.Data
```

### Use Events When:
✅ Notifying that something happened (past tense)  
✅ Cross-boundary communication (Core ↔ DiscBot)  
✅ Multiple subscribers need to know  
✅ Decoupling > immediate feedback  
✅ Caller doesn't need to wait for outcome  

**Example:**
```csharp
// Broadcast to leaderboards, notifications, Discord, etc.
await CoreEventBus.PublishAsync(new MatchCompletedEvent(matchId, winnerId));
```

### Use Request-Response Events When:
⚠️ RARELY - Cross-boundary query only  
✅ DiscBot needs data from Core  
✅ Direct reference creates unwanted coupling  
❌ AVOID for intra-boundary calls  

**Example:**
```csharp
// Only when absolutely needed
var response = await EventBus.RequestAsync<AssetResolveRequested, AssetResolved>(request);
```

---

## The Hybrid Pattern ⭐

**Most business operations should do BOTH:**

```csharp
public async Task<Result> CompleteMatchAsync(Guid matchId, Guid winnerId)
{
    // Step 1: Perform operation
    var updateResult = await UpdateMatchInDatabase(matchId, winnerId);
    
    if (!updateResult.Success)
        return Result.Failure("Failed to update match"); // Result only, no event
    
    // Step 2: Publish event for subscribers
    await CoreService.PublishAsync(new MatchCompletedEvent(matchId, winnerId));
    
    // Step 3: Return success to caller
    return Result.CreateSuccess();
}
```

### When to Use Hybrid:
✅ Core business operations that change state  
✅ Operations where both immediate feedback AND broadcast are needed  
✅ Actions that trigger downstream processing  

**Examples:**
- `CreateMatchAsync` → returns Result<Match> + publishes MatchCreatedEvent
- `CompleteMatchAsync` → returns Result + publishes MatchCompletedEvent  
- `UpdateRatingAsync` → returns Result + publishes RatingUpdatedEvent

### When NOT to Use Hybrid:
❌ Simple queries (just return Result<T>)  
❌ Pure calculations (just return Result<T>)  
❌ Operations with no state changes  

---

## Renderer/Handler Pattern

### Renderers (DiscBot DSharpPlus layer):

```csharp
public async Task<Result> CreateMatchThreadAsync(Guid matchId)
{
    try
    {
        // Perform Discord API operation
        var thread = await channel.CreateThreadAsync(...);
        
        // SUCCESS: Publish confirmation event
        await DiscBotService.PublishAsync(new MatchThreadCreatedEvent(matchId, thread.Id));
        
        // AND return Result
        return Result.CreateSuccess();
    }
    catch (Exception ex)
    {
        // FAILURE: Log error, return failure, NO event
        await DiscBotService.ErrorHandler.CaptureAsync(ex, ...);
        return Result.Failure($"Failed to create thread: {ex.Message}");
    }
}
```

**Key Points:**
- ✅ Return `Task<Result>` for operation outcome
- ✅ On success: Result.CreateSuccess() AND publish confirmation event
- ✅ On failure: Result.Failure() AND log error (NO event)
- ✅ Subscribers only receive event on success

---

## Error Handling

| Scenario | Result.Failure | ErrorService.CaptureAsync | Event (Success) | BoundaryErrorEvent |
|----------|----------------|---------------------------|-----------------|-------------------|
| Method succeeds (intra-boundary) | ❌ | ❌ | ✅ (if state changed) | ❌ |
| Method fails (intra-boundary) | ✅ | ✅ | ❌ | ❌ |
| Method succeeds (cross-boundary) | ✅ | ❌ | ✅ | ❌ |
| Method fails (cross-boundary) | ✅ | ✅ | ❌ | ✅ (if affects others) |
| Event handler fails | N/A | ✅ | ❌ | ✅ (if cross-boundary) |
| Validation failure (expected) | ✅ | ❌ | ❌ | ❌ |
| Unexpected exception | ✅ | ✅ | ❌ | ✅ (if cross-boundary) |

### Key Principles:
1. **Result** = immediate outcome (success/failure + data/error)
2. **Events** = broadcast facts (what happened, not outcome)
3. **ErrorService** = logging/monitoring (all exceptions)
4. **BoundaryErrorEvent** = cross-boundary failures others need to know about

---

## Decision Tree

```
Is this within the same project boundary?
├─ YES → Use direct method call returning Result<T>
│         ├─ Does it change state?
│         │   └─ YES → Also publish event (Hybrid pattern)
│         └─ NO → Just return Result<T>
│
└─ NO (cross-boundary)
    ├─ Is this a command/notification?
    │   └─ Use fire-and-forget event (Global bus)
    │
    ├─ Is this a query for data?
    │   ├─ Can you call service method directly?
    │   │   └─ YES → Use method call with Result<T>
    │   └─ Must be completely decoupled?
    │       └─ Use Request-Response event (rarely)
    │
    └─ Is this an error affecting other boundaries?
        └─ Publish BoundaryErrorEvent + return Result.Failure
```

---

## Common Patterns

### Pattern 1: Core Business Operation (Hybrid)
```csharp
public async Task<Result> ExecuteBusinessLogicAsync(...)
{
    // Validation
    if (invalid) return Result.Failure("validation error");
    
    // Perform work
    var data = await DoWork();
    
    // Publish event for subscribers
    await EventBus.PublishAsync(new BusinessEventCompleted(...));
    
    // Return result to caller
    return Result.CreateSuccess();
}
```

### Pattern 2: Query (Result only)
```csharp
public async Task<Result<Data>> GetDataAsync(Guid id)
{
    var data = await repository.GetByIdAsync(id);
    
    if (data is null)
        return Result.Failure("Not found");
    
    return Result.CreateSuccess(data);
}
```

### Pattern 3: Cross-Boundary Command (Event only)
```csharp
// DiscBot sends command to Core
await GlobalEventBus.PublishAsync(new ScrimmageChallengeRequested(
    challengerTeam, opponentTeam));

// No return value, fire-and-forget
```

### Pattern 4: Renderer Operation (Hybrid)
```csharp
public async Task<Result> RenderAsync(...)
{
    try
    {
        await DiscordAPI.DoSomething();
        
        // Success: event + result
        await EventBus.PublishAsync(new RenderCompletedEvent(...));
        return Result.CreateSuccess();
    }
    catch (Exception ex)
    {
        // Failure: log + result (no event)
        await ErrorService.CaptureAsync(ex, ...);
        return Result.Failure(ex.Message);
    }
}
```

---

## Anti-Patterns to Avoid

### ❌ Using Events as Return Values
```csharp
// BAD: Using event to get data back
public async Task DoWorkAsync()
{
    var response = await EventBus.RequestAsync<GetData, DataResponse>(...);
    // Should just call GetDataAsync() directly!
}
```

### ❌ Publishing Events on Failure
```csharp
// BAD: Publishing event when operation failed
if (!success)
{
    await EventBus.PublishAsync(new OperationFailedEvent(...)); // NO!
    return Result.Failure("...");
}
```
Use `Result.Failure` for the caller and `ErrorService.CaptureAsync` for logging instead.

### ❌ Using Result for Cross-Boundary Notifications
```csharp
// BAD: Trying to return Result across boundaries
// From Core, trying to notify DiscBot
public async Task<Result> NotifyDiscord(...)  // Can't do this!
{
    // DiscBot can't call this directly!
}
```
Use events for cross-boundary notifications instead.

### ❌ Not Using Hybrid for State Changes
```csharp
// BAD: State change with no event
public async Task<Result> CompleteMatchAsync(...)
{
    await UpdateDatabase(...);
    return Result.CreateSuccess(); // Leaderboards won't update!
}

// GOOD: State change with Result + Event
public async Task<Result> CompleteMatchAsync(...)
{
    await UpdateDatabase(...);
    await EventBus.PublishAsync(new MatchCompletedEvent(...)); // Leaderboards notified
    return Result.CreateSuccess();
}
```

---

## Summary

**Result Pattern:**
- Direct caller ↔ callee communication
- Immediate feedback
- Control flow decisions
- Intra-boundary preferred

**Event Pattern:**
- One-to-many broadcast
- Decoupled notification
- Cross-boundary communication
- State change announcements

**Hybrid Pattern (Most Common):**
- Business operations that change state
- Return Result for caller + Publish Event for subscribers
- Events only on success
- Best of both worlds

**Remember:** Result tells the caller what happened. Events tell everyone else.

