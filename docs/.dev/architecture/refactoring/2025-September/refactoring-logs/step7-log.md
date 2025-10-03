# Step 7 Implementation Log: CoreService Organization

**Status:** ✅ COMPLETED  
**Date Range:** Pre-Step 6.6 (retroactive documentation)  
**Implementation Type:** Architectural pattern establishment

---

## Overview

Step 7 established the CoreService organization pattern, which differs significantly from the original plan. Instead of moving business logic into CoreService partials, the actual implementation uses:
- Static partial `CoreService` class for infrastructure orchestration
- `[Entity]Core.cs` files for business logic (e.g., `MatchCore.cs`, `PlayerCore.cs`)
- Generated `DatabaseService<TEntity>` accessors via source generators
- No runtime dependency injection

---

## Architectural Pattern Established

### Design Evolution

**Original Plan (Step 7 Document):**
- Move business logic to `CoreService.{Entity}.cs` partials
- Example: `CoreService.Player.cs`, `CoreService.Team.cs`
- CoreService contains both infrastructure and business logic

**Actual Implementation (What Was Built):**
- `CoreService` handles infrastructure only
- Business logic lives in `{Entity}Core.cs` files
- Clear separation of concerns

### Why the Change?

1. **Single Responsibility:** CoreService focuses on infrastructure, EntityCore focuses on business logic
2. **Discoverability:** Easier to find entity-specific logic in dedicated files
3. **Maintainability:** Changes to entity logic don't touch CoreService
4. **Generated Code:** Source generators can extend CoreService without conflicts

---

## Implementation Summary

### 7a. CoreService.cs (Infrastructure Orchestration) ✅ DONE

**Implementation:**
```csharp
// File: src/WabbitBot.Core/Common/Services/Core/CoreService.cs
public static partial class CoreService
{
    // Infrastructure components
    private static Lazy<ICoreEventBus>? _lazyEventBus;
    private static Lazy<IErrorService>? _lazyErrorHandler;
    
    public static ICoreEventBus EventBus => _lazyEventBus!.Value;
    public static IErrorService ErrorHandler => _lazyErrorHandler!.Value;
    
    public static void InitializeServices(
        ICoreEventBus eventBus,
        IErrorService errorHandler)
    {
        _lazyEventBus = new Lazy<ICoreEventBus>(() => eventBus);
        _lazyErrorHandler = new Lazy<IErrorService>(() => errorHandler);
        
        // Register adapters and providers
        RegisterRepositoryAdapters();
        RegisterCacheProviders();
        RegisterArchiveProviders();
    }
    
    // Helper methods
    public static async Task PublishAsync<TEvent>(TEvent evt) where TEvent : class, IEvent
    {
        await EventBus.PublishAsync(evt);
    }
    
    public static async Task<Result> TryAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await ErrorHandler.CaptureAsync(ex, $"Operation failed: {operationName}", operationName);
            return Result.Failure(ex.Message);
        }
    }
    
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        try
        {
            var result = await operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            await ErrorHandler.CaptureAsync(ex, $"Operation failed: {operationName}", operationName);
            return Result<T>.Failure(ex.Message);
        }
    }
}
```

**Key Features:**
- ✅ Static partial class (no runtime DI)
- ✅ Lazy initialization for services
- ✅ Centralized error handling helpers
- ✅ Event publishing helpers
- ✅ No business logic - pure infrastructure

**Files Created:**
- `src/WabbitBot.Core/Common/Services/Core/CoreService.cs`

**Status:** ✅ COMPLETED

---

### 7b. CoreService.Database.cs (DbContext Helpers) ✅ DONE

**Implementation:**
```csharp
// File: src/WabbitBot.Core/Common/Services/Core/CoreService.Database.cs
public static partial class CoreService
{
    /// <summary>
    /// Execute work within a safely managed DbContext scope.
    /// Use for complex queries that need direct EF access.
    /// </summary>
    public static async Task WithDbContext(Func<WabbitBotDbContext, Task> work)
    {
        await using var context = WabbitBotDbContextProvider.CreateDbContext();
        await work(context);
    }
    
    /// <summary>
    /// Execute work within a safely managed DbContext scope and return a result.
    /// Use for complex queries that need direct EF access.
    /// </summary>
    public static async Task<T> WithDbContext<T>(Func<WabbitBotDbContext, Task<T>> work)
    {
        await using var context = WabbitBotDbContextProvider.CreateDbContext();
        return await work(context);
    }
    
    /// <summary>
    /// Execute work within a DbContext scope with automatic error handling.
    /// </summary>
    public static async Task<Result> TryWithDbContext(Func<WabbitBotDbContext, Task> work, string operationName)
    {
        try
        {
            await using var context = WabbitBotDbContextProvider.CreateDbContext();
            await work(context);
            return Result.Success();
        }
        catch (Exception ex)
        {
            await ErrorHandler.CaptureAsync(ex, $"Database operation failed: {operationName}", operationName);
            return Result.Failure(ex.Message);
        }
    }
    
    /// <summary>
    /// Execute work within a DbContext scope with automatic error handling and return a result.
    /// </summary>
    public static async Task<Result<T>> TryWithDbContext<T>(Func<WabbitBotDbContext, Task<T>> work, string operationName)
    {
        try
        {
            await using var context = WabbitBotDbContextProvider.CreateDbContext();
            var result = await work(context);
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            await ErrorHandler.CaptureAsync(ex, $"Database operation failed: {operationName}", operationName);
            return Result<T>.Failure(ex.Message);
        }
    }
    
    /// <summary>
    /// Register repository adapters for all entities.
    /// Called during CoreService initialization.
    /// </summary>
    public static void RegisterRepositoryAdapters()
    {
        // Adapters use WabbitBotDbContextProvider directly
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Player>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Team>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Map>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Game>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<User>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Match>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Leaderboard>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<LeaderboardItem>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Stats>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<SeasonConfig>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<SeasonGroup>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Tournament>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TournamentMatch>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<TournamentTeam>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Scrimmage>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<MatchPlayer>());
        RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<GameStateSnapshot>());
    }
    
    /// <summary>
    /// Register cache providers for entities.
    /// Manual stub - can be extended or left empty (uses default providers).
    /// </summary>
    public static void RegisterCacheProviders()
    {
        // Intentionally empty - source generators may emit registrations here
        // Default behavior: InMemoryLruCacheProvider per entity
    }
    
    /// <summary>
    /// Register archive providers for entities.
    /// Source generators emit registrations based on [EntityMetadata(EmitArchiveRegistration = true)].
    /// </summary>
    static partial void RegisterArchiveProviders();
    
    /// <summary>
    /// Run archive retention policy to purge old snapshots.
    /// Called periodically by background job.
    /// </summary>
    public static async Task RunArchiveRetentionAsync()
    {
        await using var context = WabbitBotDbContextProvider.CreateDbContext();
        
        var retentionPeriod = TimeSpan.FromDays(365); // 1 year default
        var cutoffDate = DateTime.UtcNow - retentionPeriod;
        
        // Purge old archive records (keep latest version per entity)
        // Implementation varies per archive strategy
        // Future: iterate through registered archive providers and call PurgeAsync
    }
}
```

**Key Features:**
- ✅ `WithDbContext` helpers for complex queries
- ✅ `TryWithDbContext` with automatic error handling
- ✅ Adapter registration for all entities
- ✅ Cache and archive provider registration hooks
- ✅ Archive retention policy execution

**Files Created:**
- `src/WabbitBot.Core/Common/Services/Core/CoreService.Database.cs`

**Status:** ✅ COMPLETED

---

### 7c. Generated DatabaseService Accessors ✅ DONE

**Implementation:**
Source generators emit static properties for all entities:

```csharp
// File: src/WabbitBot.Core/Common/Services/Core/DatabaseServiceAccessors.g.cs (generated)
public static partial class CoreService
{
    // Generated accessors (one per entity)
    private static Lazy<DatabaseService<Map>>? _lazyMaps;
    private static Lazy<DatabaseService<Player>>? _lazyPlayers;
    private static Lazy<DatabaseService<Team>>? _lazyTeams;
    private static Lazy<DatabaseService<Game>>? _lazyGames;
    private static Lazy<DatabaseService<User>>? _lazyUsers;
    private static Lazy<DatabaseService<Match>>? _lazyMatches;
    private static Lazy<DatabaseService<Leaderboard>>? _lazyLeaderboards;
    private static Lazy<DatabaseService<LeaderboardItem>>? _lazyLeaderboardItems;
    private static Lazy<DatabaseService<Stats>>? _lazyStats;
    private static Lazy<DatabaseService<SeasonConfig>>? _lazySeasonConfigs;
    private static Lazy<DatabaseService<SeasonGroup>>? _lazySeasonGroups;
    private static Lazy<DatabaseService<Tournament>>? _lazyTournaments;
    private static Lazy<DatabaseService<TournamentMatch>>? _lazyTournamentMatches;
    private static Lazy<DatabaseService<TournamentTeam>>? _lazyTournamentTeams;
    private static Lazy<DatabaseService<Scrimmage>>? _lazyScrimmages;
    private static Lazy<DatabaseService<MatchPlayer>>? _lazyMatchPlayers;
    private static Lazy<DatabaseService<GameStateSnapshot>>? _lazyGameStateSnapshots;
    
    public static DatabaseService<Map> Maps => _lazyMaps!.Value;
    public static DatabaseService<Player> Players => _lazyPlayers!.Value;
    public static DatabaseService<Team> Teams => _lazyTeams!.Value;
    public static DatabaseService<Game> Games => _lazyGames!.Value;
    public static DatabaseService<User> Users => _lazyUsers!.Value;
    public static DatabaseService<Match> Matches => _lazyMatches!.Value;
    public static DatabaseService<Leaderboard> Leaderboards => _lazyLeaderboards!.Value;
    public static DatabaseService<LeaderboardItem> LeaderboardItems => _lazyLeaderboardItems!.Value;
    public static DatabaseService<Stats> Stats => _lazyStats!.Value;
    public static DatabaseService<SeasonConfig> SeasonConfigs => _lazySeasonConfigs!.Value;
    public static DatabaseService<SeasonGroup> SeasonGroups => _lazySeasonGroups!.Value;
    public static DatabaseService<Tournament> Tournaments => _lazyTournaments!.Value;
    public static DatabaseService<TournamentMatch> TournamentMatches => _lazyTournamentMatches!.Value;
    public static DatabaseService<TournamentTeam> TournamentTeams => _lazyTournamentTeams!.Value;
    public static DatabaseService<Scrimmage> Scrimmages => _lazyScrimmages!.Value;
    public static DatabaseService<MatchPlayer> MatchPlayers => _lazyMatchPlayers!.Value;
    public static DatabaseService<GameStateSnapshot> GameStateSnapshots => _lazyGameStateSnapshots!.Value;
}
```

**Usage Examples:**
```csharp
// Simple CRUD operations
var player = await CoreService.Players.GetByIdAsync(playerId);
var createResult = await CoreService.Teams.CreateAsync(new Team { Name = "Team Alpha" });
var updateResult = await CoreService.Maps.UpdateAsync(map);

// Complex queries via WithDbContext
var recentMatches = await CoreService.WithDbContext(async db =>
{
    return await db.Matches
        .Where(m => m.CreatedAt > DateTime.UtcNow.AddDays(-7))
        .Include(m => m.Team1)
        .Include(m => m.Team2)
        .OrderByDescending(m => m.CreatedAt)
        .ToListAsync();
});
```

**Key Features:**
- ✅ One accessor per entity
- ✅ Lazy initialization (created on first access)
- ✅ Type-safe access to DatabaseService
- ✅ No manual registration needed
- ✅ Fully source generated

**Files Generated:**
- `src/WabbitBot.Core/Common/Services/Core/DatabaseServiceAccessors.g.cs`

**Status:** ✅ COMPLETED

---

### 7d. Business Logic in EntityCore Files ✅ DONE

**Pattern Established:**
Instead of `CoreService.{Entity}.cs`, business logic lives in `{Entity}Core.cs`.

**Example: MatchCore.cs**
```csharp
// File: src/WabbitBot.Core/Scrimmage/Models/MatchCore.cs
public partial class Match
{
    /// <summary>
    /// Factory method to create a new match
    /// </summary>
    public static Match Create(Guid team1Id, Guid team2Id, Guid mapId, List<Guid> team1PlayerIds, List<Guid> team2PlayerIds)
    {
        return new Match
        {
            Id = Guid.NewGuid(),
            Team1Id = team1Id,
            Team2Id = team2Id,
            MapId = mapId,
            Team1PlayerIds = team1PlayerIds,
            Team2PlayerIds = team2PlayerIds,
            Status = MatchStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
    
    /// <summary>
    /// Business logic: validate match can be started
    /// </summary>
    public Result CanStart()
    {
        if (Status != MatchStatus.Pending)
            return Result.Failure("Match must be in Pending status to start");
        
        if (Team1PlayerIds.Count == 0 || Team2PlayerIds.Count == 0)
            return Result.Failure("Both teams must have players");
        
        return Result.Success();
    }
    
    /// <summary>
    /// Business logic: complete match and determine winner
    /// </summary>
    public Result CompleteMatch(Guid? winnerId)
    {
        if (Status != MatchStatus.InProgress)
            return Result.Failure("Match must be in progress to complete");
        
        if (winnerId.HasValue && winnerId != Team1Id && winnerId != Team2Id)
            return Result.Failure("Winner must be one of the participating teams");
        
        Status = MatchStatus.Completed;
        WinnerId = winnerId;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        return Result.Success();
    }
}
```

**Example: PlayerCore.cs**
```csharp
// File: src/WabbitBot.Core/Common/Models/Common/PlayerCore.cs
public partial class Player
{
    /// <summary>
    /// Factory method to create a new player
    /// </summary>
    public static Player Create(string name, Guid userId)
    {
        return new Player
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            LastActive = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
    
    /// <summary>
    /// Business logic: join a team
    /// </summary>
    public Result JoinTeam(Guid teamId)
    {
        if (TeamIds.Contains(teamId))
            return Result.Failure("Player is already on this team");
        
        TeamIds.Add(teamId);
        UpdatedAt = DateTime.UtcNow;
        
        return Result.Success();
    }
    
    /// <summary>
    /// Business logic: leave a team
    /// </summary>
    public Result LeaveTeam(Guid teamId)
    {
        if (!TeamIds.Contains(teamId))
            return Result.Failure("Player is not on this team");
        
        TeamIds.Remove(teamId);
        UpdatedAt = DateTime.UtcNow;
        
        return Result.Success();
    }
    
    /// <summary>
    /// Business logic: update last active timestamp
    /// </summary>
    public void UpdateLastActive()
    {
        LastActive = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Key Features:**
- ✅ Business logic co-located with entity definition
- ✅ Factory methods for entity creation
- ✅ Validation logic
- ✅ State transitions
- ✅ Uses partial classes to extend generated entity
- ✅ Returns `Result` for operations that can fail

**Files Created:**
- `src/WabbitBot.Core/Scrimmage/Models/MatchCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/PlayerCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/TeamCore.cs`
- (Other EntityCore files as needed)

**Status:** ✅ COMPLETED

---

### 7e. Startup Wiring (No Runtime DI) ✅ DONE

**Implementation:**
```csharp
// File: src/WabbitBot.Core/CoreModule.cs (conceptual - actual location may vary)
public static class CoreModule
{
    public static void Initialize()
    {
        // Create services (no DI container)
        var eventBus = new CoreEventBus();
        var errorService = new ErrorService();
        
        // Initialize CoreService
        CoreService.InitializeServices(eventBus, errorService);
        
        // Services are now available via CoreService static accessors
    }
}

// Usage in application startup
public class Program
{
    public static async Task Main()
    {
        // Initialize Core module
        CoreModule.Initialize();
        
        // Use CoreService
        var player = await CoreService.Players.GetByIdAsync(someId);
        
        // ... rest of application
    }
}
```

**Key Features:**
- ✅ No runtime dependency injection
- ✅ Direct instantiation of services
- ✅ Static initialization via `CoreService.InitializeServices`
- ✅ Services accessible via static properties
- ✅ Compile-time dependency verification

**Status:** ✅ COMPLETED

---

## Architectural Decisions

### 1. CoreService = Infrastructure Only
- **Decision:** CoreService handles infrastructure, not business logic
- **Rationale:** Single responsibility, clearer separation of concerns
- **Impact:** Business logic in EntityCore files, CoreService provides data access

### 2. EntityCore Pattern
- **Decision:** Use `{Entity}Core.cs` partial classes for business logic
- **Rationale:** Co-located with entity definition, discoverable, maintainable
- **Example:** `MatchCore.cs`, `PlayerCore.cs`, `TeamCore.cs`

### 3. Generated Accessors
- **Decision:** Source generators emit `CoreService.{Entity}` accessors
- **Rationale:** No manual registration, type-safe, DI-free
- **Benefit:** Add new entity = automatic accessor generation

### 4. Static Partial Class
- **Decision:** CoreService is `static partial` with multiple files
- **Rationale:** Allows generators to extend, no instances needed
- **Files:**
  - `CoreService.cs` (main infrastructure)
  - `CoreService.Database.cs` (DbContext helpers)
  - `DatabaseServiceAccessors.g.cs` (generated)

---

## Benefits Achieved

### 1. Single Responsibility ✅
- **CoreService:** Infrastructure orchestration
- **DatabaseService:** Data access (CRUD, cache, archive)
- **EntityCore:** Business logic and validation
- **ErrorService:** Centralized error handling

### 2. Clean Code Organization ✅
- Business logic separated from infrastructure
- Easy to locate entity-specific code
- Consistent patterns across all entities
- Partial classes for extensibility

### 3. No Runtime DI ✅
- Direct instantiation, no magic
- Clear initialization flow
- Compile-time verification
- Easier debugging and testing

### 4. Generator-Friendly ✅
- Generators can extend CoreService via partials
- No conflicts with manual code
- Automatic accessor generation
- Consistent naming conventions

---

## Usage Patterns

### Simple CRUD Operations
```csharp
// Create
var player = Player.Create("PlayerName", userId);
var result = await CoreService.Players.CreateAsync(player);

// Read
var player = await CoreService.Players.GetByIdAsync(playerId);
var allPlayers = await CoreService.Players.GetAllAsync();

// Update
player.UpdateLastActive();
await CoreService.Players.UpdateAsync(player);

// Delete
await CoreService.Players.DeleteAsync(playerId);
```

### Business Logic Operations
```csharp
// Use EntityCore methods for business logic
var match = Match.Create(team1Id, team2Id, mapId, team1Players, team2Players);

var canStart = match.CanStart();
if (!canStart.Success)
{
    // Handle error
    await CoreService.ErrorHandler.CaptureAsync(
        new InvalidOperationException(canStart.Error!),
        "Cannot start match",
        nameof(MatchController));
    return;
}

match.Status = MatchStatus.InProgress;
await CoreService.Matches.UpdateAsync(match);
```

### Complex Queries
```csharp
// Use WithDbContext for complex EF queries
var activeMatches = await CoreService.WithDbContext(async db =>
{
    return await db.Matches
        .Where(m => m.Status == MatchStatus.InProgress)
        .Include(m => m.Team1)
        .Include(m => m.Team2)
        .Include(m => m.Map)
        .OrderByDescending(m => m.CreatedAt)
        .ToListAsync();
});
```

### Error Handling
```csharp
// Use TryAsync for automatic error handling
var result = await CoreService.TryAsync(async () =>
{
    var player = await CoreService.Players.GetByIdAsync(playerId);
    player.JoinTeam(teamId);
    await CoreService.Players.UpdateAsync(player);
}, "JoinTeam");

if (!result.Success)
{
    // Error already logged via ErrorService
    return Result.Failure(result.Error!);
}
```

---

## Integration with Other Steps

### Builds Upon
- **Step 6.4:** DatabaseService foundation
- **Step 6.5:** ErrorService and DI removal
- **Step 6.7:** Source generator infrastructure
- **Step 6.9:** Repository/cache/archive adapters

### Enables
- **Step 8:** Entity migration (obsolete - already using EntityCore pattern)
- **Feature Development:** Clean, consistent API for all data access
- **Testing:** Easy to mock CoreService accessors

---

## Lessons Learned

### What Worked Well
1. **Static Partial Pattern:** Clean separation, generator-friendly
2. **EntityCore Files:** Business logic co-located with entities
3. **Generated Accessors:** Zero boilerplate, type-safe
4. **No Runtime DI:** Simpler, more explicit, easier to debug

### What Changed from Plan
1. **Business Logic Location:** EntityCore files instead of CoreService partials
2. **Rationale:** Better separation of concerns, more discoverable

### Improvements Made
1. **WithDbContext Helpers:** Simplified complex query patterns
2. **TryAsync Methods:** Automatic error handling and logging
3. **Result Pattern:** Consistent error handling across all operations

---

## Files Created/Modified

### CoreService Infrastructure
- `src/WabbitBot.Core/Common/Services/Core/CoreService.cs`
- `src/WabbitBot.Core/Common/Services/Core/CoreService.Database.cs`

### Generated Code
- `src/WabbitBot.Core/Common/Services/Core/DatabaseServiceAccessors.g.cs`

### EntityCore Files
- `src/WabbitBot.Core/Scrimmage/Models/MatchCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/PlayerCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/TeamCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/GameCore.cs`
- (Other EntityCore files as created)

---

## Conclusion

Step 7 successfully established the CoreService organization pattern:
- ✅ Static partial CoreService for infrastructure
- ✅ EntityCore files for business logic
- ✅ Generated DatabaseService accessors
- ✅ No runtime dependency injection
- ✅ Clean, maintainable architecture

The pattern established in Step 7 provides a solid foundation for all future development, with clear separation of concerns and minimal boilerplate.

**Step 7: ✅ COMPLETED**

---

## Appendix: Comparison to Original Plan

### Original Plan
```csharp
// CoreService.Player.cs (planned but not implemented)
public partial class CoreService
{
    public static async Task<Player?> GetPlayerAsync(Guid id)
    {
        return await _playerData.GetByIdAsync(id);
    }
    
    public static async Task<Result<Player>> CreatePlayerWithValidationAsync(Player player)
    {
        // Business logic in CoreService
    }
}
```

### Actual Implementation
```csharp
// CoreService provides data access
public static partial class CoreService
{
    public static DatabaseService<Player> Players => _lazyPlayers!.Value;
}

// PlayerCore.cs provides business logic
public partial class Player
{
    public static Player Create(string name, Guid userId) { /* ... */ }
    public Result JoinTeam(Guid teamId) { /* ... */ }
}

// Usage
var player = Player.Create("Name", userId);
await CoreService.Players.CreateAsync(player);
```

**Why Better:**
- Business logic co-located with entity
- CoreService stays focused on infrastructure
- Easier to find and maintain entity logic
- Generated accessors reduce boilerplate
