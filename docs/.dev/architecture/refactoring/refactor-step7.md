#### Step 7: CoreService Organization

### Single CoreService Architecture with Partial Classes

**🎯 CRITICAL**: Consolidate all core entity operations into a single `CoreService` that uses partial classes for organization, direct instantiation, and event messaging instead of dependency injection.

#### 7a. Create Main CoreService.cs

Implement the main CoreService class with direct instantiation of data and error services.

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/CoreService.cs
public partial class CoreService
{
    // Unified DatabaseService per entity
    private readonly DatabaseService<Player> _playerData;
    private readonly DatabaseService<User> _userData;
    private readonly DatabaseService<Team> _teamData;
    private readonly DatabaseService<Map> _mapData;

    // Event bus and unified error handling
    private readonly ICoreEventBus _eventBus;
    private readonly IErrorService _errorService;

    public CoreService(
        ICoreEventBus eventBus,
        IErrorService errorService)
    {
        _eventBus = eventBus;
        _errorService = errorService;

        // Create DatabaseService for each entity via direct instantiation
        _playerData = new DatabaseService<Player>();
        _userData = new DatabaseService<User>();
        _teamData = new DatabaseService<Team>();
        _mapData = new DatabaseService<Map>();
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Initialization logic for the service
        return Task.CompletedTask;
    }
}
```

#### 7b. Create CoreService.Data.cs with Generic Operations

The unified `DatabaseService<TEntity>` encapsulates generic data operations, removing the need for them in `CoreService`. The service now directly uses the `DatabaseService` instances.

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/CoreService.Data.cs
// DEPRECATED: Generic data operations are now part of DatabaseService<TEntity>
// This partial class can be removed or repurposed for entity-specific data logic orchestration if needed.
public partial class CoreService
{
    // Example of direct usage from a business logic method:
    public async Task<Player?> GetPlayerAsync(Guid playerId)
    {
        // DatabaseService handles the cache-repository logic automatically.
        var player = await _playerData.GetByIdAsync(playerId);
        return player;
    }

    public async Task<Result<Player>> CreatePlayerAsync(Player newPlayer)
    {
        // DatabaseService handles write-through caching automatically.
        var result = await _playerData.CreateAsync(newPlayer);
        return result;
    }
}
```

#### 7c. Create CoreService.Player.cs with Business Logic

Implement player-specific business logic using the unified `DatabaseService` directly.

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/Player/CoreService.Player.cs
public partial class CoreService
{
    #region Player Business Logic

    // IMPORTANT: Business logic methods now call the unified DatabaseService directly.

    // ✅ GOOD: Direct and clear data access.
    public async Task<Player?> GetPlayerByIdAsync(Guid id)
    {
        return await _playerData.GetByIdAsync(id);
    }

    // ✅ GOOD: Specific methods for unique business logic are still valuable.
    public async Task<Player?> GetPlayerByNameAsync(string name)
    {
        // Custom logic can still exist, but data access is simplified.
        // Assuming a custom query method exists on DatabaseService.Repository.cs
        var player = await _playerData.GetByNameAsync(name);
        return player;
    }

    #endregion
}
```

#### 7d. Create CoreService.Player.Data.cs with Data Operations

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/Player/CoreService.Player.Data.cs
public partial class CoreService
{
    #region Player Data Operations

    // IMPORTANT: Entity-specific data operations now belong in the relevant partial
    // class of DatabaseService, e.g., DatabaseService.Repository.cs for custom queries.
    // This CoreService partial should only contain orchestration logic if needed.

    #endregion
}
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
// File: src/WabbitBot.Core/Program.cs - Service Registration (No Runtime DI)
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Direct instantiation - no runtime dependency injection
        var coreService = new CoreService(
            eventBus: new CoreEventBus(),
            errorService: new ErrorService());

        // Start the core service
        await coreService.InitializeAsync(CancellationToken.None);

        host.Run();
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

#### After (Unified Partial Classes):
```csharp
// ✅ NEW: Single CoreService for business logic, single DatabaseService for data access
public partial class CoreService
{
    // CoreService.Player.cs - Player business logic
    // CoreService.Team.cs - Team business logic
}

public partial class DatabaseService<TEntity>
{
    // DatabaseService.cs - High-level coordination (e.g., cache-first reads)
    // DatabaseService.Repository.cs - Database operations
    // DatabaseService.Cache.cs - Caching operations
    // DatabaseService.Archive.cs - Archiving operations
}

// Direct instantiation - no runtime DI complexity
var errorService = new ErrorService();
var coreService = new CoreService(new CoreEventBus(), errorService);
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

**This partial class CoreService architecture establishes our unified, maintainable entity management foundation!** 🎯
