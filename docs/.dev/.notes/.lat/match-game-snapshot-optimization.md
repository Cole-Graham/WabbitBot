# Match/Game State Snapshot Design Optimization

**Date:** 2025-10-01  
**Context:** Optimizing the state management design for Match, Game, and their snapshots

---

## Current Issues

### 1. Data Duplication
Both `MatchStateSnapshot` and `GameStateSnapshot` store:
- Lifecycle properties (StartedAt, CompletedAt, CancelledAt, ForfeitedAt)
- Status properties (WinnerId, CancelledByUserId, ForfeitedByUserId, etc.)

### 2. Relationship Issues
- `MatchStateSnapshot` has navigation collections to `Game` entities (should be removed)
- `GameStateSnapshot` duplicates data from `Game` entity (MatchId, MapId, TeamSize, PlayerIds, GameNumber)

### 3. Forfeit Logic Confusion
- Game has forfeit properties but forfeiting a game = forfeiting the match
- Unclear which entity should "own" the forfeit state

---

## Design Questions & Answers

### Q1: Where should deck codes be stored?

**Answer: Keep deck codes in `GameStateSnapshot` ✅**

**Reasoning:**
1. **Deck codes are per-game, not per-match** - Players submit new deck codes before each game
2. **State snapshots track state changes** - Deck submission is a game-level state change
3. **Dictionary approach is problematic:**
   ```csharp
   // If in MatchStateSnapshot - BAD:
   public Dictionary<int, GameDeckInfo> GameDeckCodes { get; set; }
   
   // Problems:
   // - Complicates archiving (dictionaries with complex objects)
   // - Breaks single responsibility (match tracking game-level state)
   // - Makes querying harder (can't directly query for game deck state)
   // - Violates normalization (duplicates data that belongs to Game)
   ```
4. **Current design is correct** - GameStateSnapshot properly captures game-specific state

**Recommendation:** ✅ Keep deck codes in `GameStateSnapshot`

---

### Q2: How should forfeit logic work?

**Answer: Distinguish between game-level and match-level forfeits**

**Current Behavior:**
- Forfeiting a game = forfeiting the match
- Only the active game can be forfeited
- Match immediately ends when any game is forfeited

**Design Options:**

#### Option A: Remove Game Forfeit Properties (Simpler) ❌
```csharp
public class GameStateSnapshot
{
    // Remove all forfeit properties
    // Forfeit only exists at Match level
}
```
**Problems:**
- Can't tell WHICH game was forfeited when looking at archives
- Loses historical context
- Can't reconstruct exact state progression

#### Option B: Keep Game Forfeit as Read-Only Mirror (Recommended) ✅
```csharp
public class GameStateSnapshot
{
    // Forfeit properties exist but are DERIVED from Match forfeit
    // Set automatically when Match is forfeited during this game
    public DateTime? ForfeitedAt { get; set; }
    public Guid? ForfeitedByUserId { get; set; }
    public Guid? ForfeitedTeamId { get; set; }
    public string? ForfeitReason { get; set; }
}
```
**Benefits:**
- Historical record shows which game was active during forfeit
- Can reconstruct exact timeline
- Archive records are complete
- Query: "Show me all games that were forfeited" works

**Implementation:**
```csharp
// In Match forfeit logic:
public void ForfeitMatch(Guid userId, Guid teamId, string reason)
{
    // 1. Create Match forfeit snapshot
    var matchSnapshot = new MatchStateSnapshot
    {
        ForfeitedAt = DateTime.UtcNow,
        ForfeitedByUserId = userId,
        ForfeitedTeamId = teamId,
        ForfeitReason = reason,
        CurrentGameNumber = this.CurrentGameNumber
    };
    
    // 2. Create Game forfeit snapshot for the active game
    var activeGame = Games.FirstOrDefault(g => g.GameNumber == CurrentGameNumber);
    if (activeGame != null)
    {
        var gameSnapshot = new GameStateSnapshot
        {
            GameId = activeGame.Id,
            ForfeitedAt = DateTime.UtcNow,  // Same time
            ForfeitedByUserId = userId,      // Same user
            ForfeitedTeamId = teamId,        // Same team
            ForfeitReason = reason           // Same reason
        };
        activeGame.StateHistory.Add(gameSnapshot);
    }
    
    this.StateHistory.Add(matchSnapshot);
}
```

**Recommendation:** ✅ Keep game forfeit properties as a mirror of match forfeit

---

## Optimized Design

### Principle: Clear Ownership & Responsibility

1. **Match owns match-level state**
   - Overall match lifecycle (started, completed, cancelled, forfeited)
   - Map ban process (match-level, happens before games start)
   - Winner determination
   - Score tracking

2. **Game owns game-level state**
   - Individual game lifecycle
   - Deck submission (per-game, not per-match)
   - Game-specific winner
   - When forfeited: mirrors match forfeit

3. **Snapshots are immutable historical records**
   - NO navigation properties to mutable entities
   - Capture state at a point in time
   - Include denormalized data for historical completeness

---

## Recommended Changes

### 1. Remove Invalid Navigation Properties from MatchStateSnapshot

**REMOVE these lines (156, 161):**
```csharp
// ❌ REMOVE - Snapshots should NOT navigate to mutable entities
public ICollection<Game> Games { get; set; } = new List<Game>();
public ICollection<Game> FinalGames { get; set; } = new List<Game>();
```

**Why:** 
- Snapshots are immutable records, Games are mutable entities
- Creates EF Core relationship conflicts
- Violates separation of concerns

### 2. Clarify Game Forfeit Properties

**ADD documentation:**
```csharp
/// <summary>
/// Game forfeit properties. These are set automatically when a Match is forfeited
/// during this game. Forfeiting a game always forfeits the entire match.
/// These properties create a historical record of which game was active during forfeit.
/// </summary>
public DateTime? ForfeitedAt { get; set; }
public Guid? ForfeitedByUserId { get; set; }
public Guid? ForfeitedTeamId { get; set; }
public string? ForfeitReason { get; set; }
```

### 3. Remove Redundant Data from GameStateSnapshot

**Current duplication (lines 300-305):**
```csharp
// These are duplicated from Game entity - do we need them?
public Guid MatchId { get; set; }
public Guid MapId { get; set; }
public TeamSize TeamSize { get; set; }
public List<Guid> Team1PlayerIds { get; set; } = new();
public List<Guid> Team2PlayerIds { get; set; } = new();
public int GameNumber { get; set; } = 1;
```

**Question:** Are these needed for historical completeness or query performance?

**Options:**

#### A. Remove entirely (Normalize) ❌
```csharp
// Just reference the Game
public Guid GameId { get; set; }
public Game Game { get; set; }
// To get MatchId: Game.MatchId
// To get MapId: Game.MapId
```
**Problems:**
- Archive queries require joins
- Can't reconstruct if Game is deleted
- Lose historical context

#### B. Keep for denormalized historical record ✅
```csharp
// Keep denormalized for archive/historical completeness
public Guid MatchId { get; set; }      // Denormalized for queries
public Guid MapId { get; set; }        // Denormalized for queries  
public TeamSize TeamSize { get; set; } // Denormalized for queries
public List<Guid> Team1PlayerIds { get; set; } // Historical record
public List<Guid> Team2PlayerIds { get; set; } // Historical record
public int GameNumber { get; set; }    // Historical record
```
**Benefits:**
- Can query archives without joins
- Complete historical record even if Game is deleted
- Performance: Direct queries without navigation

**Recommendation:** ✅ Keep denormalized data for historical completeness

### 4. Clarify Snapshot Purpose with Documentation

**Add to class documentation:**

```csharp
/// <summary>
/// Immutable snapshot of game state at a point in time.
/// 
/// Design Principles:
/// 1. IMMUTABLE - Never modified after creation
/// 2. COMPLETE - Contains all data needed to reconstruct state without joins
/// 3. DENORMALIZED - Duplicates data for historical completeness and query performance
/// 4. NO NAVIGATION TO MUTABLE ENTITIES - Only reference by ID
/// 
/// Deck Submission:
/// - Deck codes are game-specific and submitted before each game
/// - Each game may have different deck codes
/// - Stored here (not in MatchStateSnapshot) because this is game-level state
/// 
/// Forfeit Handling:
/// - If match is forfeited during this game, forfeit properties are set
/// - Forfeiting a game always forfeits the entire match
/// - Game forfeit properties mirror the match forfeit for historical record
/// </summary>
public class GameStateSnapshot : Entity, IMatchEntity
```

---

## State Transition Examples

### Example 1: Best of 3 Match with Deck Submissions

```
Match Created
└─ MatchStateSnapshot[1]: Created, CurrentGameNumber=0

Map Bans Submitted
└─ MatchStateSnapshot[2]: Team1BansSubmitted=true
└─ MatchStateSnapshot[3]: Team2BansSubmitted=true, FinalMapPool calculated

Game 1 Created
├─ Game[1] created with Map from FinalMapPool
└─ GameStateSnapshot[1]: Created, GameNumber=1

Team 1 Submits Deck for Game 1
└─ GameStateSnapshot[2]: Team1DeckCode set, Team1DeckSubmittedAt

Team 2 Submits Deck for Game 1
└─ GameStateSnapshot[3]: Team2DeckCode set, Team2DeckSubmittedAt

Game 1 Starts
└─ GameStateSnapshot[4]: StartedAt set

Game 1 Completes (Team 1 wins)
├─ GameStateSnapshot[5]: CompletedAt set, WinnerId = Team1Id
└─ MatchStateSnapshot[4]: CurrentGameNumber=1, Score updated

Game 2 Created
├─ Game[2] created
└─ GameStateSnapshot[6]: Created, GameNumber=2

Team 1 Submits NEW Deck for Game 2 (different from Game 1!)
└─ GameStateSnapshot[7]: Team1DeckCode set (different than Game 1)

Team 2 Submits NEW Deck for Game 2
└─ GameStateSnapshot[8]: Team2DeckCode set

Game 2 Starts
└─ GameStateSnapshot[9]: StartedAt set

Game 2 Completes (Team 1 wins again)
├─ GameStateSnapshot[10]: CompletedAt set, WinnerId = Team1Id
└─ MatchStateSnapshot[5]: Match completed, WinnerId = Team1Id, FinalScore = "2-0"
```

**Key Points:**
- Deck codes are per-game (note different codes for Game 1 vs Game 2)
- Each game has its own state progression
- Match tracks overall progression

### Example 2: Forfeit During Game 2

```
Match Created → Game 1 Completes (Team 1 wins) → Game 2 Created and Started

Team 2 Forfeits During Game 2
├─ GameStateSnapshot[11]: ForfeitedAt, ForfeitedByUserId, ForfeitedTeamId, GameNumber=2
└─ MatchStateSnapshot[6]: ForfeitedAt, ForfeitedByUserId, ForfeitedTeamId, WinnerId=Team1Id, CurrentGameNumber=2

Result:
- Game 2 is marked as forfeited (historical record)
- Match is marked as forfeited
- Both timestamps are the same
- Historical record shows forfeit happened during Game 2
```

---

## Implementation Checklist

- [ ] Remove `MatchStateSnapshot.Games` navigation property
- [ ] Remove `MatchStateSnapshot.FinalGames` navigation property  
- [ ] Add documentation to `GameStateSnapshot` forfeit properties explaining they mirror match forfeits
- [ ] Add comprehensive class-level documentation to both snapshot classes
- [ ] Update forfeit logic to create both Match and Game snapshots atomically
- [ ] Add validation: Game forfeit properties can ONLY be set via Match forfeit
- [ ] Consider: Should `GameStateSnapshot.ForfeitedAt` have a check constraint that it matches a `MatchStateSnapshot.ForfeitedAt`?

---

## Summary

### Deck Codes
✅ **Keep in GameStateSnapshot** - They're per-game state, not per-match

### Forfeit Properties
✅ **Keep in GameStateSnapshot** - But document that they:
1. Mirror match forfeit values
2. Create historical record of which game was active
3. Cannot be set independently (always via match forfeit)

### Denormalized Data
✅ **Keep in GameStateSnapshot** - For:
1. Historical completeness
2. Archive query performance
3. Reconstruction without joins

### Navigation Properties
❌ **Remove from MatchStateSnapshot** - Snapshots should not navigate to mutable entities

---

## Alternative: Minimal Snapshot Design (Not Recommended)

If we wanted to go minimalist, we could reduce snapshots to only capture CHANGES:

```csharp
public class GameStateSnapshot
{
    public Guid GameId { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid PlayerId { get; set; }
    
    // ONLY the thing that changed
    public string? EventType { get; set; } // "DeckSubmitted", "GameStarted", etc.
    public Dictionary<string, object> EventData { get; set; } // The change
}
```

**Why we DON'T recommend this:**
- Harder to query ("show me all games where Team 1 forfeited")
- Requires event replay to reconstruct state
- More complex application logic
- Less intuitive for business users

**Current design is better:** Full state snapshots are easier to work with and query.

