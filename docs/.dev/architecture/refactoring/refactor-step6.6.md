#### Step 6.5: Application & Database Versioning Strategy ⭐ CURRENT

### Modern Versioning Philosophy: Loose Coupling

**🎯 CRITICAL**: Implement independent versioning for application and database schema with compatibility ranges and automatic drift monitoring.

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

#### 6.5a. Create Application Version Tracking (ApplicationInfo Class)

Implement centralized application version tracking with compatibility matrix support.

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

    private static bool VersionMatches(string version, string pattern)
    {
        if (pattern.EndsWith(".x"))
        {
            var baseVersion = pattern.Substring(0, pattern.Length - 2);
            return version.StartsWith(baseVersion);
        }
        return version == pattern;
    }

    private static bool VersionInRange(string version, string min, string max)
    {
        return string.Compare(version, min) >= 0 &&
               string.Compare(version, max) <= 0;
    }
}
```

#### 6.5b. Implement Schema Version Tracking (SchemaVersionTracker Class)

Create schema version tracker that extracts version information from EF Core migrations.

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

    private string ParseMigrationToSchemaVersion(string? migrationName)
    {
        if (string.IsNullOrEmpty(migrationName))
            return "000-0.0";

        // Extract version pattern from migration name
        // Implementation depends on your migration naming convention
        return ExtractVersionFromMigration(migrationName);
    }
}
```

#### 6.5c. Add Version Compatibility Checking on Startup

Integrate version validation into application startup process.

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

#### 6.5d. Create Feature Flags System (FeatureManager for Gradual Rollouts)

Implement feature flags that enable/disable functionality based on version compatibility.

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

#### 6.5e. Implement Version Metadata Table (Schema_Metadata for Audit Trail)

Add database table to track schema changes and compatibility information.

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

#### 6.5f. Add Version Drift Monitoring (Alerting for Incompatible Combinations)

Implement a long-running service to monitor version compatibility and alert on drift.

```csharp
// File: src/WabbitBot.Core/Common/Services/VersionMonitor.cs
public class VersionMonitor
{
    private readonly Func<WabbitBotDbContext> _dbContextFactory;
    // private readonly ILogger<VersionMonitor> _logger; // TODO: Inject a logger
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);

    public VersionMonitor(Func<WabbitBotDbContext> dbContextFactory /*, ILogger<VersionMonitor> logger */)
    {
        _dbContextFactory = dbContextFactory;
        // _logger = logger;
    }

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
                // _logger.LogError(ex, "Error checking version compatibility");
                Console.WriteLine($"Error checking version compatibility: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CheckVersionDriftAsync()
    {
        using var dbContext = _dbContextFactory();
        var schemaTracker = new SchemaVersionTracker(dbContext);
        var appVersion = ApplicationInfo.VersionString;
        var schemaVersion = await schemaTracker.GetCurrentSchemaVersionAsync();

        if (!ApplicationInfo.IsCompatibleWithSchema(schemaVersion))
        {
            // _logger.LogWarning(
            //     "Version drift detected: App {AppVersion} vs Schema {SchemaVersion}",
            //     appVersion, schemaVersion);
            Console.WriteLine($"Version drift detected: App {appVersion} vs Schema {schemaVersion}");
        }
        else
        {
            // _logger.LogDebug(
            //     "Version compatibility OK: App {AppVersion} ↔ Schema {SchemaVersion}",
            //     appVersion, schemaVersion);
            Console.WriteLine($"Version compatibility OK: App {appVersion} ↔ Schema {schemaVersion}");
        }
    }
}
```

#### 6.5g. Create Compatibility Test Suite (VersionCompatibilityTests)

Implement comprehensive tests for version compatibility scenarios.

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

#### 6.5h. Update Migration Templates (Version Metadata Integration)

Modify migration templates to include version metadata tracking.

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

#### STEP 6.5 IMPACT:

### Version Compatibility Infrastructure Created

#### Before (Rigid Version Coupling):
```csharp
// ❌ OLD: Rigid version dependencies
App v1.0.0 → Database Schema v1
App v1.1.0 → Database Schema v1 (no changes)
App v1.2.0 → Database Schema v2 (breaking change)
```

#### After (Independent Evolution):
```csharp
// ✅ NEW: Compatibility ranges enable independent evolution
App v1.0.0 to v1.5.0 → Compatible with Schema v1-v2
App v1.6.0 to v2.0.0 → Compatible with Schema v2-v3
App v2.1.0+ → Compatible with Schema v3+
```

### Infrastructure Components Implemented

#### ApplicationInfo.cs - Centralized Version Tracking
```csharp
public static class ApplicationInfo
{
    public static Version CurrentVersion => new Version(
        FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);

    public static string VersionString => CurrentVersion.ToString(3);

    public static bool IsCompatibleWithSchema(string schemaVersion)
    {
        // Define compatibility ranges for independent evolution
        var ranges = new Dictionary<string, (string min, string max)>
        {
            ["1.0.x"] = ("001-1.0", "001-1.1"),
            ["1.1.x"] = ("001-1.0", "002-1.0"),
            ["1.2.x"] = ("002-1.0", "999-9.9")
        };
        // ... compatibility logic
    }
}
```

#### SchemaVersionTracker.cs - Database Schema Monitoring
```csharp
public class SchemaVersionTracker
{
    public async Task<string> GetCurrentSchemaVersionAsync()
    {
        // Extract version from latest EF Core migration
        var migrations = await _context.Database.GetAppliedMigrationsAsync();
        var latestMigration = migrations.OrderByDescending(m => m).FirstOrDefault();
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

#### FeatureManager.cs - Gradual Feature Rollouts
```csharp
public class FeatureManager
{
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
}
```

#### VersionMonitor.cs - Proactive Drift Detection
```csharp
public class VersionMonitor
{
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckVersionDriftAsync();
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task CheckVersionDriftAsync()
    {
        // ... logic to get versions and compare ...
        if (!ApplicationInfo.IsCompatibleWithSchema(schemaVersion))
        {
            // _logger.LogWarning(...)
        }
    }
}
```

### Schema Metadata Table for Audit Trail
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

### Benefits of Loose Coupling Versioning

1. **🎯 Independent Evolution**: App and database can evolve separately within compatibility ranges
2. **🚀 Zero-Downtime Deploys**: Rolling updates without service interruption
3. **🛡️ Safe Rollouts**: Feature flags enable gradual feature deployment
4. **📊 Proactive Monitoring**: Automatic detection of version incompatibilities
5. **🔧 Easy Rollbacks**: Clear compatibility matrices guide emergency rollbacks
6. **📈 Flexible Scaling**: Support mixed version environments during transitions
7. **🧪 Comprehensive Testing**: Test across version combinations
8. **📚 Clear Documentation**: Compatibility matrices guide deployments

### Version Compatibility Documentation
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

**This loose coupling versioning strategy enables modern deployment practices while maintaining system stability!** 🎯
