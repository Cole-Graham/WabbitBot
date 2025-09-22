#### Step 6: JSONB Schema Migration ✅ COMPLETE

### Complete Database Schema Transformation

**🎯 CRITICAL**: Transform the entire database schema from manual JSON strings to native PostgreSQL JSONB support.

#### 6a. Implement EF Core Migrations Strategy

Set up EF Core migrations infrastructure for automatic schema management and version control.

```csharp
// WabbitBotDbContextFactory.cs - Design-time factory for migrations
public class WabbitBotDbContextFactory : IDesignTimeDbContextFactory<WabbitBotDbContext>
{
    public WabbitBotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WabbitBotDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("WABBITBOT_CONNECTION_STRING")
            ?? "Host=localhost;Database=wabbitbot;Username=user;Password=password";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.UseJsonb();
        });

        return new WabbitBotDbContext(optionsBuilder.Options);
    }
}

// WabbitBotDbContextProvider.cs - Runtime provider
public static class WabbitBotDbContextProvider
{
    private static string? _connectionString;

    public static void Initialize(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static WabbitBotDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<WabbitBotDbContext>();
        optionsBuilder.UseNpgsql(_connectionString, npgsqlOptions =>
        {
            npgsqlOptions.UseJsonb();
        });

        return new WabbitBotDbContext(optionsBuilder.Options);
    }
}
```

#### 6b. Update Database Schema to Use JSONB Columns

Replace all manual JSON string columns with native PostgreSQL JSONB columns.

```csharp
// In WabbitBotDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Players table with JSONB columns
    modelBuilder.Entity<Player>(entity =>
    {
        entity.ToTable("players");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("uuid_generate_v4()");

        // JSONB columns for complex objects
        entity.Property(e => e.TeamMemberships)
            .HasColumnType("jsonb")
            .IsRequired();

        entity.Property(e => e.Stats)
            .HasColumnType("jsonb")
            .IsRequired();

        entity.Property(e => e.Metadata)
            .HasColumnType("jsonb")
            .IsRequired();
    });

    // Add GIN indexes for JSONB performance
    modelBuilder.Entity<Player>()
        .HasIndex(p => p.TeamMemberships)
        .HasMethod("gin");

    modelBuilder.Entity<Player>()
        .HasIndex(p => p.Stats)
        .HasMethod("gin");
}
```

#### 6c. Add JSONB Indexes for Performance Optimization

Implement specialized indexes for JSONB querying performance.

```sql
-- GIN indexes for JSONB columns
CREATE INDEX CONCURRENTLY idx_players_team_memberships
    ON players USING GIN (team_memberships);

CREATE INDEX CONCURRENTLY idx_players_stats
    ON players USING GIN (stats);

CREATE INDEX CONCURRENTLY idx_players_metadata
    ON players USING GIN (metadata);

-- Composite indexes for common query patterns
CREATE INDEX CONCURRENTLY idx_players_active_stats
    ON players (is_archived)
    WHERE is_archived = false;
```

#### 6d. Update Table Structures for Native JSON Support

Design tables that leverage PostgreSQL JSONB capabilities to the fullest.

```sql
-- Complete players table with JSONB optimization
CREATE TABLE players (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    last_active TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    archived_at TIMESTAMP WITH TIME ZONE NULL,

    -- JSONB columns for complex data
    team_memberships JSONB NOT NULL DEFAULT '[]'::jsonb,
    stats JSONB NOT NULL DEFAULT '{}'::jsonb,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,

    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
```

#### 6e. Migration Strategy Documented (./database-migration-strategy.md)

Reference the comprehensive migration strategy document for production deployment guidance.

#### STEP 6 IMPACT:

### Schema Transformation Results

#### Before (Manual JSON Strings):
```sql
-- ❌ OLD: Manual JSON storage
CREATE TABLE players (
    Id UUID PRIMARY KEY,
    Name VARCHAR(255),
    TeamIdsJson TEXT,  -- Manual JSON string
    StatsJson TEXT,    -- Manual JSON string
    CreatedAt TIMESTAMP
);
```

#### After (Native JSONB):
```sql
-- ✅ NEW: Native JSONB storage
CREATE TABLE players (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(255) NOT NULL,
    LastActive TIMESTAMP NOT NULL,
    IsArchived BOOLEAN NOT NULL DEFAULT FALSE,
    ArchivedAt TIMESTAMP NULL,
    TeamMemberships JSONB,  -- Native JSONB array
    Stats JSONB,           -- Native JSONB object
    Metadata JSONB,        -- Flexible JSONB data
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    UpdatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

-- JSONB indexes for performance
CREATE INDEX idx_players_team_memberships ON players USING GIN (TeamMemberships);
CREATE INDEX idx_players_stats ON players USING GIN (Stats);
CREATE INDEX idx_players_metadata ON players USING GIN (Metadata);
```

### Migration Infrastructure Created

#### WabbitBotDbContextFactory.cs
```csharp
// Enables EF Core migrations without runtime DI
public class WabbitBotDbContextFactory : IDesignTimeDbContextFactory<WabbitBotDbContext>
{
    public WabbitBotDbContext CreateDbContext(string[] args)
    {
        // Design-time context creation for migrations
    }
}
```

#### WabbitBotDbContextProvider.cs
```csharp
// Static provider for runtime DbContext access
public static class WabbitBotDbContextProvider
{
    public static void Initialize(string connectionString)
    public static WabbitBotDbContext CreateDbContext()
}
```

#### Initial Migration (20241201120000_InitialSchema.cs)
```csharp
public partial class InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Complete schema creation with JSONB support
        // UUID generation, indexes, constraints
    }
}
```

### EF Core Integration in Program.cs

```csharp
public static async Task Main(string[] args)
{
    var host = CreateHostBuilder(args).Build();

    // Initialize database on startup
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = WabbitBotDbContextProvider.CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    await host.RunAsync();
}
```

### CoreService EF Core Integration

```csharp
public partial class CoreService.Player.Data
{
    // ✅ CORRECT: Use EF Core directly - no thin wrapper methods
    // Direct usage: dbContext.Players.FirstOrDefaultAsync(p => p.Id == playerId)

    public async Task<Result<Player>> CreatePlayerWithStatsAsync(Player player, PlayerStats stats)
    {
        using var dbContext = WabbitBotDbContextProvider.CreateDbContext();

        // Business logic: create player with associated stats
        player.Stats = stats;
        dbContext.Players.Add(player);
        await dbContext.SaveChangesAsync();

        return Result<Player>.Success(player);
    }
}
```

### Performance Benefits Achieved

1. **🚀 Query Performance**: Native JSONB operations vs string parsing
2. **📊 Index Optimization**: GIN indexes on JSONB columns
3. **🔍 Rich Querying**: LINQ support for complex JSON operations
4. **💾 Storage Efficiency**: Optimized JSONB storage format
5. **⚡ Type Safety**: Strongly-typed JSON access

### Migration Strategy Documentation

For comprehensive guidance on handling database schema migrations when deploying entity definition changes to production, see: [`database-migration-strategy.md`](./database-migration-strategy.md)

**This JSONB schema migration step completes our database transformation to native PostgreSQL JSON support!** 🎯
