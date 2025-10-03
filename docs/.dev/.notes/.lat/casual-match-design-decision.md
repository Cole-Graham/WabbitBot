# Casual Match Design Decision

**Date:** 2025-10-01  
**Status:** ✅ DECIDED  
**Decision:** Do NOT create a `Casual` entity. Do NOT include `Casual` in `MatchParentType` enum.

---

## The Question

> Unlike Tournament or Scrimmage, there is no "Casual" entity to serve as a parent for casual matches.
> Should we create a thin wrapper `Casual` entity for consistency?

---

## The Decision

**NO** - Do not create a `Casual` entity. Do not include `Casual` in the enum.

**Instead:** Use `null` for both `ParentId` and `ParentType` to represent casual/standalone matches.

---

## Reasoning

### 1. Semantic Correctness ✅

**`MatchParentType` means "type of parent entity"**

```csharp
public enum MatchParentType
{
    Scrimmage,   // ✅ Parent entity: Scrimmage
    Tournament,  // ✅ Parent entity: Tournament
    Casual,      // ❌ Parent entity: ??? (doesn't exist!)
}
```

- Casual matches have **no parent**
- Including `Casual` in `MatchParentType` is semantically contradictory
- It's a "type of parent" but there is no parent

### 2. YAGNI Principle ✅

**Don't create what you don't need**

Creating a `Casual` entity would require:
```csharp
[EntityMetadata(...)]
public class Casual : Entity, IMatchEntity
{
    // What goes here? Nothing!
    // This entity has no purpose except to exist
}
```

**Problems:**
- ❌ Empty or near-empty entity
- ❌ Database table with minimal/no columns
- ❌ Maintenance overhead
- ❌ Confusion for developers ("Why does this exist?")
- ❌ Forced consistency that obscures domain meaning

**Benefit:**
- ??? Consistency? (Not a real benefit if it adds complexity)

### 3. Null is the Perfect Representation ✅

**`null` naturally means "no parent"**

```csharp
// Scrimmage match
var match = new Match
{
    ParentId = scrimmageId,                    // ✅ Has a parent
    ParentType = MatchParentType.Scrimmage,    // ✅ Type: Scrimmage
};

// Casual match
var match = new Match
{
    ParentId = null,       // ✅ No parent - self-documenting!
    ParentType = null,     // ✅ No parent type - makes sense!
};
```

**This is more clear than:**
```csharp
// Casual match with enum value
var match = new Match
{
    ParentId = null,                           // No parent
    ParentType = MatchParentType.Casual,       // ❌ "Type of parent" is... no parent?
};
```

### 4. Consistency Can Be Harmful ✅

**Not everything should be consistent for the sake of consistency**

- **Tournament**: IS a container entity that owns multiple matches
- **Scrimmage**: IS a container entity that owns a match
- **Casual**: IS NOT a container - it's the absence of a container

**Domain Truth:**
- Tournaments and Scrimmages are fundamentally SIMILAR (both contain matches)
- Casual matches are fundamentally DIFFERENT (standalone, no container)

**Forcing consistency** by creating a `Casual` entity obscures this important domain distinction.

### 5. Future Flexibility ✅

If you later need to categorize matches beyond parent type:

```csharp
// Separate concerns: parent vs category
public enum MatchType          // What KIND of match
{
    Scrimmage,
    Tournament,
    Casual,
    Ranked,
    Practice,
}

public enum MatchParentType    // What type of PARENT (if any)
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

**Current approach doesn't prevent this.** You can add `MatchType` later if needed.

---

## Implementation

### Final Design

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
```

### Usage Patterns

```csharp
// Creating matches
var scrimmageMatch = new Match 
{ 
    ParentId = scrimmageId, 
    ParentType = MatchParentType.Scrimmage 
};

var tournamentMatch = new Match 
{ 
    ParentId = tournamentId, 
    ParentType = MatchParentType.Tournament 
};

var casualMatch = new Match 
{ 
    ParentId = null, 
    ParentType = null  // ✅ Simple and clear
};

// Identifying match types
bool isCasual = match.ParentId is null && match.ParentType is null;
bool isScrimm = match.ParentType == MatchParentType.Scrimmage;
bool isTourney = match.ParentType == MatchParentType.Tournament;

// Helper method
public static bool IsCasualMatch(Match match)
    => match.ParentId is null && match.ParentType is null;

// Switch expression
var matchCategory = match.ParentType switch
{
    MatchParentType.Scrimmage => "Scrimmage",
    MatchParentType.Tournament => "Tournament",
    null => "Casual",  // ✅ null case is valid
    _ => throw new InvalidOperationException()
};

// Querying
var casualMatches = await context.Matches
    .Where(m => m.ParentType == null && m.ParentId == null)
    .ToListAsync();

var parentedMatches = await context.Matches
    .Where(m => m.ParentType != null)
    .ToListAsync();
```

---

## Alternatives Considered

### Option A: Include `Casual` in enum ❌

```csharp
public enum MatchParentType
{
    Scrimmage,
    Tournament,
    Casual,
}
```

**Rejected because:**
- ❌ Semantically incorrect (`Casual` is not a "type of parent")
- ❌ Requires validation (ParentId must be null for Casual)
- ❌ Confusing for developers

### Option B: Create empty `Casual` entity ❌

```csharp
public class Casual : Entity
{
    // Nothing here...
}
```

**Rejected because:**
- ❌ No purpose except to exist
- ❌ Violates YAGNI
- ❌ Adds complexity with no benefit
- ❌ Forces consistency where it doesn't make sense

### Option C: Use null (CHOSEN) ✅

```csharp
ParentId = null, ParentType = null  // Casual match
```

**Chosen because:**
- ✅ Semantically correct
- ✅ Simple
- ✅ Self-documenting
- ✅ No unnecessary entities
- ✅ Natural representation

---

## Validation Rules

To ensure data integrity:

```csharp
// Validation: Both must be null or both must be set
public static class MatchValidation
{
    public static bool IsValid(Match match)
    {
        // Valid: Both null (casual match)
        if (match.ParentId is null && match.ParentType is null)
            return true;
        
        // Valid: Both set (parented match)
        if (match.ParentId is not null && match.ParentType is not null)
            return true;
        
        // Invalid: One null, one set
        return false;
    }
    
    public static void ValidateOrThrow(Match match)
    {
        if (!IsValid(match))
        {
            throw new InvalidOperationException(
                "Match ParentId and ParentType must both be null or both be set. " +
                $"ParentId: {match.ParentId}, ParentType: {match.ParentType}"
            );
        }
    }
}
```

---

## Database Impact

### Storage

```sql
-- matches table
CREATE TABLE matches (
    id UUID PRIMARY KEY,
    parent_id UUID NULL,        -- NULL for casual matches
    parent_type INTEGER NULL,   -- NULL for casual matches
    -- other columns...
    
    CONSTRAINT check_parent_consistency 
        CHECK ((parent_id IS NULL AND parent_type IS NULL) 
            OR (parent_id IS NOT NULL AND parent_type IS NOT NULL))
);

-- Data examples:
parent_id                            | parent_type | Match Type
-------------------------------------|-------------|------------
'123e4567-e89b-12d3-a456-426614174000'| 0          | Scrimmage
'987e6543-e21b-34d5-c678-426614174001'| 1          | Tournament
NULL                                 | NULL       | Casual
```

**Benefits:**
- ✅ Nullable columns naturally represent optional parent
- ✅ Check constraint ensures consistency
- ✅ No extra table needed
- ✅ Efficient queries with indexes on `parent_type`

---

## Summary

### The Question
Should we create a `Casual` entity or include `Casual` in the `MatchParentType` enum?

### The Answer
**NO** on both counts.

### The Reasoning
1. **Semantic correctness:** `MatchParentType` describes parent entities; casual has no parent
2. **YAGNI:** Don't create entities without purpose
3. **Natural representation:** `null` perfectly represents "no parent"
4. **Domain truth:** Casual matches are fundamentally different from parented matches
5. **Simplicity:** Two nulls vs an empty entity + enum value

### The Implementation
```csharp
public enum MatchParentType  // Only actual parent types
{
    Scrimmage,
    Tournament,
}

// null + null = casual match ✅
```

### The Takeaway
**Consistency for its own sake can obscure domain meaning.**  
Let your domain model reflect reality, not force reality into patterns.

---

## Related Discussions

- `docs/.dev/.notes/.lat/match-parent-type-enum.md` - Full enum implementation
- `docs/.dev/.notes/.lat/match-game-snapshot-optimization.md` - Match/game state design

## Status
✅ **DECIDED AND IMPLEMENTED**

