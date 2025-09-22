#### 1a. Entity Redefinition for Native PostgreSQL JSON Support

### Zero Backwards Compatibility - Complete Redesign Freedom

**🎯 CRITICAL ADVANTAGE**: Since this application has **no production deployments or existing data**, we can completely redesign entity classes to maximize PostgreSQL JSON capabilities.

### Current Entity Problems (To Fix)

```csharp
// ❌ CURRENT: Manual JSON serialization (BAD)
public class Player : Entity
{
    public List<string> TeamIds { get; set; } = new();

    // Manual JSON properties - REMOVE THESE!
    [JsonPropertyName("TeamIds")]
    public string TeamIdsJson
    {
        get => JsonUtil.Serialize(TeamIds);
        set => TeamIds = JsonUtil.Deserialize<List<string>>(value) ?? new();
    }
}
```

### ✅ NEW: Native PostgreSQL JSON Support

```csharp
// ✅ FUTURE: Native PostgreSQL JSON (GOOD)
public class Player : Entity
{
    // Direct complex objects - Npgsql handles JSON automatically
    public List<TeamMembership> TeamMemberships { get; set; } = new();
    public PlayerStats Stats { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Nested complex objects work natively
    public class TeamMembership
    {
        public string TeamId { get; set; }
        public string Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class PlayerStats
    {
        public int GamesPlayed { get; set; }
        public int Wins { get; set; }
        public decimal WinRate => GamesPlayed > 0 ? (decimal)Wins / GamesPlayed : 0;
    }
}
```

#### 1b. **Remove Manual JSON Properties**
```csharp
// ❌ REMOVE these patterns:
[JsonPropertyName("Field")]
public string FieldJson { get => JsonUtil.Serialize(Field); set => Field = JsonUtil.Deserialize<T>(value); }
```

#### 1c. **Use Native Complex Objects**
```csharp
// ✅ REPLACE with direct complex objects:
public List<TeamMembership> TeamMemberships { get; set; } = new();
public Dictionary<string, PlayerAchievement> Achievements { get; set; } = new();
```

#### 1d. **Leverage PostgreSQL JSONB Features**
```csharp
// ✅ PostgreSQL JSONB supports:
public class MatchResult
{
    public string WinnerId { get; set; }
    public Dictionary<string, int> PlayerScores { get; set; } = new();
    public List<GameResult> Games { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}
```

#### 1e. **Npgsql Automatic Mapping**
- No manual serialization needed
- Complex nested objects work automatically
- JSON queries are optimized by PostgreSQL
- Type-safe operations with LINQ support

#### STEP 1 IMPACT:

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