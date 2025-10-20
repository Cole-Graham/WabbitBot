# Replay Parser Changelog

## 2025-10-17: Victory Code Extraction

### What We Discovered

By analyzing [Kraku's WARNO Replays Analyser](https://github.com/Kraku/warno-replays-analyser), we discovered that **Kraku's parser does NOT do binary parsing** for match results. Instead, the result data is **already in the JSON metadata** embedded in the replay file!

The `.rpl3` file contains:
1. Binary header/garbage data
2. **Game metadata JSON** - `{"game":...}` with settings and players
3. Binary event data
4. **Result JSON** - `{"Duration":"2241","Victory":"6"}` with the match outcome

### Changes Made

#### Python Parser (`replay_parser.py`)
- ✅ Now reads the entire file instead of just 2 lines
- ✅ Extracts result data using regex: `{"Duration":"...", "Victory":"..."}`
- ✅ Includes result in formatted output
- ✅ Prints result information during parsing

#### C# Parser (`ReplayCore.cs`)
- ✅ Reads entire file as UTF-8 string instead of line-by-line
- ✅ Searches for result JSON using regex
- ✅ Passes `victoryCode` and `durationSeconds` to extraction method
- ✅ Added `InterpretVictoryCode()` helper method with alliance-aware interpretation

#### Entity Model (`Replay.cs`)
- ✅ Added `VictoryCode` property (string, nullable)
- ✅ Added `DurationSeconds` property (int, nullable)
- ✅ Documented victory code ranges

### Testing

```bash
# Test Python parser
cd dev/tools/ReplayParser
python replay_parser.py
```

**Expected Output:**
```
Found result: Duration=2241s, Victory=6
ASCII-safe formatted data written to out\replay_2025-10-17_14-35-11.json
Unicode formatted data written to out\replay_2025-10-17_14-35-11.unicode.json
```

**Output includes:**
```json
{
  "game": { ... },
  "result": {
    "Duration": "2241",
    "Victory": "6"
  }
}
```

### Key Insights

1. **No Binary Parsing Needed** - Victory code is in plaintext JSON metadata
2. **Alliance Matters** - Victory codes are from Alliance 0's perspective
3. **Simple Extraction** - Just search for `{"Duration":"...", "Victory":"..."}` pattern
4. **Actual Duration** - Duration is the real match time, not the configured limit

### Next Steps

To use this in WabbitBot:

1. **Database Migration** - Add `VictoryCode` and `DurationSeconds` columns to `replays` table
2. **Update Handlers** - Use `ReplayCore.InterpretVictoryCode()` to determine winners
3. **Auto-scoring** - Automatically determine match results from replay uploads
4. **Validation** - Verify user-reported results against replay data

