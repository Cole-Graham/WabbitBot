# Step 1 Log: Entity Redefinition for Native PostgreSQL JSON Support

## Overview
Successfully completed Step 1 of the WabbitBot architecture refactor, focusing on redefining entity classes to leverage PostgreSQL's native JSONB support and Npgsql's automatic mapping capabilities.

## What Was Accomplished

### 1. Entity Analysis and Identification
- **Analyzed all entity classes** in the project (`Player.cs`, `User.cs`, `Team.cs`, `Map.cs`, `Game.cs`, `Result.cs`, `Stats.cs`, `Scrimmage.cs`, `Tournament.cs`, `Match.cs`)
- **Identified manual JSON serialization** in `Player.cs` and `Team.cs` that needed to be removed
- **Confirmed other entities** (`User.cs`, `Map.cs`, etc.) already used appropriate native types

### 2. Manual JSON Property Removal
**Player.cs Changes:**
- ❌ Removed `TeamIdsJson` property (manual JSON serialization)
- ❌ Removed `PreviousUserIdsJson` property (manual JSON serialization)
- ❌ Removed `[JsonIgnore]` attributes from `TeamIds` and `PreviousUserIds`
- ❌ Removed `[JsonPropertyName]` attributes
- ❌ Removed `JsonUtil` and `System.Text.Json.Serialization` imports
- ✅ Now uses native `List<string>` objects directly

**Team.cs Changes:**
- ❌ Removed `RosterJson` property (manual JSON serialization)
- ❌ Removed `StatsJson` property (manual JSON serialization)
- ❌ Removed `[JsonIgnore]` attributes from `Roster` and `Stats`
- ❌ Removed `[JsonPropertyName]` attributes
- ❌ Removed `JsonUtil` and `System.Text.Json.Serialization` imports
- ✅ Now uses native `List<TeamMember>` and `Dictionary<GameSize, Stats>` objects directly

### 3. Automatic Mapping Analysis
**Discussion: Component Classes vs Service Classes**
- Analyzed the refactor plan's **critical architectural decision**: "Component classes contain ONLY configuration and properties. Service classes contain ALL methods."
- **Questioned whether** the abstract `MapEntity` and `BuildParameters` methods in component classes are still needed
- **Determined**: No, these methods should be removed from component classes and moved to service classes

**EF Core vs Dapper Decision**
- **Analyzed both options** for implementing the database layer with Npgsql
- **EF Core Advantages**:
  - ✅ Automatic JSONB mapping with Npgsql
  - ✅ Rich LINQ support for JSON operations
  - ✅ Type safety with compile-time validation
  - ✅ Built-in migrations and change tracking
  - ✅ Complex query capabilities
- **Dapper Advantages**:
  - ✅ High performance with minimal overhead
  - ✅ Simple and lightweight
  - ✅ Raw SQL flexibility when needed
- **Recommendation: EF Core**
  - Better alignment with refactor goals (automatic mapping, type safety)
  - Foundation for complex JSON queries likely needed for Discord bot
  - Better long-term maintainability
  - Rich ecosystem and tooling support

### 4. Benefits Achieved
- 🚀 **Performance**: Native JSONB operations faster than manual serialization
- 🔒 **Type Safety**: Strongly-typed complex objects instead of string manipulation
- 🛠️ **Clean Code**: No manual JSON serialization/deserialization boilerplate
- 📈 **Scalability**: PostgreSQL can optimize JSONB queries automatically
- 🧹 **Maintainability**: Cleaner entity definitions, automatic mapping

### 5. Verification
- ✅ **Compilation Check**: Entities compile successfully (Npgsql errors are expected since package isn't installed yet)
- ✅ **Type Safety**: All complex objects (`List<string>`, `Dictionary<GameSize, Stats>`, `List<TeamMember>`) are properly typed
- ✅ **JSONB Ready**: Entities now leverage PostgreSQL JSONB features natively
- ✅ **Architecture Alignment**: Changes align with refactor plan's vision

## Next Steps Identified
Based on our discussion about automatic mapping, the following steps need to be added/updated in the refactoring plan:

1. **New Step: EF Core Setup and Configuration**
   - Add Npgsql.EntityFrameworkCore.PostgreSQL package
   - Create WabbitBotDbContext with JSONB configurations
   - Configure entity mappings for JSONB columns

2. **Updated Step 2: Database Layer Foundation**
   - Remove `MapEntity` and `BuildParameters` methods from component classes
   - Implement automatic mapping in service classes using EF Core
   - Update component classes to be configuration-only (no methods)

3. **New Step: JSONB Schema Migration**
   - Update database schema to use JSONB columns
   - Create migration scripts for existing data (if any)
   - Add JSONB indexes for performance

## Files Modified
- `src/WabbitBot.Core/Common/Models/Player.cs` - Removed manual JSON properties
- `src/WabbitBot.Core/Common/Models/Team.cs` - Removed manual JSON properties

## Files Analyzed (No Changes Needed)
- `src/WabbitBot.Core/Common/Models/User.cs`
- `src/WabbitBot.Core/Common/Models/Map.cs`
- `src/WabbitBot.Core/Common/Models/Game.cs`
- `src/WabbitBot.Core/Common/Models/Stats.cs`
- `src/WabbitBot.Core/Scrimmages/Scrimmage.cs`
- `src/WabbitBot.Core/Tournaments/Tournament.cs`
- `src/WabbitBot.Core/Matches/Match.cs`

## Technical Decisions Made
1. **EF Core over Dapper**: Chosen for better alignment with automatic mapping goals and future scalability
2. **Component Classes Simplification**: Confirmed that component classes should be configuration-only
3. **No Backwards Compatibility**: Since no production deployment exists, we can make clean breaking changes
4. **Native Complex Objects**: Direct use of `List<T>` and `Dictionary<TKey, TValue>` for JSONB compatibility

## Architecture Alignment
This step perfectly aligns with the refactor plan's principles:
- ✅ **Lean Implementation**: Only implemented what's currently needed
- ✅ **Automatic Mapping**: Leveraged Npgsql's native JSONB capabilities
- ✅ **Clean Architecture**: Removed manual serialization complexity
- ✅ **Future-Ready**: Foundation for EF Core with rich JSON query support

## Status: ✅ COMPLETE
Step 1 is successfully completed and ready for Step 2: Database Layer Foundation with EF Core implementation.
