#### Step 7: CoreService Organization

### Static CoreService with Generator-defined Accessors

**🎯 CRITICAL**: `CoreService` is a static partial class. The source generators emit DatabaseService accessors into a
generated partial, and shared helpers live in manual partials (e.g., `CoreService.Database.cs`). There are no
per-entity service classes like `PlayerService`.

#### 7a. CoreService.cs (Static Initialization and Helpers)

`CoreService` exposes static properties for the event bus, error handler, and DbContext factory, initialized once
at startup. It also provides standardized error-handling helpers.

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/CoreService.cs
public static partial class CoreService
{
    public static ICoreEventBus EventBus { get; }
    public static IErrorService ErrorHandler { get; }
    public static IDbContextFactory<WabbitBotDbContext> DbContextFactory { get; }

    public static void InitializeServices(
        ICoreEventBus eventBus,
        IErrorService errorHandler,
        IDbContextFactory<WabbitBotDbContext> dbContextFactory)
    {
        // Sets lazy/static fields; called once during startup
    }

    public static Task PublishAsync<TEvent>(TEvent evt) where TEvent : class, IEvent { /* ... */ }
    public static Task<Result> TryAsync(Func<Task> op, string operationName) { /* ... */ }
    public static Task<Result<T>> TryAsync<T>(Func<Task<T>> op, string operationName) { /* ... */ }
}
```

#### 7b. CoreService.Database.cs (DbContext Helpers)

Manual partial providing safe DbContext scope helpers.

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/CoreService.Database.cs
public static partial class CoreService
{
    public static Task WithDbContext(Func<WabbitBotDbContext, Task> work) { /* ... */ }
    public static Task<T> WithDbContext<T>(Func<WabbitBotDbContext, Task<T>> work) { /* ... */ }
    public static Task<Result> TryWithDbContext(Func<WabbitBotDbContext, Task> work, string op) { /* ... */ }
    public static Task<Result<T>> TryWithDbContext<T>(Func<WabbitBotDbContext, Task<T>> work, string op) { /* ... */ }
}
```

#### 7c. Generated DatabaseService Accessors

The DatabaseService accessors are generated into a CoreService partial (e.g., `DatabaseServiceAccessors.g.cs`).
They provide static, lazily-initialized `DatabaseService<TEntity>` instances per entity.

```csharp
// Auto-generated (example)
public static partial class CoreService
{
    public static DatabaseService<Map> Maps { get; }
    public static DatabaseService<Player> Players { get; }
    public static DatabaseService<Team> Teams { get; }
    // ... and so on for all entities
}

Usage is straightforward and DI-free:

```csharp
var map = await CoreService.Maps.GetByIdAsync(mapId);
var playerResult = await CoreService.Players.CreateAsync(new Player { Name = name });
await CoreService.PublishAsync(new SomeDomainEvent(...));
```
```

#### 7d. Startup Wiring (No Runtime DI)

Initialize `CoreService` once after database options are ready.

```csharp
// File: src/WabbitBot.Core/Program.cs (conceptual)
CoreService.InitializeServices(coreEventBus, errorService, dbContextFactory);
```

#### 7e. Create CoreService.Player.Validation.cs with Validation

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/Player/CoreService.Player.Validation.cs
public partial class CoreService
{
    #region Player Validation

    // IMPORTANT: Only implement validation methods when specific validation rules are required
    // DO NOT implement speculative validation logic

    // Example: This file starts empty and only gets validation methods
    // when they are actually needed by the application

    // private async Task<Result> ValidatePlayerAsync(Player player)
    // {
    //     // Add validation logic here ONLY when specific rules are determined
    //     // Don't assume what validation is needed
    // }

    #endregion
}
```

#### 7f. Set up Direct Instantiation and Event Messaging (No Runtime DI)

Configure the service for direct instantiation and event-driven communication.

```csharp
// File: src/WabbitBot.Core/Program.cs - Direct Instantiation (No Runtime DI)
public class Program
{
    public static async Task Main()
    {
        // Direct instantiation - no runtime dependency injection
        var coreService = new CoreService(
            eventBus: new CoreEventBus(),
            errorService: new ErrorService());

        // Start the core service
        await coreService.InitializeAsync(CancellationToken.None);

        // Keep application alive (e.g., event-driven bot)
        await Task.Delay(-1);
    }
}
```

#### STEP 7 IMPACT:

### Partial Class Architecture Benefits

#### Before (Scattered Services):
```csharp
// ❌ OLD: Multiple service classes with unclear responsibilities
public class PlayerService      // Business logic mixed with data access
public class PlayerRepository   // Data access layer
public class PlayerCache        // Caching layer
public class DataServiceManager // Complex coordination
```

#### After (Static CoreService + Generated Accessors):
```csharp
// ✅ NEW: Static CoreService with generator-defined DatabaseService accessors
public static partial class CoreService
{
    public static DatabaseService<Player> Players { get; }
    public static DatabaseService<Team> Teams { get; }
    // ...
}
```

### Architecture Benefits Achieved

#### 1. **🎯 Single Responsibility Principle**
- **CoreService**: Business logic and orchestration.
- **DatabaseService**: All data access concerns (Repository, Cache, Archive).
- **ErrorService**: Centralized error handling.

#### 2. **🧹 Clean Code Organization**
- Business logic fully separated from data access.
- Validation concerns isolated
- Generic operations reusable across entities
- Easy to locate and maintain specific functionality

#### 3. **🔧 Easy Extension Pattern**
- New entities follow the same partial class structure
- Add `CoreService.Team.cs`, `CoreService.Match.cs`, etc.
- Consistent patterns across all entity types
- No boilerplate service registration

#### 4. **🚀 Direct Instantiation Benefits**
- No runtime dependency injection overhead.
- Clear service initialization flow.
- Easier debugging and testing
- Compile-time dependency verification

#### 5. **📡 Event-Driven Communication**
- Event bus integration for loose coupling
- Services communicate through events, not direct calls
- Easier to add new subscribers without modifying existing code
- Better testability through event mocking

#### 6. **🧪 Focused Testing Strategy**
- Test generic operations once, reuse across entities
- Test business logic in isolation
- Test validation rules separately
- Easier to mock and verify specific concerns

### Lean Implementation Approach

**Phase 1 - Minimal Viable CoreService:**
- ✅ CoreService.cs with business logic orchestration.
- ✅ `DatabaseService<TEntity>` handling all data access.
- ✅ Direct usage of `DatabaseService` from `CoreService`.
- ✅ `ErrorService` handling all error logic.

**Phase 2 - Iterative Enhancement:**
- 🔄 Add player-specific data queries only when required
- 🔄 Add validation methods only when business rules emerge
- 🔄 Add event publishing only when inter-service communication needed
- 🔄 Add new entity support following the established pattern

### Success Metrics

The CoreService refactor succeeds when:
- ✅ All entity business logic flows through a single `CoreService` instance.
- ✅ All data access flows through `DatabaseService<T>` instances.
- ✅ Code is organized with clear separation of concerns.
- ✅ New entity support requires minimal boilerplate
- ✅ Direct instantiation eliminates DI complexity
- ✅ Event messaging enables loose coupling
- ✅ No speculative code exists - everything serves a current need

**This static CoreService + generator accessors model is the authoritative pattern going forward.** 🎯
