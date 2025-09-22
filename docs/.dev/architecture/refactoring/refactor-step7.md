#### Step 7: CoreService Organization

### Single CoreService Architecture with Partial Classes

**🎯 CRITICAL**: Consolidate all core entity operations into a single `CoreService` that uses partial classes for organization, direct instantiation, and event messaging instead of dependency injection.

#### 7a. Create Main CoreService.cs

Implement the main CoreService class as a BackgroundService with direct instantiation of data services.

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/CoreService.cs
public partial class CoreService : BackgroundService
{
    // Entity configurations
    private readonly PlayerDbConfig _playerDbConfig;
    private readonly UserDbConfig _userDbConfig;
    private readonly TeamDbConfig _teamDbConfig;
    private readonly MapConfig _mapDbConfig;

    // Data services (generic, configured per entity)
    private readonly RepositoryService<Player> _playerRepositoryData;
    private readonly CacheService<Player> _playerCacheData;
    private readonly ArchiveService<Player> _playerArchiveData;

    // Event bus and error handling
    private readonly ICoreEventBus _eventBus;
    private readonly ICoreErrorHandler _errorHandler;

    public CoreService(
        ICoreEventBus eventBus,
        ICoreErrorHandler errorHandler)
        // Data services created via direct instantiation (no runtime DI)
        // RepositoryService<Player> playerRepository,
        // CacheService<Player> playerCache,
        // ArchiveService<Player> playerArchive)
    {
        _eventBus = eventBus;
        _errorHandler = errorHandler;

        // Initialize configurations
        _playerDbConfig = new PlayerDbConfig();
        _userDbConfig = new UserDbConfig();
        _teamDbConfig = new TeamDbConfig();
        _mapDbConfig = new MapConfig();

        // Create data services via direct instantiation (no runtime DI)
        _playerRepositoryData = new RepositoryService<Player>(
            new DatabaseConnectionProvider(),
            _playerDbConfig.TableName,
            _playerDbConfig.Columns);
        _playerCacheData = new CacheService<Player>(
            new CacheMonitor(),
            _playerDbConfig.MaxCacheSize);
        _playerArchiveData = new ArchiveService<Player>(
            new DatabaseConnectionProvider(),
            _playerDbConfig.ArchiveTableName,
            _playerDbConfig.Columns);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // Initialize data services
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        // Cleanup
        return Task.CompletedTask;
    }
}
```

#### 7b. Create CoreService.Data.cs with Generic Operations

Implement generic data operations that all entity services can use.

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/CoreService.Data.cs
public partial class CoreService
{
    #region Generic Data Operations

    private async Task<Result<TEntity>> CreateEntityAsync<TEntity>(
        TEntity entity,
        RepositoryService<TEntity> repository,
        CacheService<TEntity> cache) where TEntity : IEntity
    {
        // Create in repository
        var result = await repository.CreateAsync(entity, DatabaseComponent.Repository);
        if (result.Success)
        {
            // Cache the result
            await cache.CreateAsync(result.Data!, DatabaseComponent.Cache);
        }
        return result;
    }

    private async Task<TEntity?> GetByIdAsync<TEntity>(
        object id,
        RepositoryService<TEntity> repository,
        CacheService<TEntity> cache) where TEntity : IEntity
    {
        // Try cache first
        var cached = await cache.GetByIdAsync(id, DatabaseComponent.Cache);
        if (cached != null) return cached;

        // Fall back to repository
        var entity = await repository.GetByIdAsync(id, DatabaseComponent.Repository);
        if (entity != null)
        {
            await cache.CreateAsync(entity, DatabaseComponent.Cache);
        }
        return entity;
    }

    #endregion
}
```

#### 7c. Create CoreService.Player.cs with Business Logic

Implement player-specific business logic using generic operations directly.

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/Player/CoreService.Player.cs
public partial class CoreService
{
    #region Player Business Logic

    IMPORTANT: Start with MINIMAL business logic
    Use generic methods directly - NO thin wrappers or pseudonyms

    ❌ AVOID: Pseudonym methods that confuse intent
    public Task<Player?> GetPlayerByIdAsync(object id) =>
        GetByIdAsync(id, _playerRepositoryData, _playerCacheData); // Confusing!

    ✅ GOOD: Use generic methods directly from calling code
    var player = await GetByIdAsync(playerId, _playerRepositoryData, _playerCacheData);

    ✅ GOOD: Create specific methods ONLY when there's unique business logic
    public async Task<Player?> GetPlayerByNameAsync(string name)
    {
        // Custom caching logic, validation, or business rules here
        var cached = await GetByNameAsync(name, _playerCacheData, DatabaseComponent.Cache);
        if (cached != null) return cached;

        var player = await GetByNameAsync(name, _playerRepositoryData, DatabaseComponent.Repository);
        if (player != null)
        {
            await CreateAsync(player, _playerCacheData, DatabaseComponent.Cache);
        }
        return player;
    }

    #endregion
}
```

#### 7d. Create CoreService.Player.Data.cs with Data Operations

Implement player-specific data operations that are immediately needed.

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/Player/CoreService.Player.Data.cs
public partial class CoreService
{
    #region Player Data Operations

    // IMPORTANT: Only implement data operations when they are actually needed
    // Start with basic CRUD operations and add complex queries as required

    // Example: Start with just the basic operations and add more as needed
    // Don't implement speculative methods like archiving, complex queries, etc.

    // public async Task<IEnumerable<Player>> GetPlayersByTeamAsync(string teamId)
    // {
    //     // Only implement this when the feature is actually needed
    //     // Don't assume we'll need team-based queries
    // }

    #endregion
}
```

#### 7e. Create CoreService.Player.Validation.cs with Validation

Implement player-specific validation logic only when needed.

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
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Direct instantiation - no runtime dependency injection
        var coreService = new CoreService(
            eventBus: new CoreEventBus(),
            errorHandler: new CoreErrorHandler());

        // Start the core service
        host.Services.GetRequiredService<IServiceProvider>()
            .GetRequiredService<CoreService>()
            .StartAsync(CancellationToken.None);

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
public class PlayerRepository   // Data access with business logic
public class PlayerCache        // Caching mixed with business rules
public class PlayerValidator    // Validation scattered across services

// Complex dependency injection setup
services.AddScoped<IPlayerService, PlayerService>();
services.AddScoped<IPlayerRepository, PlayerRepository>();
// ... many more registrations
```

#### After (Unified Partial Classes):
```csharp
// ✅ NEW: Single CoreService with clear separation via partial classes
public partial class CoreService
{
    // CoreService.cs - Main service orchestration
    // CoreService.Data.cs - Generic data operations
    // CoreService.Player.cs - Player business logic
    // CoreService.Player.Data.cs - Player-specific data operations
    // CoreService.Player.Validation.cs - Player validation rules
}

// Direct instantiation - no runtime DI complexity
var coreService = new CoreService(eventBus, errorHandler);
```

### Architecture Benefits Achieved

#### 1. **🎯 Single Responsibility Principle**
- **CoreService.cs**: Service orchestration and configuration
- **CoreService.Data.cs**: Generic CRUD operations for all entities
- **CoreService.Player.cs**: Player-specific business logic only
- **CoreService.Player.Data.cs**: Player data operations when needed
- **CoreService.Player.Validation.cs**: Player validation rules when required

#### 2. **🧹 Clean Code Organization**
- Business logic separated from data access
- Validation concerns isolated
- Generic operations reusable across entities
- Easy to locate and maintain specific functionality

#### 3. **🔧 Easy Extension Pattern**
- New entities follow the same partial class structure
- Add `CoreService.Team.cs`, `CoreService.Match.cs`, etc.
- Consistent patterns across all entity types
- No boilerplate service registration

#### 4. **🚀 Direct Instantiation Benefits**
- No runtime dependency injection overhead
- Clear service initialization flow
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
- ✅ CoreService.cs with basic orchestration
- ✅ CoreService.Data.cs with essential CRUD generics
- ✅ CoreService.Player.cs with direct generic usage
- ✅ Empty validation and data operations files (add when needed)

**Phase 2 - Iterative Enhancement:**
- 🔄 Add player-specific data queries only when required
- 🔄 Add validation methods only when business rules emerge
- 🔄 Add event publishing only when inter-service communication needed
- 🔄 Add new entity support following the established pattern

### Success Metrics

The CoreService refactor succeeds when:
- ✅ All entity operations work through a single service instance
- ✅ Code is organized with clear separation of concerns
- ✅ New entity support requires minimal boilerplate
- ✅ Direct instantiation eliminates DI complexity
- ✅ Event messaging enables loose coupling
- ✅ No speculative code exists - everything serves a current need

**This partial class CoreService architecture establishes our unified, maintainable entity management foundation!** 🎯
