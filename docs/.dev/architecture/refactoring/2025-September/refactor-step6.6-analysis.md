# Step 6.6 Architecture Analysis and Required Updates

## Executive Summary

Step 6.6 (Application & Database Versioning Strategy) contains **several architectural mismatches** with the current WabbitBot implementation. This document identifies the discrepancies and proposes corrections.

**🚨 CRITICAL:** A deeper investigation revealed a **fatal initialization bug** in the current codebase. See [refactor-step6.6-critical-findings.md](./refactor-step6.6-critical-findings.md) for details on the uninitialized `DbContextFactory` issue that would cause startup crashes.

---

## Current Architecture Overview

### 1. DbContext Initialization Pattern

**Current Implementation (as of refactor 6.9):**
```csharp
// File: src/WabbitBot.Core/Program.cs
private static async Task InitializeCoreAsync(IConfiguration configuration)
{
    // Initialize the DbContext provider
    WabbitBotDbContextProvider.Initialize(configuration);

    // Create DbContext, run migrations, and validate schema version
    using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
    {
        await dbContext.Database.MigrateAsync();
        
        var versionTracker = new SchemaVersionTracker(dbContext);
        await versionTracker.ValidateCompatibilityAsync();
        
        await GlobalEventBus.PublishAsync(new DatabaseInitializedEvent());
    }

    // Initialize core services with their dependencies
    await InitializeCoreServices();
}
```

**Key Points:**
- Uses `WabbitBotDbContextProvider` static class (not `WabbitBotDbContextFactory`)
- Provider is initialized with `IConfiguration`, not passed to `CoreService.InitializeServices`
- Creates contexts via `WabbitBotDbContextProvider.CreateDbContext()` static method
- No runtime DI; follows service locator pattern

### 2. CoreService Integration

**Current Implementation:**
```csharp
// File: src/WabbitBot.Core/Common/Services/Core/CoreService.cs
public static partial class CoreService
{
    private static Lazy<ICoreEventBus>? _lazyEventBus;
    private static Lazy<IErrorService>? _lazyErrorHandler;
    private static Lazy<IDbContextFactory<WabbitBotDbContext>>? _lazyDbContextFactory;

    public static ICoreEventBus EventBus => _lazyEventBus!.Value;
    public static IErrorService ErrorHandler => _lazyErrorHandler!.Value;
    public static IDbContextFactory<WabbitBotDbContext> DbContextFactory => _lazyDbContextFactory!.Value;

    // Initialization method called once at startup
    public static void InitializeServices(
        ICoreEventBus eventBus,
        IErrorService errorHandler,
        IDbContextFactory<WabbitBotDbContext> dbContextFactory)
    {
        _lazyEventBus = new Lazy<ICoreEventBus>(() => eventBus ?? 
            throw new ArgumentNullException(nameof(eventBus)), 
            LazyThreadSafetyMode.ExecutionAndPublication);

        _lazyErrorHandler = new Lazy<IErrorService>(() => errorHandler ?? 
            throw new ArgumentNullException(nameof(errorHandler)), 
            LazyThreadSafetyMode.ExecutionAndPublication);

        _lazyDbContextFactory = new Lazy<IDbContextFactory<WabbitBotDbContext>>(() => dbContextFactory ?? 
            throw new ArgumentNullException(nameof(dbContextFactory)), 
            LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
```

**Issue Identified:**
- `CoreService.InitializeServices()` method exists but **is NOT called** in `Program.Main()`
- `CoreService.DbContextFactory` property exists but is **never initialized**
- Current code uses `WabbitBotDbContextProvider.CreateDbContext()` directly instead of going through `CoreService`

### 3. WabbitBotDbContextProvider Implementation

**Current Implementation:**
```csharp
// File: src/WabbitBot.Core/Common/Database/WabbitBotDbContextProvider.cs
public static class WabbitBotDbContextProvider
{
    private static DbContextOptions<WabbitBotDbContext>? _options;
    private static NpgsqlDataSource? _dataSource;
    private static string? _connectionString;

    public static void Initialize(IConfiguration configuration)
    {
        var databaseSettings = new DatabaseSettings();
        configuration.GetSection("Bot:Database").Bind(databaseSettings);
        databaseSettings.Validate();

        _connectionString = databaseSettings.GetEffectiveConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<WabbitBotDbContext>();

        if (databaseSettings.Provider.ToLowerInvariant() == "postgresql")
        {
            var dsBuilder = new NpgsqlDataSourceBuilder(_connectionString);
            dsBuilder.EnableDynamicJson();
            _dataSource = dsBuilder.Build();

            optionsBuilder.UseNpgsql(_dataSource, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
                npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
        }
        else
        {
            throw new NotSupportedException($"Database provider '{databaseSettings.Provider}' is not supported. Only 'postgresql' is supported.");
        }

        _options = optionsBuilder.Options;
    }

    public static WabbitBotDbContext CreateDbContext()
    {
        if (_options == null)
            throw new InvalidOperationException("DbContextProvider has not been initialized. Call Initialize() first.");

        return new WabbitBotDbContext(_options);
    }

    public static string GetConnectionString()
    {
        return _connectionString ?? throw new InvalidOperationException("DbContextProvider has not been initialized.");
    }
}
```

**Key Features:**
- PostgreSQL-only (no SQLite support)
- Uses `NpgsqlDataSource` with `EnableDynamicJson()` for JSONB support
- Implements retry logic and query splitting
- Static initialization pattern (no runtime DI)

---

## Step 6.6 Discrepancies

### ❌ Issue 1: Incorrect DbContext Creation Pattern

**Step 6.6 shows:**
```csharp
// File: src/WabbitBot.Core/Program.cs
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        // Validate version compatibility before starting
        using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
        {
            var versionTracker = new SchemaVersionTracker(dbContext);
            await versionTracker.ValidateCompatibilityAsync();
        }

        await host.RunAsync();
    }
}
```

**Problems:**
1. References `CreateHostBuilder()` which doesn't exist (no ASP.NET Core host)
2. Shows `host.RunAsync()` pattern (incompatible with current static `Main()` approach)
3. Correct but incomplete - missing migration step

**✅ Correct Pattern (Already Implemented):**
```csharp
// File: src/WabbitBot.Core/Program.cs
private static async Task InitializeCoreAsync(IConfiguration configuration)
{
    // Initialize the DbContext provider
    WabbitBotDbContextProvider.Initialize(configuration);

    // Create DbContext, run migrations, and validate schema version
    using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
    {
        await dbContext.Database.MigrateAsync();

        var versionTracker = new SchemaVersionTracker(dbContext);
        await versionTracker.ValidateCompatibilityAsync();

        await GlobalEventBus.PublishAsync(new DatabaseInitializedEvent());
    }

    // Initialize core services with their dependencies
    await InitializeCoreServices();
}
```

### ❌ Issue 2: VersionMonitor DbContextFactory Pattern

**Step 6.6 shows:**
```csharp
// File: src/WabbitBot.Core/Common/Services/VersionMonitor.cs
public class VersionMonitor
{
    private readonly Func<WabbitBotDbContext> _dbContextFactory;
    
    public VersionMonitor(Func<WabbitBotDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task CheckVersionDriftAsync()
    {
        using var dbContext = _dbContextFactory();
        var schemaTracker = new SchemaVersionTracker(dbContext);
        // ...
    }
}
```

**Problems:**
1. Uses `Func<WabbitBotDbContext>` instead of proper disposal pattern
2. Doesn't align with current `WabbitBotDbContextProvider` usage
3. Missing async context creation

**✅ Correct Pattern:**
```csharp
// File: src/WabbitBot.Core/Common/Services/VersionMonitor.cs
public class VersionMonitor
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public async Task StartAsync(CancellationToken stoppingToken)
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
                await CoreService.ErrorHandler.CaptureAsync(
                    ex, 
                    "Error checking version compatibility", 
                    nameof(CheckVersionDriftAsync));
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CheckVersionDriftAsync()
    {
        using var dbContext = WabbitBotDbContextProvider.CreateDbContext();
        var schemaTracker = new SchemaVersionTracker(dbContext);
        var appVersion = ApplicationInfo.VersionString;
        var schemaVersion = await schemaTracker.GetCurrentSchemaVersionAsync();

        if (!ApplicationInfo.IsCompatibleWithSchema(schemaVersion))
        {
            await CoreService.ErrorHandler.CaptureAsync(
                new IncompatibleVersionException($"Version drift detected: App {appVersion} vs Schema {schemaVersion}"),
                $"Version drift detected: App {appVersion} vs Schema {schemaVersion}",
                nameof(CheckVersionDriftAsync));
        }
    }
}
```

### ❌ Issue 3: Missing Integration with Current Startup Flow

**Step 6.6 doesn't show:**
1. How `VersionMonitor` should be started in `Program.Main()`
2. Integration with existing event system
3. How to handle monitoring lifecycle with current architecture

**✅ Required Integration:**
```csharp
// File: src/WabbitBot.Core/Program.cs
private static async Task InitializeCoreAsync(IConfiguration configuration)
{
    try
    {
        // Initialize the DbContext provider
        WabbitBotDbContextProvider.Initialize(configuration);

        // Create DbContext, run migrations, and validate schema version
        using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
        {
            await dbContext.Database.MigrateAsync();

            var versionTracker = new SchemaVersionTracker(dbContext);
            await versionTracker.ValidateCompatibilityAsync();

            await GlobalEventBus.PublishAsync(new DatabaseInitializedEvent());
        }

        // Initialize core services with their dependencies
        await InitializeCoreServices();

        // Initialize event handlers
        await InitializeEventHandlers(CoreEventBus, ErrorService);

        // Start version monitoring in background
        var versionMonitor = new VersionMonitor();
        _ = Task.Run(async () =>
        {
            var cts = new CancellationTokenSource();
            try
            {
                await versionMonitor.StartAsync(cts.Token);
            }
            catch (Exception ex)
            {
                await ErrorService.CaptureAsync(ex, "Version monitor failed", nameof(InitializeCoreAsync));
            }
        });

        // ... rest of initialization
    }
    catch (Exception ex)
    {
        await ErrorService.CaptureAsync(ex, "Program startup error", nameof(InitializeCoreAsync));
        throw;
    }
}
```

### ❌ Issue 4: Schema Metadata Table Not Generated

**Step 6.6 proposes:**
```sql
CREATE TABLE schema_metadata (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    schema_version VARCHAR(20) NOT NULL,
    applied_at TIMESTAMP NOT NULL DEFAULT NOW(),
    applied_by VARCHAR(255),
    description TEXT,
    is_breaking_change BOOLEAN NOT NULL DEFAULT FALSE,
    compatibility_notes TEXT
);
```

**Problems:**
1. No entity class defined
2. No `[EntityMetadata]` attribute
3. Not integrated with source generators
4. No migration created

**✅ Required Implementation:**
```csharp
// File: src/WabbitBot.Core/Common/Models/Common/SchemaMetadata.cs
using System;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data;

namespace WabbitBot.Core.Common.Models.Common
{
    [EntityMetadata(
        TableName = "schema_metadata",
        Description = "Tracks database schema version history",
        EmitArchive = false,
        EmitCache = false
    )]
    public class SchemaMetadata : Entity
    {
        public string SchemaVersion { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public string? AppliedBy { get; set; }
        public string? Description { get; set; }
        public bool IsBreakingChange { get; set; }
        public string? CompatibilityNotes { get; set; }
    }
}
```

Then create migration:
```bash
cd src/WabbitBot.Core
dotnet ef migrations add AddSchemaMetadataTable
```

---

## Implementation Status

### ✅ Already Implemented (No Changes Needed)

1. **ApplicationInfo.cs** - Exists and matches spec
2. **SchemaVersionTracker.cs** - Exists and is used in startup
3. **FeatureManager.cs** - Exists with correct implementation
4. **Version validation on startup** - Already integrated in `Program.InitializeCoreAsync()`

### ⏳ Partially Implemented (Needs Updates)

5. **VersionMonitor.cs** - Class doesn't exist yet, needs creation
6. **Schema metadata table** - Not created, needs entity + migration

### ❌ Not Implemented

7. **VersionCompatibilityTests** - Test suite doesn't exist
8. **Migration template updates** - Not implemented

---

## Recommended Action Plan

### Phase 1: Fix VersionMonitor (Priority: High)

1. Create `VersionMonitor.cs` using corrected pattern
2. Integrate into `Program.InitializeCoreAsync()`
3. Use `WabbitBotDbContextProvider.CreateDbContext()` for context creation
4. Use `CoreService.ErrorHandler` for error capture

### Phase 2: Add Schema Metadata Tracking (Priority: Medium)

1. Create `SchemaMetadata` entity with `[EntityMetadata]` attribute
2. Generate migration with `dotnet ef migrations add AddSchemaMetadataTable`
3. Update `SchemaVersionTracker` to write to `schema_metadata` table
4. Update migration template to auto-insert metadata records

### Phase 3: Add Test Coverage (Priority: Low)

1. Create `VersionCompatibilityTests.cs` in `WabbitBot.Core.Tests`
2. Add integration tests for version checking
3. Add unit tests for `ApplicationInfo.IsCompatibleWithSchema()`

### Phase 4: Update Documentation (Priority: Low)

1. Update step 6.6 with corrected code examples
2. Add "how to" guide for adding new schema versions
3. Document rollback procedures

---

## Key Architectural Decisions to Preserve

### 1. No Runtime Dependency Injection
- Continue using `WabbitBotDbContextProvider` static class
- Avoid introducing DI containers or service collections
- Use service locator pattern via `CoreService`

### 2. PostgreSQL-Only Support
- Remove any SQLite references in step 6.6
- Emphasize JSONB and PostgreSQL-specific features
- Use `NpgsqlDataSource` with `EnableDynamicJson()`

### 3. Event-Driven Architecture
- Version compatibility failures should publish events
- Use `GlobalEventBus` for cross-project communication
- Use `CoreEventBus` for internal Core events

### 4. Source Generation First
- All entities must use `[EntityMetadata]` attribute
- Let generators handle DbSet registration
- Avoid manual configuration files

---

## Conclusion

Step 6.6 provides good conceptual guidance but needs **significant updates** to align with the actual WabbitBot architecture. The core version tracking infrastructure (ApplicationInfo, SchemaVersionTracker, FeatureManager) is already in place and working correctly. The missing pieces are:

1. **VersionMonitor background service** (not created)
2. **SchemaMetadata entity + migration** (not created)
3. **Test coverage** (not created)
4. **Documentation updates** (step 6.6 needs corrections)

All corrections should maintain the project's architectural principles: no runtime DI, PostgreSQL-only, event-driven, and source generation first.
