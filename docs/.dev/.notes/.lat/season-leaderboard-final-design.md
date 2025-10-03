# Season/Leaderboard Final Design (TPH Inheritance)

**Date:** 2025-10-01  
**Status:** ✅ APPROVED - Ready for Implementation  
**Approach:** Table-per-Hierarchy (TPH) Inheritance

---

## Design Summary

### Key Changes

1. ✅ **One Season = One Actual Season** (remove `TeamSize` from Season)
2. ✅ **Inheritance:** `ScrimmageLeaderboard` and `TournamentLeaderboard` extend `Leaderboard`
3. ✅ **Clear Navigation:** Season ↔ Leaderboard ↔ LeaderboardItem
4. ✅ **Deprecate SeasonGroup** (no longer needed)
5. ✅ **8 Leaderboards per Season** (4 scrimmage + 4 tournament, one per TeamSize each)

### Structure Per Season

```
Season: "Fall 2024"
├─ ScrimmageLeaderboard (1v1)
│  └─ LeaderboardItems (with Rating, Wins, Losses)
├─ ScrimmageLeaderboard (2v2)
├─ ScrimmageLeaderboard (3v3)
├─ ScrimmageLeaderboard (4v4)
├─ TournamentLeaderboard (1v1)
│  └─ LeaderboardItems (with TournamentPoints, Placements)
├─ TournamentLeaderboard (2v2)
├─ TournamentLeaderboard (3v3)
└─ TournamentLeaderboard (4v4)
```

---

## Entity Definitions

### 1. Leaderboard (Abstract Base)

```csharp
[EntityMetadata(tableName: "leaderboards", ...)]
public abstract class Leaderboard : Entity, ILeaderboardEntity
{
    public Guid SeasonId { get; set; }
    public virtual Season Season { get; set; } = null!;
    public TeamSize TeamSize { get; set; }
    public virtual ICollection<LeaderboardItem> Items { get; set; } = new List<LeaderboardItem>();
    
    public override Domain Domain => Domain.Leaderboard;
}
```

### 2. ScrimmageLeaderboard

```csharp
public class ScrimmageLeaderboard : Leaderboard
{
    public double InitialRating { get; set; } = 1500.0;
    public double KFactor { get; set; } = 32.0;
    public bool VarietyBonusEnabled { get; set; } = true;
    public DateTime? LastRatingUpdate { get; set; }
}
```

### 3. TournamentLeaderboard

```csharp
public class TournamentLeaderboard : Leaderboard
{
    public Dictionary<int, int> PlacementPoints { get; set; } = new() { [1] = 100, [2] = 75, [3] = 50 };
    public int MinTournamentsForRanking { get; set; } = 1;
    public bool UseAveragePlacement { get; set; } = false;
    public DateTime? LastTournamentCompleted { get; set; }
}
```

### 4. LeaderboardItem

```csharp
[EntityMetadata(tableName: "leaderboard_items", ...)]
public class LeaderboardItem : Entity, ILeaderboardEntity
{
    public Guid LeaderboardId { get; set; }
    public virtual Leaderboard Leaderboard { get; set; } = null!;
    
    public Guid TeamId { get; set; }
    public List<Guid> PlayerIds { get; set; } = new();
    public string Name { get; set; } = string.Empty;
    public bool IsTeam { get; set; }
    public int Rank { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // SCRIMMAGE PROPERTIES (used when parent is ScrimmageLeaderboard)
    public int Wins { get; set; }
    public int Losses { get; set; }
    public double Rating { get; set; }
    public double Confidence { get; set; }
    public double? PeakRating { get; set; }
    
    // TOURNAMENT PROPERTIES (used when parent is TournamentLeaderboard)
    public int? TournamentPoints { get; set; }
    public List<int> TournamentPlacements { get; set; } = new();
    public int? TournamentsPlayed { get; set; }
    public double? AveragePlacement { get; set; }
    public int? BestPlacement { get; set; }
    
    public override Domain Domain => Domain.Leaderboard;
}
```

**Note:** Single `LeaderboardItem` class with type-specific properties. Alternative (separate classes per type) rejected as overly complex.

### 5. Season

```csharp
[EntityMetadata(tableName: "seasons", ...)]
public class Season : Entity, ILeaderboardEntity
{
    public string Name { get; set; } = string.Empty;  // "Fall 2024"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    
    public Guid SeasonConfigId { get; set; }
    public virtual SeasonConfig Config { get; set; } = null!;
    
    // Polymorphic collection (contains both ScrimmageLeaderboard and TournamentLeaderboard)
    public virtual ICollection<Leaderboard> Leaderboards { get; set; } = new List<Leaderboard>();
    
    // Helper properties for type-safe access
    public IEnumerable<ScrimmageLeaderboard> ScrimmageLeaderboards => 
        Leaderboards.OfType<ScrimmageLeaderboard>();
    public IEnumerable<TournamentLeaderboard> TournamentLeaderboards => 
        Leaderboards.OfType<TournamentLeaderboard>();
    
    // Helper methods
    public ScrimmageLeaderboard? GetScrimmageLeaderboard(TeamSize size) =>
        ScrimmageLeaderboards.FirstOrDefault(l => l.TeamSize == size);
    public TournamentLeaderboard? GetTournamentLeaderboard(TeamSize size) =>
        TournamentLeaderboards.FirstOrDefault(l => l.TeamSize == size);
    
    public override Domain Domain => Domain.Leaderboard;
}
```

**Removed:** `TeamSize` property (no longer needed), `SeasonGroupId` (deprecated)

### 6. SeasonGroup (DEPRECATED)

```csharp
[Obsolete("SeasonGroup is deprecated. Use Season directly.")]
public class SeasonGroup : Entity, ILeaderboardEntity
{
    public string Name { get; set; } = string.Empty;
    public override Domain Domain => Domain.Leaderboard;
}
```

---

## Database Schema (TPH)

```sql
-- Leaderboards: Single table with discriminator
CREATE TABLE leaderboards (
    id UUID PRIMARY KEY,
    season_id UUID NOT NULL,
    team_size INTEGER NOT NULL,
    leaderboard_type VARCHAR(50) NOT NULL,  -- "Scrimmage" or "Tournament"
    
    -- Scrimmage columns (nullable)
    initial_rating REAL,
    k_factor REAL,
    variety_bonus_enabled BOOLEAN,
    last_rating_update TIMESTAMP,
    
    -- Tournament columns (nullable)
    placement_points JSONB,
    min_tournaments_for_ranking INTEGER,
    use_average_placement BOOLEAN,
    last_tournament_completed TIMESTAMP,
    
    CONSTRAINT fk_leaderboards_season FOREIGN KEY (season_id) REFERENCES seasons(id)
);

-- LeaderboardItems: Standard table
CREATE TABLE leaderboard_items (
    id UUID PRIMARY KEY,
    leaderboard_id UUID NOT NULL,
    team_id UUID NOT NULL,
    rank INTEGER NOT NULL,
    
    -- Scrimmage properties (nullable)
    wins INTEGER,
    losses INTEGER,
    rating REAL,
    peak_rating REAL,
    
    -- Tournament properties (nullable)
    tournament_points INTEGER,
    tournament_placements INTEGER[],
    average_placement REAL,
    
    CONSTRAINT fk_leaderboard_items_leaderboard FOREIGN KEY (leaderboard_id) REFERENCES leaderboards(id)
);

-- Seasons: Simplified (no team_size, no season_group_id)
CREATE TABLE seasons (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,
    is_active BOOLEAN NOT NULL,
    season_config_id UUID NOT NULL
);
```

---

## Usage Examples

### Creating a Season

```csharp
var season = new Season { Name = "Fall 2024", StartDate = oct1, EndDate = dec31, IsActive = true };

foreach (TeamSize size in Enum.GetValues<TeamSize>())
{
    season.Leaderboards.Add(new ScrimmageLeaderboard 
    { 
        TeamSize = size, 
        InitialRating = 1500 
    });
    
    season.Leaderboards.Add(new TournamentLeaderboard 
    { 
        TeamSize = size,
        PlacementPoints = new() { [1] = 100, [2] = 75 }
    });
}
// Result: 8 leaderboards
```

### Querying

```csharp
// Get current season
var season = await context.Seasons
    .Include(s => s.Leaderboards).ThenInclude(l => l.Items)
    .Where(s => s.IsActive)
    .FirstAsync();

// Type-safe access
var scrimmage1v1 = season.GetScrimmageLeaderboard(TeamSize.OneVOne);
var tournament2v2 = season.GetTournamentLeaderboard(TeamSize.TwoVTwo);

// Type-specific query
var allScrimmageBoards = await context.Leaderboards
    .OfType<ScrimmageLeaderboard>()
    .Where(l => l.Season.IsActive)
    .ToListAsync();
```

### Pattern Matching

```csharp
foreach (var board in season.Leaderboards)
{
    if (board is ScrimmageLeaderboard scrimmage)
    {
        Console.WriteLine($"Scrimmage {board.TeamSize}: K-Factor={scrimmage.KFactor}");
    }
    else if (board is TournamentLeaderboard tournament)
    {
        Console.WriteLine($"Tournament {board.TeamSize}: {tournament.TotalTournaments} events");
    }
}
```

---

## Benefits

✅ **Semantic Clarity:** One instance = one actual leaderboard  
✅ **Type Safety:** Compile-time checking with pattern matching  
✅ **Queryability:** `OfType<>` and LINQ support  
✅ **Performance:** Single table, no joins required  
✅ **Extensibility:** Easy to add `RankedLeaderboard`, etc.  
✅ **Navigation:** Clear relationships throughout

## Trade-offs

⚠️ **Nullable Columns:** Some columns null depending on type (acceptable - small number of columns)  
⚠️ **LeaderboardItem Mixed Properties:** Single entity has both types (acceptable - simpler than separate types)

---

## Implementation Status

- [ ] Update `Leaderboard.cs` with inheritance
- [ ] Remove `TeamSize` and `SeasonGroupId` from `Season`
- [ ] Add navigation properties
- [ ] Update database migrations
- [ ] Migrate existing data
- [ ] Update queries and business logic
- [ ] Mark `SeasonGroup` as obsolete

