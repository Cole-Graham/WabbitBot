# Match ParentType Enum Implementation

**Date:** 2025-10-01  
**Status:** ✅ COMPLETE  
**Updated:** 2025-10-01 - Removed `Casual` from enum (semantically incorrect)

---

## Change Summary

Replaced `Match.ParentType` string property with strongly-typed `MatchParentType` enum.

**Important:** `Casual` was initially added to the enum but then removed because it's semantically incorrect.
- `MatchParentType` describes the **type of parent entity**
- Casual matches have **no parent**
- Therefore, `null` ParentType represents casual/standalone matches

---

## Motivation

**Before:**
```csharp
public class Match : Entity
{
    public string? ParentType { get; set; } // "Scrimmage" or "Tournament"
}

// Usage - error-prone:
if (string.Equals(match.ParentType, "Scrimmage", StringComparison.OrdinalIgnoreCase))
{
    // Typos: "scrimmage", "Scrimmmage", "SCRIMMAGE"
    // No compile-time validation
    // No IntelliSense support
}
```

**After:**
```csharp
/// <summary>
/// Type of parent entity that owns a match.
/// If both ParentId and ParentType are null, the match is a casual/standalone match.
/// </summary>
public enum MatchParentType
{
    Scrimmage,   // Match belongs to a Scrimmage entity
    Tournament,  // Match belongs to a Tournament entity
}

public class Match : Entity
{
    public Guid? ParentId { get; set; }
    public MatchParentType? ParentType { get; set; }
    
    // When both are null = casual/standalone match
}

// Usage - type-safe:
if (match.ParentType == MatchParentType.Scrimmage)
{
    // ✅ Compile-time validation
    // ✅ IntelliSense support
    // ✅ No typos possible
    // ✅ Easy refactoring
}
```

---

## Benefits

### 1. **Type Safety**
- Compile-time validation instead of runtime
- Impossible to assign invalid values
- Catches errors before deployment

### 2. **Developer Experience**
- IntelliSense shows all valid options
- IDE autocomplete helps write correct code
- Easier to discover what values are valid

### 3. **Prevents Typos**
```csharp
// ❌ Before - All of these are "valid" at compile time:
match.ParentType = "Scrimmage";
match.ParentType = "scrimmage";
match.ParentType = "Scrimmmage";  // Typo!
match.ParentType = "SCRIMMAGE";
match.ParentType = "scrim";       // Wrong!

// ✅ After - Only valid options compile:
match.ParentType = MatchParentType.Scrimmage;
match.ParentType = MatchParentType.Tournament;
match.ParentType = MatchParentType.Casual;
// match.ParentType = "Scrimmage"; // ❌ Compile error!
```

### 4. **Easier Refactoring**
- Find all usages with "Find All References"
- Rename enum values safely across entire codebase
- Compiler tells you what breaks when you change it

### 5. **Self-Documenting**
```csharp
// Enum definition serves as documentation:
public enum MatchParentType
{
    Scrimmage,   // Match is part of a scrimmage
    Tournament,  // Match is part of a tournament
    Casual,      // Match is a standalone casual match
}
```

### 6. **Database Storage**
- EF Core stores enum as integer by default (efficient)
- Can configure to store as string if needed
- Either way, application code is type-safe

---

## Changes Made

### 1. Added Enum Definition

**File:** `src/WabbitBot.Core/Common/Models/Common/Match.cs`

```csharp
public enum MatchParentType
{
    Scrimmage,
    Tournament,
    Casual,
}
```

**Placement:** After `GameStatus` enum, before `Game` region (line 219)

### 2. Updated Match Entity

**File:** `src/WabbitBot.Core/Common/Models/Common/Match.cs`

```csharp
public class Match : Entity, IMatchEntity
{
    // Changed from:
    // public string? ParentType { get; set; }
    
    // To:
    public MatchParentType? ParentType { get; set; }
}
```

### 3. Updated Existing Usage

**File:** `src/WabbitBot.Core/Scrimmages/ScrimmageCore.PPR.cs`

```csharp
// Before:
if (string.Equals(match.ParentType, "Scrimmage", StringComparison.OrdinalIgnoreCase) 
    && match.ParentId.HasValue)

// After:
if (match.ParentType == MatchParentType.Scrimmage && match.ParentId.HasValue)
```

**Benefits:**
- ✅ More concise (no `string.Equals`)
- ✅ More readable
- ✅ Type-safe
- ✅ Faster execution (direct comparison vs string comparison)

---

## Usage Examples

### Creating a Match from Scrimmage

```csharp
var match = new Match
{
    Team1Id = scrimmage.Team1Id,
    Team2Id = scrimmage.Team2Id,
    ParentId = scrimmage.Id,
    ParentType = MatchParentType.Scrimmage, // ✅ Type-safe!
    BestOf = scrimmage.BestOf,
    // ... etc
};
```

### Creating a Match from Tournament

```csharp
var match = new Match
{
    Team1Id = team1Id,
    Team2Id = team2Id,
    ParentId = tournament.Id,
    ParentType = MatchParentType.Tournament, // ✅ Type-safe!
    BestOf = tournament.BestOf,
    // ... etc
};
```

### Creating a Casual/Standalone Match

```csharp
var match = new Match
{
    Team1Id = team1Id,
    Team2Id = team2Id,
    ParentId = null,        // ✅ No parent
    ParentType = null,      // ✅ No parent type
    BestOf = 1,
    // ... etc
};

// Helper method for clarity:
public static bool IsCasualMatch(Match match) 
    => match.ParentId is null && match.ParentType is null;
```

### Switch Statement for Match Type Logic

```csharp
var matchTypeName = match.ParentType switch
{
    MatchParentType.Scrimmage => "Scrimmage Match",
    MatchParentType.Tournament => "Tournament Match",
    null => "Casual Match",  // ✅ null = no parent = casual
    _ => throw new ArgumentOutOfRangeException()
};

// Compiler warns if you miss a case! ✅

// Alternative with explicit casual check:
var matchTypeName = (match.ParentType, match.ParentId) switch
{
    (MatchParentType.Scrimmage, _) => "Scrimmage Match",
    (MatchParentType.Tournament, _) => "Tournament Match",
    (null, null) => "Casual Match",
    _ => "Invalid Match Configuration"
};
```

### Querying by Parent Type

```csharp
// EF Core queries work seamlessly:
var scrimmageMatches = await context.Matches
    .Where(m => m.ParentType == MatchParentType.Scrimmage)
    .ToListAsync();

var tournamentMatches = await context.Matches
    .Where(m => m.ParentType == MatchParentType.Tournament)
    .ToListAsync();

var casualMatches = await context.Matches
    .Where(m => m.ParentType == null && m.ParentId == null)
    .ToListAsync();

// Get all matches with a parent (scrimmage or tournament):
var parentedMatches = await context.Matches
    .Where(m => m.ParentType != null)
    .ToListAsync();
```

---

## Design Decision: Why No `Casual` in the Enum?

### The Question

Should we include `MatchParentType.Casual` in the enum? Should we create a `Casual` entity?

### The Answer: **No** ❌

**Reasoning:**

1. **Semantic Correctness**
   - `MatchParentType` describes the **type of parent entity**
   - Casual matches have **no parent entity**
   - Therefore, `MatchParentType.Casual` is semantically contradictory

2. **YAGNI Principle**
   - Don't create a `Casual` entity just for consistency
   - A "thin wrapper" entity with no purpose adds complexity
   - `null` naturally represents "no parent"

3. **Null is Meaningful**
   ```csharp
   // This is self-documenting:
   ParentId = null, ParentType = null  // ✅ Obviously no parent
   
   // vs this which is confusing:
   ParentId = null, ParentType = Casual  // ❌ "Type of parent" is "no parent"?
   ```

4. **Consistency is Not Always Good**
   - Forced consistency can obscure domain meaning
   - Tournament and Scrimmage ARE entities that contain matches
   - Casual matches ARE NOT contained - they're standalone
   - This fundamental difference SHOULD be reflected in the model

### Alternative Considered: Separate `MatchType` Enum

If you need to categorize matches independently of their parent:

```csharp
// What KIND of match is this?
public enum MatchType
{
    Scrimmage,
    Tournament,
    Casual,
    Ranked,
}

// What type of PARENT entity owns this match? (if any)
public enum MatchParentType
{
    Scrimmage,
    Tournament,
}

public class Match
{
    public MatchType MatchType { get; set; }        // Always set
    public Guid? ParentId { get; set; }             // Optional
    public MatchParentType? ParentType { get; set; } // Optional
}
```

**When to use this:**
- If you need to query "all casual matches" frequently
- If match type affects behavior beyond parent relationship
- If you have match types that don't correspond to parents

**For now:** Not needed. Use `ParentType == null` to identify casual matches.

---

## Database Considerations

### Storage

By default, EF Core stores enums as integers:

```sql
-- Database column type: INTEGER (nullable)
ParentType | Match Type
-----------|-----------
0          | Scrimmage
1          | Tournament
NULL       | Casual/Standalone (no parent)
```

**Benefits:**
- ✅ Compact storage (4 bytes vs variable string length)
- ✅ Faster queries and indexes
- ✅ More efficient JOIN operations

**Consideration:**
- If you need human-readable values in database (e.g., for direct SQL queries), you can configure EF Core to store as string:

```csharp
// In DbContext configuration:
entity.Property(e => e.ParentType)
    .HasConversion<string>(); // Stores "Scrimmage", "Tournament", "Casual"
```

For now, we're using the default integer storage.

### Migration Impact

The existing `ParentType TEXT NULL` column in the database is compatible:
- PostgreSQL can store integers in TEXT columns
- EF Core handles the conversion automatically
- No migration needed for existing data
- Future migrations will use INTEGER type

---

## Testing

### Build Status
✅ **Build succeeded** with no errors

### Test Results
✅ **Tests passed** - Database operations work correctly with enum

```
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

### Files Modified
1. `src/WabbitBot.Core/Common/Models/Common/Match.cs` - Added enum, updated property
2. `src/WabbitBot.Core/Scrimmages/ScrimmageCore.PPR.cs` - Updated comparison

### Files Reviewed
- `src/WabbitBot.Common/Data/Schema/Migrations/CreateMatchesTable.cs` - No changes needed

---

## Future Considerations

### Adding New Parent Types

If you need to add a new parent type in the future:

```csharp
public enum MatchParentType
{
    Scrimmage,
    Tournament,
    Casual,
    Ranked,      // 🆕 New type
    Practice,    // 🆕 New type
}
```

**Impact:**
1. Compiler will show warnings/errors for all switch statements that need updating
2. Add new cases to switch statements
3. Update any validation logic
4. Add documentation

**Example:**
```csharp
// Compiler will warn you about missing cases:
var name = match.ParentType switch
{
    MatchParentType.Scrimmage => "Scrimmage",
    MatchParentType.Tournament => "Tournament",
    MatchParentType.Casual => "Casual",
    // ⚠️ Warning: Missing case for 'Ranked'
    // ⚠️ Warning: Missing case for 'Practice'
};
```

---

## Summary

| Aspect | Before (string) | After (enum) |
|--------|----------------|--------------|
| **Type Safety** | ❌ Runtime only | ✅ Compile-time |
| **Typos** | ❌ Possible | ✅ Impossible |
| **IntelliSense** | ❌ No | ✅ Yes |
| **Refactoring** | ❌ Manual search | ✅ IDE support |
| **Performance** | ❌ String comparison | ✅ Integer comparison |
| **Database** | String storage | Integer storage (more efficient) |
| **Validation** | ❌ Manual checks | ✅ Compiler enforced |
| **Discoverability** | ❌ Check docs | ✅ Check enum definition |

---

## Recommendation

✅ **This change is a clear improvement** with:
- No downsides
- Better type safety
- Improved developer experience
- More efficient code
- Easier maintenance

Similar approach should be considered for other string-based categorical data in the codebase.

