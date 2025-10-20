# WARNO Replay Victory Codes

## Discovery

The WARNO replay file (`.rpl3`) contains **TWO separate JSON objects**:

1. **Game Metadata** - Starting with `{"game":...}` containing map, settings, players, decks
2. **Result Data** - A separate object `{"Duration":"...", "Victory":"..."}` containing match outcome

## Victory Code Format

```json
{
  "Duration": "2241",  // Match duration in seconds
  "Victory": "6"       // Victory code: "0"-"6"
}
```

## Victory Code Interpretation

The victory code is **from the perspective of Alliance 0**:

| Code | Alliance 0 Result | Alliance 1 Result | Specific Type |
|------|-------------------|-------------------|---------------|
| "0"  | Defeat            | Victory           | Unknown       |
| "1"  | Defeat            | Victory           | Unknown       |
| "2"  | Defeat            | Victory           | Unknown       |
| "3"  | Draw              | Draw              | Draw          |
| "4"  | Victory           | Defeat            | Unknown       |
| "5"  | Victory           | Defeat            | Unknown       |
| "6"  | Victory           | Defeat            | Unknown       |
| Other| Draw              | Draw              | Unknown       |

**Note:** Kraku's parser does NOT differentiate between the specific types of victory/defeat (surrender, conquest, timeout). It only distinguishes Victory (4-6), Defeat (0-2), and Draw (other). The actual meaning of each code is unknown without reverse-engineering or official Eugen Systems documentation.

### Hypothesis for Future Testing

To determine what each code means, we could:
1. Collect replays where we KNOW the victory condition (surrender, conquest, timeout)
2. Check the victory codes
3. Build a mapping

For example, if you have replays where:
- You surrendered early → Check if it's always code "0", "1", or "2"
- Enemy surrendered → Check if it's always code "4", "5", or "6"
- You won by conquest (capturing all points) → Check the code
- Game reached time limit → Check the code

**Current Data Point:**
- Victory code "6" + game ended at 37min (before 40min limit) = Possibly conquest win?

## Example Usage

### Python
```python
# From parsed replay
victory_code = data['result']['Victory']
duration = int(data['result']['Duration'])
player_alliance = data['game']['Players'][0]['PlayerAlliance']

# Interpret result
if victory_code in ['4', '5', '6']:
    result = 'Victory' if player_alliance == '0' else 'Defeat'
elif victory_code in ['0', '1', '2']:
    result = 'Defeat' if player_alliance == '0' else 'Victory'
else:
    result = 'Draw'

print(f"Player result: {result} after {duration} seconds")
```

### C#
```csharp
// Using the helper method in ReplayCore
var replay = ReplayCore.Parser.ParseReplayFile(fileData, gameId, matchId);
var player = replay.Value.Players.First();

var result = ReplayCore.InterpretVictoryCode(
    replay.Value.VictoryCode,
    player.PlayerAlliance
);

Console.WriteLine($"Player result: {result}");
Console.WriteLine($"Duration: {replay.Value.DurationSeconds} seconds");
```

## Implementation Notes

- The result data is found **after** the game metadata in the replay file
- It may be after the "star" marker used to truncate the game metadata JSON
- The victory code applies to **Alliance 0** - must be flipped for Alliance 1 players
- Duration is the **actual match duration**, not the configured time limit

## Credit

Victory code interpretation discovered by analyzing [Kraku's WARNO Replays Analyser](https://github.com/Kraku/warno-replays-analyser/blob/main/frontend/src/parsers/replaysParser.ts#L121-L125).

