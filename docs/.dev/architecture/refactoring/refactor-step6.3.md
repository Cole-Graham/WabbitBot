#### Step 6.3: Simplify Map Entity and Refactor Thumbnail Management ✅ COMPLETED

**Refactor the Map entity to remove file system coupling** and move thumbnail logic to a utility class in WabbitBot.Common. This simplifies the domain entity while improving testability and maintainability within the existing architecture. Thumbnails remain as files on disk, not stored in the database.

### **🎯 OBJECTIVES:**

1. **Remove computed thumbnail properties** from Map entity
2. **Keep only ThumbnailFilename** in database storage
3. **Move thumbnail logic** to ThumbnailUtility in WabbitBot.Common
4. **Update configuration** for thumbnail file management (files stored on disk, not in database)

### **📋 DETAILED TASKS:**

#### 6.3a. Simplify Map Entity

**Remove file system dependent computed properties** that create tight coupling and performance issues.

```csharp
// ❌ BEFORE: Complex computed properties with file system I/O
public class Map : Entity
{
    public string? ThumbnailFilename { get; set; }

    // File system dependent properties - REMOVE THESE
    public string? ThumbnailPath { get; } // ❌ File.Exists() calls
    public string? ThumbnailUrl { get; }  // ❌ Discord-specific URLs
    public bool HasThumbnail { get; }     // ❌ File system checks
}

// ✅ AFTER: Simple data-only entity
public class Map : Entity
{
    public string? ThumbnailFilename { get; set; }
    // Only store filename, no computed properties
}
```

**Remove methods:**
- `GetThumbnailsDirectory()` - Move to utility class
- `ThumbnailPath` property - Move logic to utility class
- `ThumbnailUrl` property - Move to presentation layer
- `HasThumbnail` property - Move to utility class

**Keep:**
- `ThumbnailFilename` property (simple string storage)
- `Validation` class methods (they're pure logic)

#### 6.3b. Create Thumbnail Utility in WabbitBot.Common

**Extract thumbnail logic** into a utility class in WabbitBot.Common that can be used by services in both Core and DiscBot projects. This fits the existing architecture better than creating a full service.

**Location:** `WabbitBot.Common/Utilities/ThumbnailUtility.cs`

```csharp
// In WabbitBot.Common/Utilities/
public static class ThumbnailUtility
{
    private static string? _thumbnailsDirectory;

    /// <summary>
    /// Initialize the thumbnail utility with configuration
    /// </summary>
    public static void Initialize(IConfiguration configuration)
    {
        _thumbnailsDirectory = configuration["Bot:Maps:ThumbnailsDirectory"]
                             ?? "data/maps/thumbnails";
    }

    /// <summary>
    /// Get the full file system path for a thumbnail
    /// </summary>
    public static string? GetThumbnailPath(string? filename)
    {
        if (string.IsNullOrEmpty(filename) || _thumbnailsDirectory == null)
            return null;

        var specificPath = Path.Combine(_thumbnailsDirectory, filename);
        if (File.Exists(specificPath))
            return specificPath;

        var defaultPath = Path.Combine(_thumbnailsDirectory, "default.jpg");
        return File.Exists(defaultPath) ? defaultPath : specificPath;
    }

    /// <summary>
    /// Get Discord attachment URL for a thumbnail
    /// </summary>
    public static string? GetThumbnailUrl(string? filename)
    {
        if (string.IsNullOrEmpty(filename) || _thumbnailsDirectory == null)
            return null;

        var specificPath = Path.Combine(_thumbnailsDirectory, filename);
        if (File.Exists(specificPath))
            return $"attachment://{filename}";

        var defaultPath = Path.Combine(_thumbnailsDirectory, "default.jpg");
        return File.Exists(defaultPath) ? "attachment://default.jpg" : $"attachment://{filename}";
    }

    /// <summary>
    /// Check if a thumbnail exists (either specific or default)
    /// </summary>
    public static bool HasThumbnail(string? filename)
    {
        if (string.IsNullOrEmpty(filename) || _thumbnailsDirectory == null)
            return false;

        var specificPath = Path.Combine(_thumbnailsDirectory, filename);
        var defaultPath = Path.Combine(_thumbnailsDirectory, "default.jpg");

        return File.Exists(specificPath) || File.Exists(defaultPath);
    }

    /// <summary>
    /// Async version for better performance in UI contexts
    /// </summary>
    public static async Task<bool> ThumbnailExistsAsync(string? filename)
    {
        return await Task.Run(() => HasThumbnail(filename));
    }

    /// <summary>
    /// Get the configured thumbnails directory
    /// </summary>
    public static string? ThumbnailsDirectory => _thumbnailsDirectory;
}
```

**Initialization:** Call in `Program.cs` startup of both projects:
```csharp
// In both Core and DiscBot Program.cs
ThumbnailUtility.Initialize(builder.Configuration);
```

**Usage:** Static methods, no dependency injection needed:
```csharp
// In any service that needs thumbnail functionality
var thumbnailUrl = ThumbnailUtility.GetThumbnailUrl(map.ThumbnailFilename);
var hasThumbnail = ThumbnailUtility.HasThumbnail(map.ThumbnailFilename);
```

#### 6.3c. Update Configuration for Thumbnail Management

**Refactor appsettings.json** to support dynamic thumbnail management instead of static configuration.

```json
// ❌ BEFORE: Static map configuration with thumbnails
"Maps": {
  "Maps": [
    {
      "Name": "Airport",
      "Size": "1v1",
      "ThumbnailFilename": "WA_Airport_1v1.jpg",
      "IsInRandomPool": true,
      "IsInTournamentPool": true
    }
  ]
}

// ✅ AFTER: Separate thumbnail configuration
"Maps": {
  "ThumbnailsDirectory": "data/maps/thumbnails",
  "DefaultThumbnail": "default.jpg",
  "SupportedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".webp"],
  "MaxThumbnailSizeKB": 2048
}
```

**Benefits:**
- **Configurable directory** instead of hardcoded path
- **Centralized thumbnail settings**
- **Validation rules** in configuration
- **No file system logic** in domain entities

#### 6.3d. Update Usage in Application Code

**Refactor any code that uses the old computed properties** to use the ThumbnailUtility static methods.

```csharp
// ❌ BEFORE: Direct property access (file system I/O on every call)
var thumbnailUrl = map.ThumbnailUrl; // File.Exists() calls
var hasThumbnail = map.HasThumbnail; // File system checks

// ✅ AFTER: Utility class static methods (no dependencies needed)
using WabbitBot.Common.Utilities;

// In any application code that needs thumbnail functionality
var thumbnailUrl = ThumbnailUtility.GetThumbnailUrl(map.ThumbnailFilename);
var hasThumbnail = ThumbnailUtility.HasThumbnail(map.ThumbnailFilename);
```

#### 6.3e. Update Tests

**Update EntityConfigTests.cs** to reflect the simplified Map entity.

```csharp
[Fact]
public void MapConfig_ShouldHaveCorrectSettings()
{
    var config = EntityConfigFactory.Map;

    // Should still work - only ThumbnailFilename is stored
    Assert.Contains("thumbnail_filename", config.Columns);
    // No more computed properties to test
}
```

### **✅ SUCCESS CRITERIA:**

- ✅ **Map entity** contains only simple properties (no computed properties)
- ✅ **ThumbnailUtility** handles all file system operations
- ✅ **Configuration** supports dynamic thumbnail file management
- ✅ **Application code** uses ThumbnailUtility static methods
- ✅ **All tests pass** with simplified entity
- ✅ **Better separation** of domain logic from infrastructure concerns
- ✅ **Thumbnail files stored on disk** (not in database)

### **🔗 DEPENDENCIES:**

- **Requires:** Step 6.1 (entity relocation) - Map entity must be in Common/Models
- **Requires:** Step 6.2 (configuration cleanup) - EntityConfigurations.cs must be organized
- **Enables:** Better testability, maintainability, and performance

### **📈 IMPACT:**

**Before:** Map entity with file system coupling, complex computed properties, poor testability
**After:** Clean domain entity, separated thumbnail concerns via utility class, configurable and testable

**This significantly improves the Map entity's design** by removing infrastructure dependencies while fitting within the existing Common utilities architecture! 🎯
