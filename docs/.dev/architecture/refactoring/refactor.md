# WabbitBot Architecture Refactor Plan

## Executive Summary

This refactor simplifies the over-engineered architecture by consolidating all core microservices into a single `CoreService` that uses entity configuration patterns. The goal is to reduce complexity while maintaining functionality, making the codebase more maintainable for a small to medium-sized Discord bot.

## Core Principles - DO NOT PREDICT FUTURE NEEDS

⚠️ **CRITICAL ARCHITECTURAL CONSTRAINT**: This application **explicitly avoids runtime dependency injection**. Services are created via direct instantiation, and the architecture uses vertical slice patterns, event messaging, and source generation instead.

⚠️ **CRITICAL**: This refactor follows a **lean implementation approach**. We will NOT try to predict or implement methods/features we might need in the future. Instead:

- **Implement only what is currently needed** - Start with minimal functionality
- **Add methods/features as they become required** - Driven by actual usage patterns
- **Avoid speculative implementation** - Don't assume validation rules, business logic, or methods
- **Keep it simple** - Use the partial class structure to add functionality only when needed
- **Eliminate thin wrappers** - Don't create methods that just call generic methods
- **Use strategic ID handling** - Mix `object`, `string`, and entity types appropriately

**Example of what NOT to do:**
- ❌ Don't implement `ValidatePlayerNameAsync()` with assumed rules (length limits, duplicate checks)
- ❌ Don't implement `ArchivePlayerAsync()` unless archiving is currently needed
- ❌ Don't implement complex business logic based on assumptions
- ❌ **Don't create ANY wrapper methods** - even with "consistent" names like `GetPlayerByIdAsync()`
- ❌ **Don't create ANY thin wrapper methods** - use generics directly: `GetByIdAsync(id, repository, cache)`
- ❌ **Don't create ANY method that just calls a generic method** - if no custom logic, don't create the method
- ❌ **Don't create validation wrapper methods** - standard methods already include validation

**Example of what TO do:**
- ✅ Implement basic CRUD operations for immediate needs
- ✅ Add specific validation only when validation errors occur
- ✅ Add archiving when the feature is actually requested
- ✅ Keep partial class files minimal until functionality is required
- ✅ **Use generic methods DIRECTLY** - `GetByIdAsync(id, repository, cache)` - NO wrappers!
- ✅ **Create specific methods ONLY when there's REAL business logic** (joins, complex operations, etc.)
- ✅ **Standard methods already include validation** - no need for validation wrappers
- ✅ **Strategic ID handling** - use appropriate types (`object`, `string`, or entities) per context
- ✅ **Keep services minimal** - don't add methods unless they provide actual value

## Current Architecture Problems

### Over-engineering Issues
- **8+ microservices** for basic CRUD operations
- **Entity-specific database classes** (PlayerRepository, PlayerCache, etc.)
- **Complex inheritance hierarchies** with multiple abstraction layers
- **Scattered business logic** across multiple service files
- **Configuration scattered** across different files

### Maintainability Issues
- **Too many files** to manage for simple operations
- **Code duplication** across similar entity services
- **Complex dependency chains** making changes difficult
- **Hard to understand** the flow for new developers

## New Simplified Architecture

### Core Principles
1. **Single CoreService** handles all core entities (Player, User, Team, Map)
2. **Entity Configuration Pattern** defines database mappings and settings
3. **Partial Class Organization** separates concerns across files
4. **Unified IDatabaseService Interface** with component-based operations
5. **Generic Database Components** eliminate entity-specific classes

### Architecture Overview

```
CoreService (Partial Classes)
├── CoreService.cs (Main service class)
├── CoreService.Data.cs (Data access operations)
├── CoreService.Player.cs (Player-specific business logic)
├── CoreService.Player.Data.cs (Player data operations)
└── CoreService.Player.Validation.cs (Player validation)

Entity Configurations
├── EntityConfig.cs (Base configuration)
├── PlayerDbConfig.cs (Player-specific config)
├── UserDbConfig.cs (User-specific config)
└── ...

Database Layer
├── IDatabaseService<TEntity> (Unified interface - implemented by services)
├── Repository (Configuration/properties only)
├── Cache (Configuration/properties only)
├── Archive (Configuration/properties only)
├── RepositoryService<TEntity> (Implements IDatabaseService - all methods)
├── CacheService<TEntity> (Implements IDatabaseService - all methods)
└── ArchiveService<TEntity> (Implements IDatabaseService - all methods)
```

## Entity Configuration Pattern

### Base Entity Configuration

```csharp
public abstract class EntityConfig<TEntity> where TEntity : IEntity
{
    public string TableName { get; }
    public string ArchiveTableName { get; }
    public string[] Columns { get; }
    public int MaxCacheSize { get; }
    public TimeSpan DefaultCacheExpiry { get; }

    protected EntityConfig(
        string tableName,
        string archiveTableName,
        string[] columns,
        int maxCacheSize = 1000,
        TimeSpan? defaultCacheExpiry = null)
    {
        TableName = tableName;
        ArchiveTableName = archiveTableName;
        Columns = columns;
        MaxCacheSize = maxCacheSize;
        DefaultCacheExpiry = defaultCacheExpiry ?? TimeSpan.FromHours(1);
    }
}
```

### Example Player Configuration

```csharp
public class PlayerDbConfig : EntityConfig<Player>
{
    public PlayerDbConfig() : base(
        tableName: "players",
        archiveTableName: "player_archive",
        columns: new[] {
            "Id", "Name", "LastActive", "IsArchived", "ArchivedAt",
            "TeamIds", "PreviousUserIds", "CreatedAt", "UpdatedAt"
        },
        maxCacheSize: 500,
        defaultCacheExpiry: TimeSpan.FromMinutes(30))
    {
    }
}
```

**Note**: Entity classes do NOT include SchemaVersion properties. Schema versioning is handled at the database level through EF Core migrations, not at the entity level. This provides cleaner separation of concerns and better flexibility.

## Database Design - IDatabaseService Interface

### Unified Interface

```csharp
public interface IDatabaseService<TEntity> where TEntity : IEntity
{
    // Core methods with consistent naming
    Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component);
    Task<Result<TEntity>> UpdateAsync(TEntity entity, DatabaseComponent component);
    Task<Result<TEntity>> DeleteAsync(object id, DatabaseComponent component);
    Task<bool> ExistsAsync(object id, DatabaseComponent component);

    // Flexible ID lookup methods
    Task<TEntity?> GetByIdAsync(object id, DatabaseComponent component);
    Task<TEntity?> GetByStringIdAsync(string id, DatabaseComponent component);

    // Query methods
    Task<IEnumerable<TEntity>> GetAllAsync(DatabaseComponent component);
    Task<TEntity?> GetByNameAsync(string name, DatabaseComponent component);
    Task<IEnumerable<TEntity>> GetByDateRangeAsync(DateTime start, DateTime end, DatabaseComponent component);
    Task<IEnumerable<TEntity>> QueryAsync(string whereClause, object? parameters, DatabaseComponent component);
}

public enum DatabaseComponent
{
    Repository,  // Persistent storage
    Cache,       // In-memory cache
    Archive      // Historical data
}
```

### Clean Separation: Component Classes vs Service Classes

**⚠️ CRITICAL ARCHITECTURAL DECISION**: **Component classes contain ONLY configuration and properties. Service classes contain ALL methods.**

#### Component Classes (Simple Configuration)
```csharp
// Repository.cs - Configuration and properties ONLY
public abstract class Repository<TEntity> where TEntity : IEntity
{
    protected readonly IDatabaseConnection _connection;
    protected readonly string _tableName;
    protected readonly string[] _columns;
    protected readonly string _idColumn;

    // NO METHODS - Just configuration!
    // All methods moved to RepositoryService<TEntity>
}

// Cache.cs - Configuration and properties ONLY
public class Cache<TEntity> where TEntity : IEntity
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly int _maxSize;

    // NO METHODS - Just configuration!
    // All methods moved to CacheService<TEntity>
}

// Archive.cs - Configuration and properties ONLY
public abstract class Archive<TEntity> where TEntity : IEntity
{
    protected readonly IDatabaseConnection _connection;
    protected readonly string _tableName;
    protected readonly string[] _columns;

    // NO METHODS - Just configuration!
    // All methods moved to ArchiveService<TEntity>
}
```

#### Service Classes (All Methods and Logic)
```csharp
// RepositoryService<TEntity> - ALL methods and logic
public abstract class RepositoryService<TEntity> : IDatabaseService<TEntity>
    where TEntity : IEntity
{
    // Contains ALL IDatabaseService method implementations
    // Handles PostgreSQL operations, queries, CRUD, etc.
}

// CacheService<TEntity> - ALL methods and logic
public class CacheService<TEntity> : IDatabaseService<TEntity>
    where TEntity : IEntity
{
    // Contains ALL IDatabaseService method implementations
    // Handles in-memory caching, eviction, TTL, etc.
}

// ArchiveService<TEntity> - ALL methods and logic
public abstract class ArchiveService<TEntity> : IDatabaseService<TEntity>
    where TEntity : IEntity
{
    // Contains ALL IDatabaseService method implementations
    // Handles historical data storage and retrieval
}
```

### Why This Separation Matters

1. **Single Responsibility**: Components = configuration, Services = operations
2. **No Duplication**: Methods exist in ONE place only
3. **Clean Dependencies**: Services depend on components, not vice versa
4. **Testability**: Easy to mock configuration vs test operations
5. **Maintainability**: Changes to operations don't affect configuration

### Database Schema Impact

#### Current Table (SQLite-style):
```sql
-- ❌ OLD: Manual JSON columns
CREATE TABLE players (
    Id UUID PRIMARY KEY,
    Name VARCHAR(255),
    TeamIdsJson TEXT,  -- Manual JSON string
    StatsJson TEXT     -- Manual JSON string
);
```

#### New Table (PostgreSQL JSONB):
```sql
-- ✅ NEW: Native JSONB columns
CREATE TABLE players (
    Id UUID PRIMARY KEY,
    Name VARCHAR(255),
    TeamMemberships JSONB,  -- Native JSONB array
    Stats JSONB,           -- Native JSONB object
    Metadata JSONB         -- Flexible JSONB data
);

-- JSONB indexes for performance
CREATE INDEX idx_players_team_memberships ON players USING GIN (TeamMemberships);
CREATE INDEX idx_players_stats ON players USING GIN (Stats);
```

### Migration Benefits

1. **🚀 Performance**: Native JSONB operations are faster than manual serialization
2. **🔒 Type Safety**: Strongly-typed complex objects instead of string manipulation
3. **🛠️ Rich Queries**: LINQ support for JSON operations
4. **📈 Scalability**: PostgreSQL optimizes JSONB queries
5. **🧹 Clean Code**: No manual JSON serialization/deserialization
6. **🔧 Flexibility**: Easy to add new properties without schema changes

### Example JSONB Queries

```csharp
// Native JSON queries with Npgsql
var playersInTeam = await _dbContext.Players
    .Where(p => p.TeamMemberships.Any(tm => tm.TeamId == teamId))
    .ToListAsync();

// JSON path queries
var playersWithHighScore = await _dbContext.Players
    .Where(p => p.Stats.GamesPlayed > 100)
    .ToListAsync();
```

**This entity redefinition is a game-changer!** Since there's no existing data to migrate, we can design entities that fully leverage PostgreSQL's JSON capabilities from the ground up. The result will be cleaner code, better performance, and more powerful queries.

## CoreService Organization

### Main CoreService Class

**File:** `src/WabbitBot.Core/Common/Services/Core/CoreService.cs`

```csharp
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

### Data Operations Partial Class

**File:** `src/WabbitBot.Core/Common/Services/Core/CoreService.Data.cs`

```csharp
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

### Player-Specific Partial Class

**File:** `src/WabbitBot.Core/Common/Services/Core/Player/CoreService.Player.cs`

```csharp
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

**⚠️ KEY DECISION**: **NO Pseudonym Methods - Use Generics Directly**
- **DON'T** create `GetPlayerByIdAsync()` that just calls `GetByIdAsync()`
- **DON'T** use confusing pseudonyms that hide the fact that it's generic
- **DO** call generic methods directly: `GetByIdAsync(id, repository, cache)`
- **DO** create specific methods ONLY when they have unique business logic or validation

### Player Data Operations

**File:** `src/WabbitBot.Core/Common/Services/Core/Player/CoreService.Player.Data.cs`

```csharp
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

**⚠️ REMEMBER**: This data operations file should contain only the methods that are immediately needed. Complex operations like archiving, advanced queries, or bulk operations should only be added when they become required through actual usage patterns.

### Player Validation

**File:** `src/WabbitBot.Core/Common/Services/Core/Player/CoreService.Player.Validation.cs`

```csharp
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

**⚠️ REMEMBER**: This validation file should remain mostly empty until specific validation requirements are identified through actual usage. Don't predict what validation might be needed.

## Migration Strategy

### 1: Entity Redefinition ✅ COMPLETE
1. ✅ Redefine entity classes for native PostgreSQL JSON support
2. ✅ Remove manual JSON properties from Player and Team entities
3. ✅ Enable Npgsql automatic mapping for complex objects

### 2: EF Core Foundation ✅ COMPLETE
1. ✅ Add Npgsql.EntityFrameworkCore.PostgreSQL package
2. ✅ Create WabbitBotDbContext with JSONB configurations
3. ✅ Configure entity mappings for JSONB columns
4. ✅ Set up connection string management

### 3: Database Layer Refinement ✅ COMPLETE
1. ✅ Implement `IDatabaseService<TEntity>` interface
2. ✅ Update generic `RepositoryService<TEntity>` with EF Core automatic mapping
3. ✅ Update generic `CacheService<TEntity>`
4. ✅ Update generic `ArchiveService<TEntity>`
5. ✅ Remove MapEntity/BuildParameters methods from component classes
6. ✅ Implement entity configuration pattern

### 4: Schema Migration ⭐ CURRENT
1. ⏳ **Implement database versioning strategy**
2. ⏳ **Handle schema migration scripts**
3. ⏳ **Add JSONB indexes for performance optimization**
4. ⏳ **Update table structures for native JSON support**
5. ⏳ **Migrate existing data (if any)**

### 5: Additional Entity Integration ✅ COMPLETE
1. ✅ **Integrate Stats entity** (remove IJsonVersioned, inherit from Entity)
2. ✅ **Integrate SeasonConfig entity** (runtime season configuration)
3. ✅ **Integrate SeasonGroup entity** (season grouping)
4. ✅ **Integrate LeaderboardItem entity** (extract from Leaderboard)
5. ✅ **Handle IJsonVersioned interface removal**

### 5.5: Re-evaluate ListWrapper Classes in PostgreSQL/EF Core Architecture ✅ COMPLETE

#### Critical Analysis: Do ListWrapper Classes Still Make Sense?

**Given our new architecture with PostgreSQL + EF Core + Npgsql, what role (if any) should ListWrapper classes play?**

#### PostgreSQL + EF Core Capabilities
PostgreSQL with EF Core and Npgsql provides:
- **Native JSONB operations** - Store/retrieve complex objects efficiently
- **LINQ-to-SQL translation** - Complex queries executed at database level
- **Efficient filtering/sorting** - No need for in-memory collections for basic operations
- **Complex aggregations** - Database-level computations for rankings, statistics

#### What ListWrapper Classes Were Originally For
- **Thread-safe collections** - EF Core handles concurrency
- **Complex filtering** - PostgreSQL/LINQ can do this efficiently
- **Business logic** - Should live in CoreService
- **Caching** - EF Core has built-in caching + change tracking

#### Proposed Decision: **ELIMINATE ListWrapper Classes**

**❌ Why eliminate them:**
1. **Redundancy**: EF Core + PostgreSQL handles most of what they did
2. **Performance**: Database queries are often faster than in-memory operations
3. **Complexity**: Adding unnecessary abstraction layer
4. **Maintenance**: One less layer to maintain and test

**✅ What moves to CoreService:**
```csharp
public partial class CoreService
{
    // Complex business logic that can't be efficiently done in SQL
    public async Task<IEnumerable<LeaderboardItem>> GetTopRankingsAsync(GameSize gameSize, int count = 10)
    {
        // Use EF Core for complex queries
        return await _dbContext.Leaderboards
            .Where(l => l.Rankings.ContainsKey(gameSize))
            .SelectMany(l => l.Rankings[gameSize].Values)
            .OrderByDescending(e => e.Rating)
            .Take(count)
            .ToListAsync();
    }

    // Caching expensive computations
    private readonly ConcurrentDictionary<string, CachedResult> _computationCache = new();

    public async Task<ComplexRankingResult> GetComplexRankingsAsync()
    {
        var cacheKey = "complex_rankings";
        if (_computationCache.TryGetValue(cacheKey, out var cached))
        {
            return cached.Result;
        }

        // Expensive computation
        var result = await ComputeComplexRankingsAsync();

        // Cache result
        _computationCache[cacheKey] = new CachedResult(result, DateTime.UtcNow.AddHours(1));
        return result;
    }
}
```

#### Implementation Plan

##### **Step 1: Eliminate Listwrappers and related methods/logic**
```csharp
// Eliminate:
// ❌ Simple filtering (EF Core LINQ)
// ❌ Basic CRUD (EF Core handles this)
// ❌ Thread-safe wrappers (EF Core handles this)
```

##### **Step 2: Add Strategic Caching Where Needed**
```csharp
public partial class CoreService
{
    private readonly ConcurrentDictionary<string, CachedComputation> _computationCache = new();

    public async Task<ExpensiveResult> GetExpensiveComputationAsync()
    {
        var cacheKey = "expensive_calculation";
        if (_computationCache.TryGetValue(cacheKey, out var cached) &&
            !cached.IsExpired)
        {
            return cached.Result;
        }

        var result = await ComputeExpensiveResultAsync();
        _computationCache[cacheKey] = new CachedComputation(result, DateTime.UtcNow.AddHours(1));
        return result;
    }
}
```

#### Benefits of This Approach

1. **🎯 Simplicity**: Single source of truth in CoreService
2. **🚀 Performance**: Leverage PostgreSQL's query optimization
3. **🧪 Testability**: Business logic in one place
4. **🔧 Maintainability**: No duplicate abstraction layers
5. **📈 Scalability**: Database handles complex operations efficiently

#### Final Decision: **ListWrapper classes add unnecessary complexity in the PostgreSQL/EF Core paradigm.**

### 6: JSONB Schema Migration ✅ COMPLETE
1. ⏳ Implement EF Core migrations strategy
2. ⏳ Update database schema to use JSONB columns
3. ⏳ Add JSONB indexes for performance optimization
4. ⏳ Update table structures for native JSON support
5. ⏳ **[📋 Migration Strategy Documented](./database-migration-strategy.md)**

### 6.5: Application & Database Versioning Strategy ⭐ CURRENT
1. ⏳ **Implement Loose Coupling Versioning** (independent app/schema evolution)
2. ⏳ **Create Application Version Tracking** (ApplicationInfo class)
3. ⏳ **Implement Schema Version Tracking** (SchemaVersionTracker class)
4. ⏳ **Add Version Compatibility Checking** (ApplicationVersionChecker)
5. ⏳ **Create Feature Flags System** (FeatureManager for gradual rollouts)
6. ⏳ **Implement Version Metadata Table** (schema_metadata for audit trail)
7. ⏳ **Add Version Drift Monitoring** (alerting for incompatible combinations)
8. ⏳ **Create Compatibility Test Suite** (VersionCompatibilityTests)

### 7: CoreService Organization
1. ⏳ Create main `CoreService.cs`
2. ⏳ Create `CoreService.Data.cs` with generic operations
3. ⏳ Create `CoreService.Player.cs` with business logic
4. ⏳ Create `CoreService.Player.Data.cs` with data operations
5. ⏳ Create `CoreService.Player.Validation.cs` with validation
6. ⏳ Set up direct instantiation and event messaging (no runtime DI)

### 8: Entity Migration
1. ⏳ Migrate Player entity operations
2. ⏳ Migrate User entity operations
3. ⏳ Migrate Team entity operations
4. ⏳ Migrate Map entity operations
5. ⏳ Update all calling code

### 9: Testing & Cleanup
1. ⏳ Update unit tests
2. ⏳ Integration testing
3. ⏳ Performance testing
4. ⏳ Remove old entity-specific services
5. ⏳ Update documentation
6. ⏳ Remove SchemaVersion properties from all entity configurations

## Benefits of New Architecture

### Simplicity
- **Single CoreService** instead of 8+ microservices
- **Entity configuration** eliminates hardcoded database mappings
- **Partial classes** organize code without complexity
- **Generic database components** reduce boilerplate

### Maintainability
- **Unified interface** for all data operations
- **Consistent patterns** across all entities
- **Configuration-driven** database mappings
- **Clear separation of concerns** with partial classes

### Performance
- **Component-based operations** allow targeted caching strategies
- **Generic implementations** reduce memory footprint
- **Flexible caching** per entity type
- **Efficient database queries** with PostgreSQL JSON support

### Developer Experience
- **Easy to add new entities** with configuration pattern
- **Consistent API** across all entity types
- **Clear file organization** with partial classes
- **Reduced cognitive load** with simpler architecture

## Database Schema Changes

### Unified Table Structure
All entity tables follow the same pattern:

```sql
CREATE TABLE players (
    Id UUID PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    LastActive TIMESTAMP NOT NULL,
    IsArchived BOOLEAN NOT NULL DEFAULT FALSE,
    ArchivedAt TIMESTAMP NULL,
    TeamIdsJson JSONB,
    PreviousUserIdsJson JSONB,
    CreatedAt TIMESTAMP NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL
);

-- Archive tables for historical data
CREATE TABLE player_archive (
    -- Same structure as players table
    ArchivedAt TIMESTAMP NOT NULL DEFAULT NOW()
);
```

### Indexing Strategy
```sql
-- Standard indexes for all entities
CREATE INDEX idx_players_name ON players(Name);
CREATE INDEX idx_players_created_at ON players(CreatedAt);
CREATE INDEX idx_players_is_archived ON players(IsArchived);

-- JSON indexes for complex queries
CREATE INDEX idx_players_team_ids ON players USING GIN (TeamIdsJson);
CREATE INDEX idx_players_previous_user_ids ON players USING GIN (PreviousUserIdsJson);
```

## PostgreSQL JSON Strategy - Use Npgsql Library

### Current Issue
❌ **Current approach**: Using `System.Text.Json` + manual JSON handling
❌ **Problem**: Missing out on PostgreSQL's native JSON performance

## ID Handling Strategy - Objects vs Strings

### Current Question
🤔 **Do we still need string IDs when using PostgreSQL JSON?**

### Answer: **Yes, but strategically**

**When to use `object` (flexible IDs):**
```csharp
// ✅ GOOD: Repository layer uses object for flexibility
public async Task<Player?> GetByIdAsync(object id, DatabaseComponent component)

// ✅ GOOD: Supports different ID types (string, int, Guid)
var player1 = await GetByIdAsync("player-123", DatabaseComponent.Repository);
var player2 = await GetByIdAsync(123, DatabaseComponent.Repository);
```

**When to use `string` (specific business methods):**
```csharp
// ✅ GOOD: Business layer can be specific
public async Task<Player?> GetPlayerByDiscordIdAsync(string discordId)

// ✅ GOOD: Clear intent with strongly-typed IDs
public async Task<Team?> GetTeamByIdAsync(string teamId)
```

**When to use entity objects directly:**
```csharp
// ✅ GOOD: PostgreSQL JSON allows direct object operations
public async Task<Result<Player>> CreatePlayerAsync(Player player)
public async Task<Result<Player>> UpdatePlayerAsync(Player player)

// ✅ GOOD: Complex operations work with full objects
public async Task<Result<Match>> ProcessMatchResultAsync(Match match, MatchResult result)
```

## Why We Still Need String Properties - Detailed Explanation

### The Question: Why Strings When PostgreSQL Has JSON?

Even with PostgreSQL's powerful JSON support, we **still need string parameters** for many practical reasons:

### 1. **Cache Keys Are Always Strings** 🔑
```csharp
// Cache keys must be strings for performance
_playerCacheData.GetAsync("player:123"); // ✅ String key
_playerCacheData.GetAsync(player.Id.ToString()); // ✅ Convert to string
```

**Why?** Cache systems (Redis, in-memory) use string keys for:
- Hash-based lookups (O(1) performance)
- Memory efficiency
- Serialization compatibility
- Interoperability between systems

### 2. **Database Foreign Keys Are Stored as Strings** 🔗
```csharp
// Foreign key relationships in JSON
{
  "playerId": "string-value",     // ✅ String reference
  "teamId": "string-value",       // ✅ String reference
  "matchId": "string-value"       // ✅ String reference
}
```

**Why?**
- JSON doesn't enforce referential integrity like traditional FKs
- Easier to update references without cascading updates
- Better for eventual consistency patterns
- Simpler for data migration and schema changes

### 3. **API and Network Efficiency** 📡
```csharp
// REST API calls - strings are more efficient
GET /api/players/player-123    // ✅ String ID in URL
GET /api/players/{playerId}    // ✅ String parameter

// WebSocket messages - smaller payload
{
  "action": "getPlayer",
  "playerId": "string-id"       // ✅ Compact string
}
```

**Why?**
- HTTP URLs, headers, and query params are strings
- Smaller network payloads (IDs vs full objects)
- Easier caching at network level
- Standard web API patterns

### 4. **Query Performance and Flexibility** 🔍
```csharp
// String-based queries are more flexible
var players = await QueryAsync(
    "Name LIKE @pattern AND CreatedAt > @date",
    new { pattern = "%john%", date = "2024-01-01" },
    DatabaseComponent.Repository
);
```

**Why?**
- Dynamic query building from user input (always strings)
- Partial matching and search functionality
- Integration with existing query builders
- Easier to construct complex WHERE clauses

### 5. **External System Integration** 🔌
```csharp
// Discord integration - IDs come as strings
var discordUserId = "123456789012345678"; // From Discord API
var player = await GetPlayerByDiscordIdAsync(discordUserId); // ✅ String

// File system operations
var fileName = "player-avatar-123.jpg"; // String-based naming
```

**Why?**
- External APIs (Discord, Steam, etc.) return string IDs
- File systems use string-based naming
- Configuration files store values as strings
- Integration with legacy systems

### 6. **Security and Validation** 🔒
```csharp
// Input validation - strings are safer
public async Task<Player?> GetPlayerByIdAsync(string playerId)
{
    if (!IsValidPlayerId(playerId)) return null; // ✅ Validate string
    // Prevent SQL injection, XSS, etc.
}
```

**Why?**
- Easier to validate and sanitize string inputs
- Protection against injection attacks
- Standard security practices for web applications
- Input normalization and canonicalization

### 7. **Performance Optimization Scenarios** ⚡
```csharp
// Sometimes we only need the ID, not the full object
var playerExists = await _repository.ExistsAsync(playerId, DatabaseComponent.Cache);

// Bulk operations with IDs only
var playerIds = new[] { "id1", "id2", "id3" };
await _cache.RemoveAsync(playerIds); // ✅ String array
```

**Why?**
- Reduced memory usage (ID vs full object)
- Faster operations when full data isn't needed
- Better cache performance
- More efficient bulk operations

### 8. **Debugging and Logging** 📋
```csharp
// Logging and debugging - strings are human-readable
_logger.LogInformation("Processing player {PlayerId}", playerId); // ✅ Clear logs
_logger.LogInformation("Processing player {@Player}", player); // ❌ Verbose
```

**Why?**
- Human-readable log entries
- Easier debugging and monitoring
- Smaller log file sizes
- Better performance for logging systems

## Strategic Approach to Parameter Types

### Updated IDatabaseService Interface (Clean Method Names)

```csharp
public interface IDatabaseService<TEntity> where TEntity : IEntity
{
    // Core methods with clean naming (no "Entity" prefix)
    Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component);
    Task<Result<TEntity>> UpdateAsync(TEntity entity, DatabaseComponent component);
    Task<Result<TEntity>> DeleteAsync(object id, DatabaseComponent component);
    Task<bool> ExistsAsync(object id, DatabaseComponent component);

    // Flexible ID lookup methods
    Task<TEntity?> GetByIdAsync(object id, DatabaseComponent component);
    Task<TEntity?> GetByStringIdAsync(string id, DatabaseComponent component);

    // Query methods
    Task<IEnumerable<TEntity>> GetAllAsync(DatabaseComponent component);
    Task<TEntity?> GetByNameAsync(string name, DatabaseComponent component);
    Task<IEnumerable<TEntity>> GetByDateRangeAsync(DateTime start, DateTime end, DatabaseComponent component);
    Task<IEnumerable<TEntity>> QueryAsync(string whereClause, object? parameters, DatabaseComponent component);
}
```

### How to Use These Methods - DIRECTLY, NO WRAPPERS!

```csharp
// ✅ CORRECT: Use generic methods DIRECTLY - NO wrappers!
public class SomeControllerOrService
{
    // Direct usage of generic methods - no wrapper methods needed
    public async Task DoSomethingWithPlayer()
    {
        // Direct usage of generic methods (clean names)
        var player1 = await GetByIdAsync("player-123", _repository, _cache);
        var player2 = await GetByStringIdAsync("player-456", _repository, _cache);
        var player3 = await GetByNameAsync("JohnDoe", _repository, _cache);

        // For Discord IDs - just use the generic string method
        var discordPlayer = await GetByStringIdAsync("123456789", _repository, _cache);

        // Create/Update use full entities
        var newPlayer = new Player { Name = "NewPlayer" };
        var result = await CreateAsync(newPlayer, _repository, _cache);
    }
}

// ONLY create methods when there's REAL business logic:
public class PlayerService
{
    // ✅ GOOD: Method with actual unique logic (joins, complex operations)
    public async Task<Player?> GetPlayerWithTeamInfoAsync(string playerId)
    {
        var player = await GetByStringIdAsync(playerId, _repository, _cache);
        if (player != null)
        {
            // Custom logic: load related team information
            var team = await GetByStringIdAsync(player.TeamId, _teamRepository, _teamCache);
            player.TeamName = team?.Name;
        }
        return player;
    }

    // ❌ BAD EXAMPLE REMOVED: CreatePlayerWithValidationAsync
    // We would NEVER create entities without validation!
    // Standard CreateAsync already includes necessary validation

    // ✅ CORRECT: When no additional logic needed, use standard methods DIRECTLY
    // Don't create wrapper methods - just call the standard method where needed:
    //
    // var result = await CreateAsync(player, _repository, _cache);
    //
    // That's it! No wrapper method needed.
}
}
```

### ❌ ELIMINATED: ALL Thin Wrapper Methods
```csharp
// ❌ WRONG: These are thin wrappers - DON'T CREATE THEM!
public async Task<Player?> GetPlayerByIdAsync(object playerId)
{
    return await GetByIdAsync(playerId, _repository, _cache); // Just a wrapper!
}

public async Task<Player?> GetPlayerByStringIdAsync(string playerId)
{
    return await GetByStringIdAsync(playerId, _repository, _cache); // Just a wrapper!
}

// ❌ WRONG: Entity-specific method names are also thin wrappers
public async Task<Player?> GetPlayerByIdAsync(string playerId)
public async Task<Player?> GetPlayerByNameAsync(string name)
public async Task<IEnumerable<Player>> GetPlayersByDiscordIdAsync(string discordId)
```

### Key Principles

1. **Repository Layer**: Use `object id` for maximum flexibility
2. **Business Layer**: Use `string` for external-facing APIs and user input
3. **Entity Operations**: Use full entity objects when working with complete data
4. **Performance**: Use strings when you only need references, objects when you need data
5. **Integration**: Use strings for external system compatibility
6. **Security**: Use strings for input validation and sanitization

### PostgreSQL Advantage
With PostgreSQL JSON, we can:
- Store complex objects directly in JSONB columns
- Query JSON fields efficiently
- Reduce serialization overhead
- Maintain type safety while being flexible

**But we still need strings for the practical reasons above!**

### Recommended Solution
✅ **Use Npgsql** - PostgreSQL's official .NET driver with JSON support

**Why Npgsql?**
- Native PostgreSQL JSONB support
- Automatic JSON serialization/deserialization
- Better performance than manual JSON handling
- LINQ support for JSON queries
- Type-safe JSON operations

**Migration Steps:**
1. Add Npgsql package to project
2. Replace manual JSON utilities with Npgsql's JSON features
3. Use Npgsql's `Jsonb` type for JSON columns
4. Leverage Npgsql's LINQ provider for JSON queries

**Example Npgsql JSON Usage:**
```csharp
// Instead of manual JSON utilities
public class PlayerRepository : NpgsqlRepository<Player>
{
    // Npgsql handles JSON automatically
    public async Task<IEnumerable<Player>> GetPlayersInTeam(string teamId)
    {
        return await _dbContext.Players
            .Where(p => p.TeamIdsJson.Contains(teamId)) // Native JSON query
            .ToListAsync();
    }
}
```

**Benefits:**
- 🚀 **Better Performance**: Native JSON operations
- 🔒 **Type Safety**: Strongly-typed JSON handling
- 🛠️ **Rich Features**: LINQ support, automatic mapping
- 📈 **Scalability**: Optimized for PostgreSQL JSONB

## Configuration Management

### appsettings.json Structure
```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Database=wabbitbot;Username=...;Password=..."
  },
  "Cache": {
    "DefaultExpiryMinutes": 60,
    "MaxSize": 1000
  },
  "Entities": {
    "Player": {
      "MaxCacheSize": 500,
      "DefaultCacheExpiryMinutes": 30
    },
    "User": {
      "MaxCacheSize": 1000,
      "DefaultCacheExpiryMinutes": 15
    }
  }
}
```

## Implementation Guidelines - Lean and Iterative

### Start Minimal, Grow Organically

**Phase 1 - Foundation (Start Here):**
- ✅ Implement `IDatabaseService<TEntity>` and generic database services
- ✅ **Redefine all entity classes for native PostgreSQL JSON support**
- ✅ Remove manual JSON serialization properties from entities
- ✅ Implement Npgsql native JSON mapping for complex objects
- ✅ Create basic entity configurations
- ✅ Set up minimal CoreService structure
- ✅ Implement only essential CRUD operations

**Phase 2 - Add as Needed (Iterative Growth):**
- 🔄 Add validation methods only when validation errors occur
- 🔄 Add complex queries only when specific data access patterns emerge
- 🔄 Add event publishing only when inter-service communication is required
- 🔄 Add archiving only when historical data requirements are identified

### What NOT to Do During Initial Implementation

❌ **Don't implement speculative features:**
- Player name validation rules (until validation is actually needed)
- Archiving functionality (until archiving is requested)
- Complex team-based queries (until team features are built)
- Event-driven architecture (until events are required)

❌ **Don't over-engineer the initial implementation:**
- Keep partial class files minimal to start
- Avoid complex business logic assumptions
- Don't implement methods "just in case"

### Success Criteria

The refactor is successful when:
- ✅ Basic CRUD operations work for all entities
- ✅ Code is organized and maintainable
- ✅ New features can be added incrementally without major refactoring
- ✅ The architecture scales with actual usage patterns
- ✅ No speculative code exists that isn't being used

**Remember**: It's easier to add functionality later than to remove over-engineered code. Start lean, grow based on real requirements, not assumptions.

This refactor dramatically simplifies the architecture while maintaining all functionality. The entity configuration pattern eliminates hardcoded database mappings, the unified `IDatabaseService` interface standardizes data operations, and the partial class organization keeps code organized without complexity.

The result is a maintainable, performant, and developer-friendly architecture that scales appropriately for a Discord bot.

## Database Migration Strategy

For comprehensive guidance on handling database schema migrations when deploying entity definition changes to production, see: [`database-migration-strategy.md`](./database-migration-strategy.md)

This separate document covers:
- Risk-based migration categories (Low/Medium/High Risk)
- Production deployment workflows
- Rollback strategies and best practices
- EF Core migration implementation patterns
- Testing strategies for schema changes

## Application & Database Versioning Strategy

### Modern Versioning Philosophy: Loose Coupling

**❌ OLD WAY**: Rigid version mapping
```
App v1.0.0 → Database Schema v1
App v1.1.0 → Database Schema v1 (no changes)
App v1.2.0 → Database Schema v2
App v2.0.0 → Database Schema v3
```

**✅ NEW WAY**: Independent evolution with compatibility ranges
```
App v1.0.0 to v1.5.0 → Compatible with Schema v1-v2
App v1.6.0 to v2.0.0 → Compatible with Schema v2-v3
App v2.1.0+ → Compatible with Schema v3+
```

### Implementation Steps for Loose Coupling Versioning

#### **Step 1: Create Application Version Tracking**
```csharp
// File: src/WabbitBot.Core/Common/Utilities/ApplicationInfo.cs
public static class ApplicationInfo
{
    public static Version CurrentVersion => new Version(
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);

    public static string VersionString => CurrentVersion.ToString(3);

    public static bool IsCompatibleWithSchema(string schemaVersion)
    {
        // Define compatibility ranges
        var ranges = new Dictionary<string, (string min, string max)>
        {
            ["1.0.x"] = ("001-1.0", "001-1.1"),
            ["1.1.x"] = ("001-1.0", "002-1.0"),
            ["1.2.x"] = ("002-1.0", "999-9.9")
        };

        foreach (var range in ranges)
        {
            if (VersionMatches(VersionString, range.Key))
            {
                return VersionInRange(schemaVersion, range.Value.min, range.Value.max);
            }
        }

        return false;
    }
}
```

#### **Step 2: Implement Schema Version Tracking**
```csharp
// File: src/WabbitBot.Core/Common/Utilities/SchemaVersionTracker.cs
public class SchemaVersionTracker
{
    private readonly WabbitBotDbContext _context;

    public async Task<string> GetCurrentSchemaVersionAsync()
    {
        // Get latest migration applied
        var migrations = await _context.Database.GetAppliedMigrationsAsync();
        var latestMigration = migrations.OrderByDescending(m => m).FirstOrDefault();

        // Extract version from migration name
        // e.g., "20240101120000_AddPlayerStats" → "001-1.2"
        return ParseMigrationToSchemaVersion(latestMigration);
    }

    public async Task ValidateCompatibilityAsync()
    {
        var appVersion = ApplicationInfo.VersionString;
        var schemaVersion = await GetCurrentSchemaVersionAsync();

        if (!ApplicationInfo.IsCompatibleWithSchema(schemaVersion))
        {
            throw new IncompatibleVersionException(
                $"App {appVersion} incompatible with Schema {schemaVersion}");
        }
    }
}
```

#### **Step 3: Add Version Compatibility Checking on Startup**
```csharp
// File: src/WabbitBot.Core/Program.cs (or Startup.cs)
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Validate version compatibility before starting
        using (var scope = host.Services.CreateScope())
        {
            var versionTracker = scope.ServiceProvider.GetRequiredService<SchemaVersionTracker>();
            await versionTracker.ValidateCompatibilityAsync();
        }

        await host.RunAsync();
    }
}
```

#### **Step 4: Create Feature Flags System**
```csharp
// File: src/WabbitBot.Core/Common/Utilities/FeatureManager.cs
public class FeatureManager
{
    private readonly SchemaVersionTracker _schemaTracker;

    public FeatureManager(SchemaVersionTracker schemaTracker)
    {
        _schemaTracker = schemaTracker;
    }

    public async Task<bool> IsNewLeaderboardEnabledAsync()
    {
        var appVersion = ApplicationInfo.CurrentVersion;
        var schemaVersion = await _schemaTracker.GetCurrentSchemaVersionAsync();

        return appVersion >= new Version("1.2.0") &&
               Version.Parse(schemaVersion) >= Version.Parse("002-1.0");
    }

    public async Task<bool> UseLegacyStatsFormatAsync()
    {
        var schemaVersion = await _schemaTracker.GetCurrentSchemaVersionAsync();
        return Version.Parse(schemaVersion) < Version.Parse("002-1.0");
    }

    public async Task<bool> IsAdvancedReportingEnabledAsync()
    {
        var appVersion = ApplicationInfo.CurrentVersion;
        var schemaVersion = await _schemaTracker.GetCurrentSchemaVersionAsync();

        return appVersion >= new Version("1.3.0") &&
               Version.Parse(schemaVersion) >= Version.Parse("003-1.0");
    }
}
```

#### **Step 5: Implement Version Metadata Table**
```sql
-- Add to initial migration or create new migration
CREATE TABLE schema_metadata (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    schema_version VARCHAR(20) NOT NULL,
    applied_at TIMESTAMP NOT NULL DEFAULT NOW(),
    applied_by VARCHAR(255),
    description TEXT,
    is_breaking_change BOOLEAN NOT NULL DEFAULT FALSE,
    compatibility_notes TEXT
);

-- Index for performance
CREATE INDEX idx_schema_metadata_version ON schema_metadata(schema_version);
CREATE INDEX idx_schema_metadata_applied_at ON schema_metadata(applied_at);
```

#### **Step 6: Add Version Drift Monitoring**
```csharp
// File: src/WabbitBot.Core/Common/Services/VersionMonitor.cs
public class VersionMonitor : BackgroundService
{
    private readonly SchemaVersionTracker _schemaTracker;
    private readonly ILogger<VersionMonitor> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public VersionMonitor(
        SchemaVersionTracker schemaTracker,
        ILogger<VersionMonitor> logger)
    {
        _schemaTracker = schemaTracker;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckVersionDriftAsync();
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error checking version compatibility");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CheckVersionDriftAsync()
    {
        var appVersion = ApplicationInfo.VersionString;
        var schemaVersion = await _schemaTracker.GetCurrentSchemaVersionAsync();

        if (!ApplicationInfo.IsCompatibleWithSchema(schemaVersion))
        {
            _logger.LogWarning(
                "Version drift detected: App {AppVersion} vs Schema {SchemaVersion}",
                appVersion, schemaVersion);

            // Could send alerts, notifications, etc.
        }
        else
        {
            _logger.LogDebug(
                "Version compatibility OK: App {AppVersion} ↔ Schema {SchemaVersion}",
                appVersion, schemaVersion);
        }
    }
}
```

#### **Step 7: Create Compatibility Test Suite**
```csharp
// File: src/WabbitBot.Core/Common/Tests/VersionCompatibilityTests.cs
[TestFixture]
public class VersionCompatibilityTests
{
    [TestCase("1.0.0", "001-1.0", ExpectedResult = true)]
    [TestCase("1.0.0", "001-1.2", ExpectedResult = false)] // Incompatible
    [TestCase("1.1.0", "001-1.0", ExpectedResult = true)]  // Backward compatible
    [TestCase("1.1.0", "002-1.0", ExpectedResult = true)]  // Forward compatible
    [TestCase("1.2.0", "002-1.0", ExpectedResult = true)]  // Modern features
    [TestCase("1.2.0", "001-1.0", ExpectedResult = false)] // Too old schema
    public bool VersionCompatibility_Works(string appVersion, string schemaVersion)
    {
        // Mock ApplicationInfo for testing
        return IsCompatible(appVersion, schemaVersion);
    }

    private bool IsCompatible(string appVersion, string schemaVersion)
    {
        var ranges = new Dictionary<string, (string min, string max)>
        {
            ["1.0.x"] = ("001-1.0", "001-1.1"),
            ["1.1.x"] = ("001-1.0", "002-1.0"),
            ["1.2.x"] = ("002-1.0", "999-9.9")
        };

        foreach (var range in ranges)
        {
            if (VersionMatches(appVersion, range.Key))
            {
                return VersionInRange(schemaVersion, range.Value.min, range.Value.max);
            }
        }

        return false;
    }

    private bool VersionMatches(string version, string pattern)
    {
        // Simple pattern matching: "1.1.x" matches "1.1.0", "1.1.5", etc.
        if (pattern.EndsWith(".x"))
        {
            var baseVersion = pattern.Substring(0, pattern.Length - 2);
            return version.StartsWith(baseVersion);
        }
        return version == pattern;
    }

    private bool VersionInRange(string version, string min, string max)
    {
        return string.Compare(version, min) >= 0 &&
               string.Compare(version, max) <= 0;
    }
}
```

#### **Step 8: Update Migration Templates**
```csharp
// Update migration template to include version metadata
public partial class AddNewFeature : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Schema changes here...

        // Record version metadata
        migrationBuilder.Sql(@"
            INSERT INTO schema_metadata
            (schema_version, description, is_breaking_change, compatibility_notes)
            VALUES
            ('002-1.1', 'Add new feature with backward compatibility', false, 'Compatible with app versions 1.1.0+')
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Rollback changes...

        // Remove version metadata
        migrationBuilder.Sql(@"
            DELETE FROM schema_metadata
            WHERE schema_version = '002-1.1'
        ");
    }
}
```

#### **Step 9: Create Version Compatibility Documentation**
```markdown
# Version Compatibility Guide

## Current Support Matrix
| App Version | Schema Range | Features | Notes |
|-------------|-------------|----------|-------|
| 1.0.x      | 001-1.0 to 001-1.1 | Basic features | Legacy support only |
| 1.1.x      | 001-1.0 to 002-1.0 | Extended features | Rolling update support |
| 1.2.x+     | 002-1.0+ | Modern features | Full feature set |

## Migration Windows
- **Zero-downtime**: Apps work during schema migrations within compatibility ranges
- **Grace period**: 30 days for version upgrades
- **Legacy support**: 6 months for major version transitions
- **Breaking changes**: Require coordinated deployments

## Deployment Strategy
1. **Blue-Green**: For major version transitions
2. **Rolling**: For backward-compatible updates
3. **Canary**: For testing new features with subsets of users
```

### Benefits of Loose Coupling Versioning

1. **🚀 Independent Evolution**: App and database can be updated separately
2. **🔄 Zero-Downtime Deployments**: Rolling updates without service interruption
3. **🛡️ Gradual Rollouts**: Feature flags enable/disable functionality safely
4. **📊 Better Monitoring**: Track version compatibility and drift
5. **🔧 Easier Rollbacks**: Version mismatches detected automatically
6. **📈 Flexible Scaling**: Support mixed versions during transitions
7. **🧪 Comprehensive Testing**: Test across version combinations
8. **📚 Clear Documentation**: Compatibility matrices guide deployments

This versioning strategy enables sophisticated deployment patterns while maintaining system stability and providing clear upgrade paths for both application and database changes.
