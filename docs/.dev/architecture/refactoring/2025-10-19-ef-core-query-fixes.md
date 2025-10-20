# EF Core Query Translation Fixes

**Date**: 2025-10-19  
**Issue**: PostgreSQL/EF Core cannot translate `string.Contains()`, `string.Equals()` with `StringComparison` parameters to SQL

## Problem

PostgreSQL's EF Core provider cannot translate C# string comparison methods with `StringComparison` parameters inside LINQ queries. This caused runtime errors when autocomplete queries tried to execute:

```
System.InvalidOperationException: The LINQ expression '...Name.Contains(value: __term_2, comparisonType: OrdinalIgnoreCase)' could not be translated.
```

## Root Cause

When using:
- `string.Contains(value, StringComparison.OrdinalIgnoreCase)` 
- `string.Equals(value, StringComparison.OrdinalIgnoreCase)`

inside LINQ queries against a database, EF Core needs to translate these to SQL. PostgreSQL doesn't have a direct SQL translation for these C# methods with comparison parameters.

## Solution

Replaced all database query string comparisons with `EF.Functions.ILike()`, which is PostgreSQL's native case-insensitive pattern matching operator:

### Before (fails):
```csharp
.Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
.Where(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
```

### After (works):
```csharp
.Where(t => EF.Functions.ILike(t.Name, $"%{term}%"))  // Pattern match
.Where(t => EF.Functions.ILike(t.Name, name))         // Exact match (case-insensitive)
```

## Files Changed

### `src/WabbitBot.DiscBot/App/Providers/Autocomplete.cs`

All autocomplete providers updated:

1. **UserTeamAutoComplete** (line 37)
   - Team name search

2. **OpponentTeamAutoComplete** (line 79)
   - Opponent team name search

3. **RosterPlayerAutoComplete** (lines 121, 130-131)
   - Team name exact match
   - Player username/global name search

4. **ActiveChallengeAutoComplete** (lines 180-181)
   - Challenger and opponent team name search

5. **CancellableChallengeAutoComplete** (lines 233-234)
   - Challenger and opponent team name search

6. **UserActiveGameAutoComplete** (lines 290-292)
   - Team1/Team2 names and map name search

7. **UserActiveScrimmageAutoComplete** (lines 352-353)
   - Team1/Team2 names search

8. **ScrimmagePlayerAutoComplete** (lines 420-427)
   - Player username/global name search

9. **SubstitutePlayerAutoComplete** (lines 522-529)
   - Player username/global name search

## Verification

All instances of `StringComparison` in LINQ queries were audited:

✅ **Fixed**: All `.Contains()` and `.Equals()` with `StringComparison` in `Autocomplete.cs`  
✅ **Verified**: All `.StartsWith()` and `.EndsWith()` with `StringComparison` are in-memory operations (not database queries)  
✅ **Verified**: String comparisons in `MatchRenderer.cs`, `GameRenderer.cs`, and `MatchCore.Game.cs` are in-memory  

## Notes

- `EF.Functions.ILike()` is PostgreSQL-specific but perfect for our use case
- For cross-database compatibility, alternative would be `.Where(t => t.Name.ToLower().Contains(term.ToLower()))`, but less efficient
- Pattern for `ILike()`:
  - `$"%{term}%"` = contains (anywhere)
  - `$"{term}%"` = starts with
  - `$"%{term}"` = ends with
  - `$"{term}"` = exact match (case-insensitive)

## Related Issues

- Service initialization null reference fixed in `CoreService.cs` (added `Initialize()` method)
- Database initialization updated to support environment-aware strategy (dev vs production)

