#### Step 2: EF Core Foundation ✅ COMPLETE

### Zero Dependency Injection - Direct Instantiation Architecture

**🎯 CRITICAL ARCHITECTURAL CONSTRAINT**: This application **explicitly avoids runtime dependency injection**. EF Core context creation and database operations must work with direct instantiation patterns.

#### 2a. Add Npgsql.EntityFrameworkCore.PostgreSQL Package

Add the official PostgreSQL provider for EF Core to enable native JSONB support and optimized PostgreSQL operations.

```xml
<!-- Add to your .csproj file -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
```

#### 2b. Create WabbitBotDbContext with JSONB Configurations

Implement the EF Core DbContext with native JSONB column mappings and PostgreSQL-specific configurations.

```csharp
public class WabbitBotDbContext : DbContext
{
    public WabbitBotDbContext(DbContextOptions<WabbitBotDbContext> options)
        : base(options) { }

    public DbSet<Player> Players { get; set; }
    public DbSet<User> Users { get; set; }
    // ... other entity DbSets

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // JSONB column configurations
        modelBuilder.Entity<Player>()
            .Property(p => p.TeamMemberships)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Player>()
            .Property(p => p.Stats)
            .HasColumnType("jsonb");

        // Add GIN indexes for JSONB performance
        modelBuilder.Entity<Player>()
            .HasIndex(p => p.TeamMemberships)
            .HasMethod("gin");
    }
}
```

#### 2c. Configure Entity Mappings for JSONB Columns

Set up proper JSONB serialization and deserialization for complex objects.

```csharp
// In Program.cs or Startup.cs
builder.Services.AddDbContext<WabbitBotDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsqlOptions =>
        {
            // Enable JSONB support
            npgsqlOptions.UseJsonb();
        }));
```

#### 2d. Set up Connection String Management

Configure secure connection string handling for PostgreSQL database access.

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "WabbitBotDatabase": "Host=localhost;Database=wabbitbot;Username=user;Password=password"
  }
}

// Program.cs
var connectionString = builder.Configuration.GetConnectionString("WabbitBotDatabase");
builder.Services.AddDbContext<WabbitBotDbContext>(options =>
    options.UseNpgsql(connectionString));
```

#### STEP 2 IMPACT:

### Database Foundation Benefits

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

**This EF Core foundation provides the robust database layer for our new architecture!** 🎯