#### Step 3: Database Layer Refinement ✅ COMPLETE

### Unified Database Interface Architecture

**🎯 CRITICAL ARCHITECTURAL DECISION**: **Component classes contain ONLY configuration and properties. Service classes contain ALL methods.**

#### 3a. Implement `IDatabaseService<TEntity>` Interface

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

#### 3b. Update Generic `RepositoryService<TEntity>` with EF Core Automatic Mapping

Replace manual SQL operations with EF Core's automatic entity mapping and LINQ-to-SQL translation.

```csharp
public class RepositoryService<TEntity> : IDatabaseService<TEntity>
    where TEntity : Entity
{
    private readonly WabbitBotDbContext _dbContext;
    private readonly ILogger<RepositoryService<TEntity>> _logger;

    public RepositoryService(WabbitBotDbContext dbContext, ILogger<RepositoryService<TEntity>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component)
    {
        try
        {
            _dbContext.Set<TEntity>().Add(entity);
            await _dbContext.SaveChangesAsync();
            return Result<TEntity>.Success(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create entity");
            return Result<TEntity>.Failure(ex.Message);
        }
    }

    public async Task<TEntity?> GetByIdAsync(object id, DatabaseComponent component)
    {
        return await _dbContext.Set<TEntity>()
            .FirstOrDefaultAsync(e => e.Id.Equals(id));
    }
}
```

#### 3c. Update Generic `CacheService<TEntity>`

Implement in-memory caching with TTL support and automatic eviction policies.

```csharp
public class CacheService<TEntity> : IDatabaseService<TEntity>
    where TEntity : Entity
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly int _maxSize;
    private readonly TimeSpan _defaultExpiry;

    public CacheService(int maxSize, TimeSpan defaultExpiry)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _maxSize = maxSize;
        _defaultExpiry = defaultExpiry;
    }

    public async Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component)
    {
        var key = entity.Id.ToString();
        var entry = new CacheEntry(entity, _defaultExpiry);

        _cache.AddOrUpdate(key, entry, (_, _) => entry);

        // Evict oldest entries if cache is full
        if (_cache.Count > _maxSize)
        {
            EvictExpiredEntries();
        }

        return Result<TEntity>.Success(entity);
    }

    public async Task<TEntity?> GetByIdAsync(object id, DatabaseComponent component)
    {
        if (_cache.TryGetValue(id.ToString(), out var entry))
        {
            if (!entry.IsExpired)
            {
                return (TEntity)entry.Value;
            }
            else
            {
                _cache.TryRemove(id.ToString(), out _);
            }
        }
        return default;
    }
}
```

#### 3d. Update Generic `ArchiveService<TEntity>`

Implement historical data storage with EF Core for immutable audit trails.

```csharp
public class ArchiveService<TEntity> : IDatabaseService<TEntity>
    where TEntity : Entity
{
    private readonly WabbitBotDbContext _dbContext;
    private readonly ILogger<ArchiveService<TEntity>> _logger;

    public ArchiveService(WabbitBotDbContext dbContext, ILogger<ArchiveService<TEntity>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<TEntity>> CreateAsync(TEntity entity, DatabaseComponent component)
    {
        // Create archive record with timestamp
        var archiveEntity = CreateArchiveEntity(entity);

        _dbContext.Set<TEntity>().Add(archiveEntity);
        await _dbContext.SaveChangesAsync();

        return Result<TEntity>.Success(archiveEntity);
    }

    private TEntity CreateArchiveEntity(TEntity entity)
    {
        // Clone entity and set archive timestamp
        // Implementation depends on specific entity structure
        return entity; // Placeholder
    }
}
```

#### 3e. Remove MapEntity/BuildParameters Methods from Component Classes

Eliminate manual SQL parameter building and entity mapping logic that's now handled automatically by EF Core.

```csharp
// ❌ OLD: Manual mapping methods to remove
public class OldRepository<TEntity>
{
    // Remove these methods - EF Core handles automatically
    protected virtual TEntity MapEntity(IDataReader reader) => throw new NotImplementedException();
    protected virtual object[] BuildParameters(TEntity entity) => throw new NotImplementedException();
    protected virtual string BuildInsertSql() => throw new NotImplementedException();
    protected virtual string BuildUpdateSql() => throw new NotImplementedException();
}
```

#### 3f. Implement Entity Configuration Pattern

Create configuration classes that define database mappings, caching settings, and validation rules for each entity.

```csharp
// EntityConfig.cs - Base configuration class
public abstract class EntityConfig<TEntity> where TEntity : Entity
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

// PlayerDbConfig.cs - Player-specific configuration
public class PlayerDbConfig : EntityConfig<Player>
{
    public PlayerDbConfig() : base(
        tableName: "players",
        archiveTableName: "player_archive",
        columns: new[] {
            "Id", "Name", "LastActive", "IsArchived", "ArchivedAt",
            "TeamMemberships", "Stats", "Metadata", "CreatedAt", "UpdatedAt"
        },
        maxCacheSize: 500,
        defaultCacheExpiry: TimeSpan.FromMinutes(30))
    {
    }
}
```

### Clean Separation: Component Classes vs Service Classes

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

#### STEP 3 IMPACT:

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

**This database layer refinement creates the foundation for our unified data access architecture!** 🎯
