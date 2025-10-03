# Match and Game State Flow Diagrams

**Date:** 2025-10-01  
**Purpose:** Visual reference for how state transitions work in matches and games

---

## Design Summary

### Key Principles ✅

1. **Deck codes are per-game** - Stored in `GameStateSnapshot` because players submit new codes for each game
2. **Forfeit properties exist in both entities** - Game forfeit mirrors match forfeit for historical record
3. **Snapshots are denormalized** - Include parent data for archive query performance
4. **Snapshots don't navigate to mutable entities** - Only reference by ID (except explicit navigation properties)

---

## Best of 3 Match Flow

```mermaid
sequenceDiagram
    participant User
    participant Match
    participant MatchSnapshot
    participant Game1
    participant Game1Snapshot
    participant Game2
    participant Game2Snapshot
    participant Game3
    participant Game3Snapshot

    Note over Match: MATCH CREATED
    User->>Match: Create Match (BestOf=3)
    Match->>MatchSnapshot: Create snapshot (Created)
    
    Note over Match: MAP BAN PHASE
    User->>Match: Submit Team1 Map Bans
    Match->>MatchSnapshot: Create snapshot (Team1BansSubmitted)
    User->>Match: Submit Team2 Map Bans
    Match->>MatchSnapshot: Create snapshot (Team2BansSubmitted, FinalMapPool calculated)
    
    Note over Game1: GAME 1 STARTS
    Match->>Game1: Create Game 1 (Map from FinalMapPool)
    Game1->>Game1Snapshot: Create snapshot (Created, GameNumber=1)
    
    User->>Game1: Team1 Submits Deck Code
    Game1->>Game1Snapshot: Create snapshot (Team1DeckCode, Team1DeckSubmittedAt)
    
    User->>Game1: Team2 Submits Deck Code
    Game1->>Game1Snapshot: Create snapshot (Team2DeckCode, Team2DeckSubmittedAt)
    
    User->>Game1: Start Game 1
    Game1->>Game1Snapshot: Create snapshot (StartedAt)
    
    User->>Game1: Complete Game 1 (Team1 wins)
    Game1->>Game1Snapshot: Create snapshot (CompletedAt, WinnerId=Team1)
    Match->>MatchSnapshot: Update snapshot (CurrentGameNumber=1, Score updated)
    
    Note over Game2: GAME 2 STARTS
    Match->>Game2: Create Game 2 (different Map)
    Game2->>Game2Snapshot: Create snapshot (Created, GameNumber=2)
    
    User->>Game2: Team1 Submits NEW Deck Code (different from Game 1!)
    Game2->>Game2Snapshot: Create snapshot (Team1DeckCode [DIFFERENT])
    
    User->>Game2: Team2 Submits NEW Deck Code
    Game2->>Game2Snapshot: Create snapshot (Team2DeckCode)
    
    User->>Game2: Start Game 2
    Game2->>Game2Snapshot: Create snapshot (StartedAt)
    
    User->>Game2: Complete Game 2 (Team1 wins again)
    Game2->>Game2Snapshot: Create snapshot (CompletedAt, WinnerId=Team1)
    Match->>MatchSnapshot: Update snapshot (CompletedAt, WinnerId=Team1, FinalScore="2-0")
    
    Note over Match: MATCH COMPLETE - Team1 wins 2-0 (Game 3 not needed)
```

---

## Forfeit During Best of 3 Match

```mermaid
sequenceDiagram
    participant User
    participant Match
    participant MatchSnapshot
    participant Game1
    participant Game1Snapshot
    participant Game2
    participant Game2Snapshot

    Note over Match: Match Created, Game 1 Complete (Team1 wins)
    
    Note over Game2: GAME 2 STARTS
    Match->>Game2: Create Game 2
    Game2->>Game2Snapshot: Create snapshot (Created, GameNumber=2)
    
    User->>Game2: Team1 Submits Deck Code
    Game2->>Game2Snapshot: Create snapshot (Team1DeckCode)
    
    User->>Game2: Team2 Submits Deck Code
    Game2->>Game2Snapshot: Create snapshot (Team2DeckCode)
    
    User->>Game2: Start Game 2
    Game2->>Game2Snapshot: Create snapshot (StartedAt)
    
    Note over Match,Game2: TEAM 2 FORFEITS DURING GAME 2
    User->>Match: Forfeit Match (by Team2, during Game 2)
    
    rect rgb(255, 200, 200)
        Note right of Match: ATOMIC FORFEIT OPERATION
        Match->>MatchSnapshot: Create snapshot<br/>(ForfeitedAt, ForfeitedByUserId,<br/>ForfeitedTeamId, CurrentGameNumber=2,<br/>WinnerId=Team1, FinalScore="1-0 (forfeit)")
        
        Match->>Game2Snapshot: Create snapshot<br/>(ForfeitedAt [SAME TIME],<br/>ForfeitedByUserId [SAME USER],<br/>ForfeitedTeamId [SAME TEAM],<br/>ForfeitReason [SAME REASON])
    end
    
    Note over Match,Game2: Result:<br/>- Match is forfeited (Team1 wins)<br/>- Game 2 is marked as forfeited<br/>- Historical record shows forfeit during Game 2<br/>- All timestamps match
```

---

## Deck Code Submission Per-Game

```mermaid
graph TB
    subgraph "Match State"
        Match[Match Entity]
        MS1[MatchSnapshot: Created]
        MS2[MatchSnapshot: Map Bans Complete]
        MS3[MatchSnapshot: Game 1 Complete]
        MS4[MatchSnapshot: Game 2 Complete]
    end
    
    subgraph "Game 1 State"
        G1[Game 1 Entity<br/>Map: Frost Valley]
        G1S1[GameSnapshot: Created]
        G1S2["GameSnapshot: Team1DeckCode<br/>✅ 'AAECAa0GBp...XYZ'"]
        G1S3["GameSnapshot: Team2DeckCode<br/>✅ 'AAECAf0GBp...ABC'"]
        G1S4[GameSnapshot: Game Started]
        G1S5[GameSnapshot: Game Completed<br/>Winner: Team1]
    end
    
    subgraph "Game 2 State"
        G2[Game 2 Entity<br/>Map: Desert Plains]
        G2S1[GameSnapshot: Created]
        G2S2["GameSnapshot: Team1DeckCode<br/>✅ 'BBFCAa0GBp...QRS' ⚠️ DIFFERENT!"]
        G2S3["GameSnapshot: Team2DeckCode<br/>✅ 'BBFCAf0GBp...DEF' ⚠️ DIFFERENT!"]
        G2S4[GameSnapshot: Game Started]
        G2S5[GameSnapshot: Game Completed<br/>Winner: Team1]
    end
    
    Match --> MS1 --> MS2 --> MS3 --> MS4
    Match --> G1
    Match --> G2
    
    G1 --> G1S1 --> G1S2 --> G1S3 --> G1S4 --> G1S5
    G2 --> G2S1 --> G2S2 --> G2S3 --> G2S4 --> G2S5
    
    style G1S2 fill:#90EE90
    style G1S3 fill:#90EE90
    style G2S2 fill:#FFD700
    style G2S3 fill:#FFD700
    
    Note1[Each game has INDEPENDENT deck codes]
    Note2[Players can change strategy between games]
    Note3[Storing in GameSnapshot prevents dictionary complexity]
```

---

## Why This Design Works

### ✅ Deck Codes in GameStateSnapshot

**Scenario:** Best of 3 match
```
Game 1: Player uses Aggro deck (AAECAa0GBp...XYZ)
  ↓ Team 1 wins
Game 2: Player switches to Control deck (BBFCAa0GBp...QRS) to counter opponent
  ↓ Team 1 wins
Game 3: Not played (match over)
```

**If deck codes were in MatchStateSnapshot:**
```csharp
// Would need this structure:
public class MatchStateSnapshot 
{
    public Dictionary<int, GameDeckCodes> DeckCodesByGame { get; set; }
}

public class GameDeckCodes 
{
    public string? Team1DeckCode { get; set; }
    public string? Team2DeckCode { get; set; }
    public DateTime? Team1SubmittedAt { get; set; }
    public DateTime? Team2SubmittedAt { get; set; }
    // ... 8 total properties per game
}

// Problems:
// - Complex nested structure
// - Hard to archive (dict of objects)
// - Can't easily query "show me all games where Team1 submitted deck X"
// - Match snapshot caring about game-level details (wrong responsibility)
```

**Current design (GameStateSnapshot):**
```csharp
// Simple, flat structure per game
public class GameStateSnapshot 
{
    public string? Team1DeckCode { get; set; }
    public string? Team2DeckCode { get; set; }
    // ... clear and simple
}

// Benefits:
// ✅ Easy to query: "SELECT * FROM game_state_snapshots WHERE team1_deck_code = 'XXX'"
// ✅ Easy to archive: Just copy rows to archive table
// ✅ Clear responsibility: Game state in game snapshots
// ✅ No dictionary complexity
```

---

### ✅ Forfeit Properties in Both Entities

**Why keep forfeit properties in GameStateSnapshot?**

**Scenario:** Team forfeits during Game 2 of a Best of 5

**Without game forfeit properties:**
```sql
-- Query: "Which game was active when match X was forfeited?"
SELECT current_game_number FROM match_state_snapshots 
WHERE match_id = 'X' AND forfeited_at IS NOT NULL;
-- Returns: 2

-- But then to get game details:
SELECT * FROM games WHERE match_id = 'X' AND game_number = 2;
-- Problem: Game entity might be modified after forfeit
-- Lost historical context of exact state during forfeit
```

**With game forfeit properties:**
```sql
-- Query: "Which game was forfeited in match X?"
SELECT * FROM game_state_snapshots 
WHERE match_id = 'X' AND forfeited_at IS NOT NULL;
-- Returns: Complete snapshot of Game 2 at moment of forfeit

-- Archive query: "Show me all forfeited games in Q4 2024"
SELECT g.game_id, g.match_id, g.forfeited_team_id, g.forfeit_reason
FROM game_state_snapshots_archive g
WHERE g.forfeited_at BETWEEN '2024-10-01' AND '2024-12-31';
-- Works perfectly without joins
```

**Benefits:**
- ✅ Historical completeness - Know exact state when forfeit happened
- ✅ Query performance - No joins needed
- ✅ Archive independence - Archive queries work standalone
- ✅ Clear timeline - Can see progression within forfeited game

---

## State Snapshot Responsibilities

```mermaid
graph LR
    subgraph "Match Responsibilities"
        MR1[Overall match lifecycle]
        MR2[Map ban process]
        MR3[Match winner determination]
        MR4[Final score tracking]
        MR5[Match-level forfeit/cancel]
    end
    
    subgraph "Game Responsibilities"
        GR1[Individual game lifecycle]
        GR2[Deck submission tracking]
        GR3[Game winner determination]
        GR4[Game-level state changes]
        GR5[Mirror of match forfeit<br/>when forfeited during this game]
    end
    
    Match[Match Entity] --> MR1
    Match --> MR2
    Match --> MR3
    Match --> MR4
    Match --> MR5
    
    Game[Game Entity] --> GR1
    Game --> GR2
    Game --> GR3
    Game --> GR4
    Game --> GR5
    
    MR5 -.->|triggers| GR5
    
    style MR5 fill:#FFB6C1
    style GR5 fill:#FFB6C1
    style GR2 fill:#90EE90
```

---

## Archive Query Examples

### Query 1: Find all games where specific deck was used
```sql
-- Easy because deck codes are in game_state_snapshots
SELECT 
    gs.game_id,
    gs.match_id,
    gs.game_number,
    gs.team1_deck_code,
    gs.completed_at
FROM game_state_snapshots gs
WHERE gs.team1_deck_code = 'AAECAa0GBp...XYZ'
  AND gs.completed_at IS NOT NULL
ORDER BY gs.completed_at DESC;
```

### Query 2: Find all matches forfeited during Game 2 or later
```sql
-- Easy because forfeit data is in both snapshots
SELECT 
    ms.match_id,
    ms.current_game_number,
    gs.game_id,
    ms.forfeited_team_id,
    ms.forfeit_reason
FROM match_state_snapshots ms
JOIN game_state_snapshots gs 
  ON gs.match_id = ms.match_id 
  AND gs.forfeited_at = ms.forfeited_at  -- Same timestamp
WHERE ms.forfeited_at IS NOT NULL
  AND ms.current_game_number >= 2;
```

### Query 3: Deck submission statistics
```sql
-- Find average time between deck submissions
SELECT 
    game_id,
    AVG(EXTRACT(EPOCH FROM (team2_deck_submitted_at - team1_deck_submitted_at))) as avg_seconds_between
FROM game_state_snapshots
WHERE team1_deck_submitted_at IS NOT NULL
  AND team2_deck_submitted_at IS NOT NULL
  AND team2_deck_submitted_at > team1_deck_submitted_at
GROUP BY game_id;
```

---

## Implementation Notes

### Forfeit Logic Pattern
```csharp
public static class MatchCore 
{
    public static void ForfeitMatch(
        Match match, 
        Guid userId, 
        Guid teamId, 
        string reason)
    {
        var timestamp = DateTime.UtcNow;
        var currentGameNumber = match.CurrentGameNumber;
        
        // 1. Create match forfeit snapshot
        var matchSnapshot = new MatchStateSnapshot
        {
            MatchId = match.Id,
            Timestamp = timestamp,
            UserId = userId.ToString(),
            ForfeitedAt = timestamp,
            ForfeitedByUserId = userId,
            ForfeitedTeamId = teamId,
            ForfeitReason = reason,
            CurrentGameNumber = currentGameNumber,
            WinnerId = teamId == match.Team1Id ? match.Team2Id : match.Team1Id,
            FinalScore = $"{/* calculate score */} (forfeit)"
        };
        match.StateHistory.Add(matchSnapshot);
        
        // 2. Create game forfeit snapshot for active game (if exists)
        var activeGame = match.Games
            .FirstOrDefault(g => g.GameNumber == currentGameNumber);
            
        if (activeGame != null)
        {
            var gameSnapshot = new GameStateSnapshot
            {
                GameId = activeGame.Id,
                Timestamp = timestamp,        // SAME timestamp
                PlayerId = userId,
                ForfeitedAt = timestamp,      // SAME timestamp
                ForfeitedByUserId = userId,   // SAME user
                ForfeitedTeamId = teamId,     // SAME team
                ForfeitReason = reason,       // SAME reason
                
                // Denormalized data for historical completeness
                MatchId = match.Id,
                MapId = activeGame.MapId,
                TeamSize = activeGame.TeamSize,
                GameNumber = activeGame.GameNumber,
                // ... etc
            };
            activeGame.StateHistory.Add(gameSnapshot);
        }
    }
}
```

---

## Summary

| Question | Answer | Rationale |
|----------|--------|-----------|
| Where should deck codes be stored? | ✅ GameStateSnapshot | Per-game state, avoids dictionary complexity, easier to archive and query |
| Should game have forfeit properties? | ✅ Yes, as mirror of match forfeit | Historical completeness, query performance, archive independence |
| Should we keep denormalized data in GameStateSnapshot? | ✅ Yes (MatchId, MapId, etc.) | Historical completeness, query performance, works without joins |
| Should MatchStateSnapshot navigate to Games? | ❌ No, already removed | Snapshots are immutable, shouldn't navigate to mutable entities |
| Can game forfeit be set independently? | ❌ No, always via match forfeit | Forfeiting game = forfeiting match, properties mirror for historical record |

---

## Design Validation ✅

The current design after optimization:

1. **Clear Responsibility Separation** ✅
   - Match owns match-level state
   - Game owns game-level state
   - No crossing of boundaries

2. **Historical Completeness** ✅
   - Snapshots contain all data needed for reconstruction
   - Archive queries work without joins
   - Timeline is clear and complete

3. **Query Performance** ✅
   - Denormalized data enables direct queries
   - No complex dictionary structures
   - Simple, flat table structure

4. **Maintenance** ✅
   - Clear documentation of design decisions
   - Inline comments explain rationale
   - Implementation patterns documented

5. **Flexibility** ✅
   - Supports any BestOf value (1, 3, 5, 7, etc.)
   - Deck codes can change per game
   - Historical queries remain fast

