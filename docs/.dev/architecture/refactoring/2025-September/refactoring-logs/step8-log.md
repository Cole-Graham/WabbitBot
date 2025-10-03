# Step 8 Implementation Log: Entity Migration

**Status:** ❌ OBSOLETE (Design Changed)  
**Date Range:** N/A (retroactive documentation)  
**Implementation Type:** Superseded by Entity + EntityCore pattern

---

## Overview

**Step 8 was based on an outdated design assumption and was never implemented as originally planned.**

The original Step 8 document outlined a migration strategy to move entity-specific business logic from individual services (PlayerService, TeamService, etc.) into CoreService partials (CoreService.Player.cs, CoreService.Team.cs, etc.).

However, the actual architecture evolved differently:
- Business logic was placed in **EntityCore files** (`MatchCore.cs`, `PlayerCore.cs`, etc.)
- CoreService remained focused on **infrastructure orchestration** only
- Individual entity services were never created, so no migration was needed

---

## Original Plan (Never Implemented)

### Planned Migration Strategy

**Before (Individual Services):**
```csharp
// This pattern was never implemented
public class PlayerService
{
    public async Task<Player?> GetPlayerByIdAsync(Guid playerId) { /* ... */ }
    public async Task<Result<Player>> CreatePlayerAsync(Player player) { /* ... */ }
}
```

**After (CoreService Partials):**
```csharp
// This pattern was also never implemented
public partial class CoreService
{
    #region Player Business Logic
    public async Task<Player?> GetPlayerAsync(Guid playerId)
    {
        return await _playerData.GetByIdAsync(playerId);
    }
    #endregion
}
```

### Why This Was Never Done

1. **Individual Services Never Existed:** The refactoring timeline went directly to DatabaseService<TEntity> without creating intermediate entity-specific services
2. **Better Pattern Emerged:** EntityCore files provide better separation of concerns
3. **No Migration Needed:** No legacy services to migrate from

---

## Actual Implementation (EntityCore Pattern)

### What Was Actually Built

Instead of migrating from services to CoreService partials, the project uses:

**Entity Definition:**
```csharp
// File: src/WabbitBot.Core/Common/Models/Common/Player.cs
[EntityMetadata(TableName = "players", Domain = "Common")]
public partial class Player : Entity
{
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime LastActive { get; set; }
    public List<Guid> TeamIds { get; set; } = new();
}
```

**Business Logic (EntityCore):**
```csharp
// File: src/WabbitBot.Core/Common/Models/Common/PlayerCore.cs
public partial class Player
{
    /// <summary>
    /// Factory method to create a new player
    /// </summary>
    public static Player Create(string name, Guid userId)
    {
        return new Player
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId,
            LastActive = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
    
    /// <summary>
    /// Business logic: join a team
    /// </summary>
    public Result JoinTeam(Guid teamId)
    {
        if (TeamIds.Contains(teamId))
            return Result.Failure("Player is already on this team");
        
        TeamIds.Add(teamId);
        UpdatedAt = DateTime.UtcNow;
        
        return Result.Success();
    }
}
```

**Data Access (CoreService):**
```csharp
// File: src/WabbitBot.Core/Common/Services/Core/DatabaseServiceAccessors.g.cs (generated)
public static partial class CoreService
{
    public static DatabaseService<Player> Players => _lazyPlayers!.Value;
}
```

**Usage:**
```csharp
// Create player with business logic
var player = Player.Create("PlayerName", userId);

// Persist via CoreService
var result = await CoreService.Players.CreateAsync(player);

// Business operations
player.JoinTeam(teamId);
await CoreService.Players.UpdateAsync(player);
```

---

## Why EntityCore Pattern is Better

### 1. Co-location ✅
- Business logic lives with entity definition
- Easy to find all entity-related code
- Changes to entity behavior are localized

### 2. Separation of Concerns ✅
- **Entity.cs:** Data structure (properties)
- **EntityCore.cs:** Business logic (methods)
- **CoreService:** Data access orchestration
- **DatabaseService:** CRUD operations

### 3. Discoverability ✅
```
src/WabbitBot.Core/Common/Models/Common/
├── Player.cs          # Find entity definition here
└── PlayerCore.cs      # Find entity business logic here
```
vs.
```
src/WabbitBot.Core/Common/Services/Core/
├── CoreService.cs
├── CoreService.Player.cs     # Hard to find among many partials
├── CoreService.Team.cs
├── CoreService.Match.cs
└── ... many more partials
```

### 4. Partial Class Benefits ✅
- Entity and EntityCore extend the same class
- Methods can access properties directly
- No need to pass entity as parameter everywhere

### 5. Generator-Friendly ✅
- Generators emit entity data model
- Manual EntityCore extends with business logic
- No conflicts between generated and manual code

---

## Comparison: Original Plan vs Actual Implementation

| Aspect | Original Plan (Step 8) | Actual Implementation |
|--------|------------------------|----------------------|
| **Business Logic Location** | CoreService partials | EntityCore files |
| **File Count** | Many CoreService.{Entity}.cs files | One EntityCore per entity |
| **Discoverability** | Scattered in CoreService directory | Co-located with entity |
| **Coupling** | CoreService knows all entities | CoreService only provides access |
| **Extensibility** | Add more CoreService partials | Add EntityCore methods |
| **Testing** | Test CoreService methods | Test entity methods directly |

---

## EntityCore Files Created

The following EntityCore files were created instead of CoreService partials:

### Common Domain
- `src/WabbitBot.Core/Common/Models/Common/PlayerCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/TeamCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/GameCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/MapCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/UserCore.cs`
- `src/WabbitBot.Core/Common/Models/Common/GameStateSnapshotCore.cs`

### Leaderboard Domain
- `src/WabbitBot.Core/Leaderboard/Models/LeaderboardCore.cs`
- `src/WabbitBot.Core/Leaderboard/Models/LeaderboardItemCore.cs`
- `src/WabbitBot.Core/Leaderboard/Models/StatsCore.cs`
- `src/WabbitBot.Core/Leaderboard/Models/SeasonConfigCore.cs`
- `src/WabbitBot.Core/Leaderboard/Models/SeasonGroupCore.cs`

### Scrimmage Domain
- `src/WabbitBot.Core/Scrimmage/Models/MatchCore.cs`
- `src/WabbitBot.Core/Scrimmage/Models/MatchPlayerCore.cs`
- `src/WabbitBot.Core/Scrimmage/Models/ScrimmageCore.cs`

### Tournament Domain
- `src/WabbitBot.Core/Tournament/Models/TournamentCore.cs`
- `src/WabbitBot.Core/Tournament/Models/TournamentMatchCore.cs`
- `src/WabbitBot.Core/Tournament/Models/TournamentTeamCore.cs`

---

## Business Logic Patterns in EntityCore

### Factory Methods
```csharp
public partial class Match
{
    public static Match Create(Guid team1Id, Guid team2Id, Guid mapId, 
        List<Guid> team1Players, List<Guid> team2Players)
    {
        return new Match
        {
            Id = Guid.NewGuid(),
            Team1Id = team1Id,
            Team2Id = team2Id,
            MapId = mapId,
            Team1PlayerIds = team1Players,
            Team2PlayerIds = team2Players,
            Status = MatchStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }
}
```

### Validation Methods
```csharp
public partial class Match
{
    public Result CanStart()
    {
        if (Status != MatchStatus.Pending)
            return Result.Failure("Match must be in Pending status");
        
        if (Team1PlayerIds.Count == 0 || Team2PlayerIds.Count == 0)
            return Result.Failure("Both teams must have players");
        
        return Result.Success();
    }
}
```

### State Transition Methods
```csharp
public partial class Match
{
    public Result CompleteMatch(Guid? winnerId)
    {
        if (Status != MatchStatus.InProgress)
            return Result.Failure("Match must be in progress");
        
        if (winnerId.HasValue && winnerId != Team1Id && winnerId != Team2Id)
            return Result.Failure("Winner must be a participating team");
        
        Status = MatchStatus.Completed;
        WinnerId = winnerId;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        return Result.Success();
    }
}
```

### Business Operations
```csharp
public partial class Player
{
    public Result JoinTeam(Guid teamId)
    {
        if (TeamIds.Contains(teamId))
            return Result.Failure("Already on team");
        
        TeamIds.Add(teamId);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
    
    public Result LeaveTeam(Guid teamId)
    {
        if (!TeamIds.Contains(teamId))
            return Result.Failure("Not on team");
        
        TeamIds.Remove(teamId);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
    
    public void UpdateLastActive()
    {
        LastActive = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

---

## Testing Strategy

### Entity Logic Testing
```csharp
// Test EntityCore methods directly
public class PlayerCoreTests
{
    [Fact]
    public void Player_Create_SetsDefaults()
    {
        var player = Player.Create("TestPlayer", Guid.NewGuid());
        
        Assert.NotEqual(Guid.Empty, player.Id);
        Assert.Equal("TestPlayer", player.Name);
        Assert.True(player.CreatedAt > DateTime.UtcNow.AddSeconds(-1));
    }
    
    [Fact]
    public void Player_JoinTeam_Success()
    {
        var player = Player.Create("Test", Guid.NewGuid());
        var teamId = Guid.NewGuid();
        
        var result = player.JoinTeam(teamId);
        
        Assert.True(result.Success);
        Assert.Contains(teamId, player.TeamIds);
    }
    
    [Fact]
    public void Player_JoinTeam_AlreadyOnTeam_Fails()
    {
        var player = Player.Create("Test", Guid.NewGuid());
        var teamId = Guid.NewGuid();
        player.JoinTeam(teamId);
        
        var result = player.JoinTeam(teamId); // Try again
        
        Assert.False(result.Success);
        Assert.Equal("Player is already on this team", result.Error);
    }
}
```

---

## Migration Checklist (Actual State)

Since Step 8 was never needed, here's what was actually done:

- [x] ~~Individual entity services~~ Never created
- [x] ~~Migration to CoreService partials~~ Not needed
- [x] **Entity + EntityCore pattern established**
- [x] **CoreService provides data access via generated accessors**
- [x] **Business logic in EntityCore files**
- [x] **All entities have corresponding EntityCore files**
- [x] **Testing strategy for entity logic**
- [x] **Documentation of patterns**

---

## Conclusion

**Step 8 as originally planned was obsolete before it began.**

The architecture evolved directly to the Entity + EntityCore pattern, which is superior to the originally planned CoreService partials approach.

**Current State:**
- ✅ Entity.cs: Data model (properties, attributes)
- ✅ EntityCore.cs: Business logic (factory methods, validation, operations)
- ✅ CoreService: Infrastructure and data access orchestration
- ✅ DatabaseService: CRUD operations (Repository, Cache, Archive)

**Why This is Better:**
- Co-located business logic with entities
- Clearer separation of concerns
- Better discoverability
- Simpler testing
- More maintainable

**Step 8: ❌ OBSOLETE (Not implemented; superseded by EntityCore pattern)**

---

## Appendix: If Step 8 Had Been Implemented

For historical reference, if Step 8 had been implemented as planned, it would have involved:

1. Creating individual services (PlayerService, TeamService, etc.)
2. Moving business logic into those services
3. Later migrating from services to CoreService partials
4. Updating all references throughout the codebase
5. Removing individual service classes
6. Testing the migration

**Estimated Effort:** 2-3 weeks of work  
**Actual Effort:** 0 (not needed due to better design)

**Lessons Learned:**
- Sometimes the best migration is no migration
- Architecture can evolve in better directions than initially planned
- Flexibility in execution is more valuable than rigid adherence to plans
