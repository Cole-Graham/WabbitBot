#### Step 5: Additional Entity Integration ✅ COMPLETE

### Entity Architecture Consolidation

**🎯 CRITICAL**: Remove legacy interfaces and consolidate all entities under the new `Entity` base class with native PostgreSQL JSON support.

#### 5a. Integrate Stats Entity (Remove IJsonVersioned, Inherit from Entity)

Replace manual JSON versioning with clean Entity inheritance and native JSONB support.

```csharp
// ❌ OLD: Manual versioning and inheritance
public class Stats : IJsonVersioned
{
    public int SchemaVersion { get; set; }  // Remove this
    [JsonPropertyName("gamesPlayed")]
    public string GamesPlayedJson { get; set; }  // Remove this
}

// ✅ NEW: Clean Entity inheritance
public class PlayerStats : Entity
{
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public decimal WinRate => GamesPlayed > 0 ? (decimal)Wins / GamesPlayed : 0;
}
```

#### 5b. Integrate SeasonConfig Entity (Runtime Season Configuration)

Create runtime-configurable season entities that inherit from the base Entity class.

```csharp
// SeasonConfig.cs - Runtime season configuration
public class SeasonConfig : Entity
{
    public string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SeasonSettings Settings { get; set; } = new();
}

public class SeasonSettings
{
    public int MaxTeams { get; set; }
    public int MaxPlayersPerTeam { get; set; }
    public GameSize DefaultGameSize { get; set; }
    public Dictionary<string, object> CustomRules { get; set; } = new();
}
```

#### 5c. Integrate SeasonGroup Entity (Season Grouping)

Implement season grouping for tournament organization and management.

```csharp
// SeasonGroup.cs - Season grouping and organization
public class SeasonGroup : Entity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> SeasonIds { get; set; } = new();
    public GroupSettings Settings { get; set; } = new();
}

public class GroupSettings
{
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
```

#### 5d. Integrate LeaderboardItem Entity (Extract from Leaderboard)

Extract leaderboard entries into separate entities for better data management.

```csharp
// Leaderboard.cs - Individual leaderboard entries
public class LeaderboardItem : Entity
{
    public List<string> PlayerIds { get; set; } = new();
    public string TeamId { get; set; }
    public GameSize GameSize { get; set; }
    public decimal Rating { get; set; }
    public int Rank { get; set; }
    public LeaderboardStats Stats { get; set; } = new();
}

public class LeaderboardStats
{
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public DateTime LastGameAt { get; set; }
}
```

#### 5e. Handle IJsonVersioned Interface Removal

Remove the legacy IJsonVersioned interface and all manual JSON versioning code.

```csharp
// ❌ REMOVE: This entire interface and all implementations
public interface IJsonVersioned
{
    int SchemaVersion { get; set; }
}

// ✅ MIGRATION: Update all classes that implemented IJsonVersioned
// Replace with clean Entity inheritance
public class Player : Entity  // Instead of : IJsonVersioned
{
    // Remove SchemaVersion property
    // Remove all [JsonPropertyName] manual serialization
    // Use native JSONB properties directly
}
```

#### STEP 5 IMPACT:

### Entity Hierarchy Simplification

#### Before (Complex Inheritance):
```csharp
// ❌ OLD: Multiple interfaces and complex inheritance
public class Player : IJsonVersioned  // Legacy interface
{
    public int SchemaVersion { get; set; }  // Manual versioning
    [JsonPropertyName("TeamIds")]
    public string TeamIdsJson { get; set; }  // Manual JSON
}

// ❌ OLD: Inconsistent entity patterns
public class Stats : IJsonVersioned  // Another legacy interface
{
    public int SchemaVersion { get; set; }
    // Manual serialization everywhere
}
```

#### After (Clean Entity Base Class):
```csharp
// ✅ NEW: Single Entity base class
public class Player : Entity  // Clean inheritance
{
    // No manual JSON properties!
    public List<TeamMembership> TeamMemberships { get; set; } = new();
    public PlayerStats Stats { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PlayerStats : Entity  // Consistent pattern
{
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public decimal WinRate => GamesPlayed > 0 ? (decimal)Wins / GamesPlayed : 0;
}
```

### Benefits of Entity Consolidation

1. **🎯 Consistency**: All entities inherit from `Entity` base class
2. **🧹 Clean Code**: No more manual JSON serialization properties
3. **🔒 Type Safety**: Strongly-typed complex objects
4. **📈 Scalability**: Native PostgreSQL JSONB support
5. **🛠️ Maintainability**: Single inheritance hierarchy
6. **🚀 Performance**: No serialization overhead

### Entity Configuration Pattern

```csharp
// Entity configurations define database mappings
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

**This entity integration step consolidates our data model under a clean, consistent architecture!** 🎯
