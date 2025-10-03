# Season/Leaderboard Relationship Redesign

**Date:** 2025-10-01  
**Status:** 🔴 NEEDS REDESIGN  
**Issue:** Missing relationships, overengineered structure

---

## Current Problems

### 1. ❌ No Season ↔ Leaderboard Relationship

**Current:**
```csharp
public class Season : Entity
{
    public Guid SeasonGroupId { get; set; }
    public TeamSize TeamSize { get; set; }  // One season per TeamSize!
    // NO reference to Leaderboard
}

public class Leaderboard : Entity
{
    public Dictionary<TeamSize, Dictionary<string, LeaderboardItem>> Rankings { get; set; }
    // NO reference to Season
}
```

**Problem:** Seasons and Leaderboards are completely disconnected - can't tell which Leaderboard belongs to which Season!

---

### 2. ❌ Overengineered SeasonGroup

**Current Design:**
- One **SeasonGroup** per actual season (e.g., "Fall 2024")
- Four **Season** entities per SeasonGroup (one for each TeamSize: 1v1, 2v2, 3v3, 4v4)
- All four Seasons share same start/end dates
- SeasonGroup exists only to coordinate these 4 Seasons

**Example:**
```
SeasonGroup: "Fall 2024"
├─ Season (1v1) - StartDate: Oct 1, EndDate: Dec 31
├─ Season (2v2) - StartDate: Oct 1, EndDate: Dec 31
├─ Season (3v3) - StartDate: Oct 1, EndDate: Dec 31
└─ Season (4v4) - StartDate: Oct 1, EndDate: Dec 31
```

**Problems:**
- 4 entities to represent ONE conceptual season
- Duplicate data (StartDate, EndDate, IsActive, etc.)
- Extra layer of indirection (SeasonGroup)
- No navigation properties between Season and SeasonGroup

---

### 3. ❌ Leaderboard Already Supports All TeamSizes

**Current:**
```csharp
public class Leaderboard : Entity
{
    // Dictionary key is TeamSize - so ONE Leaderboard holds ALL team sizes!
    public Dictionary<TeamSize, Dictionary<string, LeaderboardItem>> Rankings { get; set; }
}
```

**Observation:** One Leaderboard can already store rankings for ALL TeamSizes via the dictionary key!

So why do we need 4 separate Season entities?

---

## Design Options

### Option A: User's Suggestion - One Season, Multiple Leaderboards

```csharp
public class Season : Entity
{
    public string Name { get; set; }  // "Fall 2024"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public Guid SeasonConfigId { get; set; }
    
    // One Leaderboard per TeamSize
    public ICollection<Leaderboard> Leaderboards { get; set; }  // 4 leaderboards
}

public class Leaderboard : Entity
{
    public Guid SeasonId { get; set; }
    public TeamSize TeamSize { get; set; }  // Which size this leaderboard is for
    public Dictionary<string, LeaderboardItem> Rankings { get; set; }  // Simplified
    
    public Season Season { get; set; }
}
```

**Pros:**
- ✅ Clear Season ↔ Leaderboard relationship
- ✅ One Season entity per actual season
- ✅ No SeasonGroup needed
- ✅ Navigation properties work well

**Cons:**
- Changes Leaderboard structure (removes nested TeamSize dictionary)
- Still have 4 Leaderboard entities per Season

---

### Option B: Simpler - One Season, One Leaderboard

```csharp
public class Season : Entity
{
    public string Name { get; set; }  // "Fall 2024"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public Guid SeasonConfigId { get; set; }
    public Guid LeaderboardId { get; set; }  // One-to-One
    
    // Navigation
    public Leaderboard Leaderboard { get; set; }
    public SeasonConfig Config { get; set; }
}

public class Leaderboard : Entity
{
    public Guid SeasonId { get; set; }
    
    // Already supports all TeamSizes via dictionary!
    public Dictionary<TeamSize, Dictionary<string, LeaderboardItem>> Rankings { get; set; }
    
    // Navigation
    public Season Season { get; set; }
}
```

**Pros:**
- ✅ Simplest design
- ✅ One Season = One Leaderboard (1:1 relationship)
- ✅ Leaderboard already handles all TeamSizes
- ✅ No SeasonGroup needed
- ✅ Minimal entity count

**Cons:**
- Leaderboard entity mixes all TeamSizes (but it already does this!)

---

### Option C: Keep SeasonGroup but Add Navigation

```csharp
public class SeasonGroup : Entity
{
    public string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    // Navigation to the 4 seasons
    public ICollection<Season> Seasons { get; set; }
}

public class Season : Entity
{
    public Guid SeasonGroupId { get; set; }
    public TeamSize TeamSize { get; set; }
    public Guid LeaderboardId { get; set; }  // ADD THIS
    
    // Navigation
    public SeasonGroup SeasonGroup { get; set; }  // ADD THIS
    public Leaderboard Leaderboard { get; set; }  // ADD THIS
}

public class Leaderboard : Entity
{
    public Guid SeasonId { get; set; }  // ADD THIS
    public TeamSize TeamSize { get; set; }  // ADD THIS
    public Dictionary<string, LeaderboardItem> Rankings { get; set; }
    
    // Navigation
    public Season Season { get; set; }  // ADD THIS
}
```

**Pros:**
- ✅ Minimal changes to existing structure
- ✅ Adds missing relationships

**Cons:**
- ❌ Still overengineered (4 Season entities per actual season)
- ❌ Still have SeasonGroup layer
- ❌ Still duplicate data across 4 Seasons

---

## Recommendation: **Option B** ✅

### Why Option B is Best

1. **Simplest Model**
   - One Season entity per actual season
   - One Leaderboard per Season
   - No extra coordination entities

2. **Leverages Existing Structure**
   - `Leaderboard` already has `Dictionary<TeamSize, ...>`
   - Don't need to change Leaderboard's internal structure
   - Just add Season relationship

3. **Cleaner Queries**
   ```csharp
   // Get current season
   var season = await context.Seasons
       .Include(s => s.Leaderboard)
       .Where(s => s.IsActive)
       .FirstOrDefaultAsync();
   
   // Get 1v1 rankings
   var rankings1v1 = season.Leaderboard.Rankings[TeamSize.OneVOne];
   
   // Get all rankings for all sizes
   var allRankings = season.Leaderboard.Rankings;
   ```

4. **Natural Domain Model**
   - A season IS one thing, not four things
   - "Fall 2024 Season" - singular, not plural
   - One leaderboard shows all team sizes

---

## Migration Path

### Step 1: Add Navigation Properties

```csharp
public class Season : Entity
{
    // Keep existing for backward compatibility temporarily
    public Guid SeasonGroupId { get; set; }  // Will deprecate
    public TeamSize TeamSize { get; set; }    // Will deprecate
    
    // NEW: Add these
    public Guid? LeaderboardId { get; set; }
    public virtual Leaderboard? Leaderboard { get; set; }
    public virtual SeasonConfig Config { get; set; }
    
    // Existing
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public Guid SeasonConfigId { get; set; }
}

public class Leaderboard : Entity
{
    // NEW: Add these
    public Guid? SeasonId { get; set; }
    public virtual Season? Season { get; set; }
    
    // Existing - already supports all TeamSizes!
    public Dictionary<TeamSize, Dictionary<string, LeaderboardItem>> Rankings { get; set; }
}
```

### Step 2: Deprecate SeasonGroup

```csharp
[Obsolete("SeasonGroup is deprecated. Seasons no longer need grouping.")]
public class SeasonGroup : Entity
{
    // Keep for backward compatibility, but mark as obsolete
}
```

### Step 3: Refactor Season Creation

```csharp
// OLD WAY (4 entities per season):
var seasonGroup = new SeasonGroup { Name = "Fall 2024" };
var season1v1 = new Season { SeasonGroupId = seasonGroup.Id, TeamSize = TeamSize.OneVOne };
var season2v2 = new Season { SeasonGroupId = seasonGroup.Id, TeamSize = TeamSize.TwoVTwo };
var season3v3 = new Season { SeasonGroupId = seasonGroup.Id, TeamSize = TeamSize.ThreeVThree };
var season4v4 = new Season { SeasonGroupId = seasonGroup.Id, TeamSize = TeamSize.FourVFour };

// NEW WAY (1 entity per season):
var leaderboard = new Leaderboard
{
    Rankings = new Dictionary<TeamSize, Dictionary<string, LeaderboardItem>>
    {
        [TeamSize.OneVOne] = new(),
        [TeamSize.TwoVTwo] = new(),
        [TeamSize.ThreeVThree] = new(),
        [TeamSize.FourVFour] = new(),
    }
};

var season = new Season
{
    Name = "Fall 2024",
    StartDate = new DateTime(2024, 10, 1),
    EndDate = new DateTime(2024, 12, 31),
    IsActive = true,
    LeaderboardId = leaderboard.Id,
    Leaderboard = leaderboard,
};
```

---

## Alternative: If You Need Per-TeamSize Seasons

**If there's a business reason** to have separate seasons per TeamSize (e.g., they can start/end at different times), then:

### Option D: Separate But Simpler

```csharp
public class Season : Entity
{
    public string Name { get; set; }  // "Fall 2024 - 1v1"
    public TeamSize TeamSize { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public Guid LeaderboardId { get; set; }  // One-to-One
    
    // Optional: If seasons are coordinated
    public string? SeasonGroupName { get; set; }  // Just a string, not an entity
    
    // Navigation
    public Leaderboard Leaderboard { get; set; }
}

public class Leaderboard : Entity
{
    public Guid SeasonId { get; set; }
    public TeamSize TeamSize { get; set; }
    public Dictionary<string, LeaderboardItem> Rankings { get; set; }  // Single TeamSize
    
    // Navigation
    public Season Season { get; set; }
}
```

**When to use this:**
- Seasons CAN start/end at different times per TeamSize
- Need independent activation per TeamSize
- Business logic differs per TeamSize

**When NOT to use this:**
- All TeamSizes start/end together (current case)
- Same rules for all TeamSizes
- Just grouping for display purposes

---

## Questions to Answer

1. **Can different TeamSizes have different season dates?**
   - If NO → Use Option B (one Season, one Leaderboard with dictionary)
   - If YES → Use Option D (separate Seasons, one Leaderboard each)

2. **Do you ever need to query "all 1v1 seasons" separately?**
   - If NO → Option B is simpler
   - If YES → Option D might be better

3. **Is there any business logic that differs per TeamSize?**
   - If NO → Option B
   - If YES → Option D

---

## Summary Table

| Aspect | Current | Option A | Option B ✅ | Option C | Option D |
|--------|---------|----------|-------------|----------|----------|
| Seasons per actual season | 4 | 1 | 1 | 4 | 4 |
| Leaderboards per Season | ? | 4 | 1 | 1 | 1 |
| SeasonGroup needed | Yes | No | No | Yes | No |
| Season ↔ Leaderboard nav | ❌ | ✅ | ✅ | ✅ | ✅ |
| Season ↔ SeasonGroup nav | ❌ | N/A | N/A | ✅ | N/A |
| Complexity | High | Medium | **Low** | High | Medium |
| Best for | N/A | Varied dates | Same dates | Legacy | Varied dates |

---

## Recommended Implementation: Option B

```csharp
/// <summary>
/// Represents a competitive season across all team sizes.
/// Each season has one leaderboard that tracks rankings for all TeamSize values.
/// </summary>
public class Season : Entity, ILeaderboardEntity
{
    public string Name { get; set; } = string.Empty;  // "Fall 2024"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    
    // Configuration
    public Guid SeasonConfigId { get; set; }
    public virtual SeasonConfig Config { get; set; } = null!;
    
    // Leaderboard (one-to-one)
    public Guid LeaderboardId { get; set; }
    public virtual Leaderboard Leaderboard { get; set; } = null!;
    
    // Teams participating (across all sizes)
    public Dictionary<string, string> ParticipatingTeams { get; set; } = new();
    
    public override Domain Domain => Domain.Leaderboard;
}

/// <summary>
/// Leaderboard for a season, tracking rankings for all team sizes.
/// </summary>
public class Leaderboard : Entity, ILeaderboardEntity
{
    public Guid SeasonId { get; set; }
    public virtual Season Season { get; set; } = null!;
    
    // Rankings organized by TeamSize, then by team/player identifier
    public Dictionary<TeamSize, Dictionary<string, LeaderboardItem>> Rankings { get; set; } = new();
    
    public override Domain Domain => Domain.Leaderboard;
}

/// <summary>
/// [DEPRECATED] SeasonGroup is no longer used.
/// Seasons are now independent entities with their own leaderboards.
/// </summary>
[Obsolete("SeasonGroup is deprecated. Use Season directly.")]
public class SeasonGroup : Entity, ILeaderboardEntity
{
    public string Name { get; set; } = string.Empty;
    public override Domain Domain => Domain.Leaderboard;
}
```

---

## Next Steps

1. ✅ Document the design decision
2. Add navigation properties to Season and Leaderboard
3. Update Season creation logic
4. Migrate existing data (consolidate 4 Seasons → 1 Season)
5. Deprecate SeasonGroup
6. Update queries and business logic
7. Remove TeamSize property from Season

