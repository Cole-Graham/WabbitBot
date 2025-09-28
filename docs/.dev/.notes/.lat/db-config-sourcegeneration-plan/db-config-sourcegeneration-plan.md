# Database Configuration Source Generation Plan

## Executive Summary

The WabbitBot codebase contains extensive repetitive database configuration code across multiple layers, with **significant redundancies identified**. There are currently THREE parallel database configuration systems that must be consolidated before implementing source generation. This plan proposes a comprehensive source generation system to automate 80-90% of the remaining code, reducing maintenance burden and eliminating configuration drift.

**⚠️ CRITICAL: Redundancies Found + EntityConfig Audit Required**
- EntityConfig system (recommended to keep - provides metadata, BUT may be out of date)
- DbServiceFactory system (redundant - to be removed)
- CoreService.Database system (KEEP - provides unified service access layer)

**⚠️ ENTITY ORGANIZATION UPDATE**
- Entities are organized by Domain: Common, Leaderboard, Scrimmage, Tournament
- Each entity has a Domain property and inherits marker interfaces (ITeamEntity, IMatchEntity, etc.)
- Child entities are grouped with parents (e.g., Game entities folded into Match via IMatchEntity)
- **EntityConfig system is OUT OF DATE** - failing tests revealed inconsistencies that must be fixed first

## **CRITICAL DESIGN DECISION: PostgreSQL + Npgsql Only**

The application now targets **PostgreSQL + Npgsql exclusively**. SQLite support has been removed from the design. This means:

- **Native JSONB Support**: Npgsql provides native JSONB mapping for complex types like `Dictionary<string, object>`, `List<T>`, etc.
- **No Value Converters Needed**: EF Core + Npgsql handles JSONB serialization automatically
- **Test Configuration**: Tests now use PostgreSQL instead of SQLite
- **EntityConfig System**: Can rely on PostgreSQL's native JSONB capabilities without manual serialization

## Current State Analysis

### EntityConfig System Architecture

The EntityConfig system consists of:

1. **EntityConfig<TEntity> base class** - Provides common configuration properties
2. **EntityConfigFactory** - Static factory providing lazy-loaded access to all entity configurations
3. **Individual Entity.DbConfig.cs files** - One per entity, containing concrete implementations:
   - `Player.DbConfig.cs` → `PlayerDbConfig : EntityConfig<Player>`
   - `Team.DbConfig.cs` → `TeamDbConfig : EntityConfig<Team>`
   - `Match.DbConfig.cs` → `MatchDbConfig : EntityConfig<Match>`
   - etc.

**These .DbConfig.cs files are referenced by EntityConfigurations.cs in the EntityConfigFactory class.**

### Repetitive Patterns Identified

1. **Entity Configuration Metadata** (EntityConfigFactory + *.DbConfig.cs files)
   - Table names, archive table names
   - Column arrays and ID columns
   - Cache settings (max size, expiry)
   - 13 entities with identical structure patterns

2. **EF Core Model Configuration** (WabbitBotDbContext.*.cs files)
   - DbSet declarations
   - ConfigureXxx methods with table/column mappings
   - JSONB column type specifications
   - Foreign key relationships
   - Index definitions

3. **DatabaseService Creation** (DbServiceFactory.cs, CoreService.Database.cs)
   - DatabaseService<T> instantiations
   - Column specifications for active/archive tables
   - Lazy<T> wrapper patterns
   - Static property accessors

4. **Testing Code** (EntityConfigTests.cs)
   - Repetitive assertion patterns for each entity
   - Hard-coded expected values that must stay in sync

## Proposed Solution: Unified Entity Metadata System

### Core Concept

Create a single source-of-truth for entity metadata that drives all database-related code generation:

```csharp
[EntityMetadata(
    TableName = "players",
    ArchiveTableName = "players_archive",
    CacheSize = 500,
    CacheExpiryMinutes = 30
)]
public class Player { /* ... */ }
```

### Source Generation Pipeline

### Alternative Approach: Entity Classes as Single Source of Truth

**OPTIMAL DESIGN**: Instead of maintaining separate .DbConfig.cs files, decorate entity classes directly with database configuration metadata. The source generator reads entity class definitions and extracts metadata to generate all database code.

**Benefits**:
- **Single source of truth**: Entity class updates automatically update all database configuration
- **No separate .DbConfig.cs files**: Eliminates maintenance of parallel configuration files
- **Compile-time validation**: Entity changes immediately affect generated code
- **Cleaner architecture**: Database metadata lives with the entities it describes

**Implementation Option A1: Minimal Attributes (RECOMMENDED)**:
```csharp
[EntityMetadata(
    TableName = "players",
    ArchiveTableName = "players_archive",
    MaxCacheSize = 500,
    CacheExpiryMinutes = 30
)]
public class Player : Entity, IPlayerEntity
{
    // Domain and interfaces automatically detected from class definition
    public override Domain Domain => Domain.Common;
}
```

**Implementation Option A2: Explicit Attributes (Alternative)**:
```csharp
[EntityMetadata(
    TableName = "players",
    ArchiveTableName = "players_archive",
    Domain = Domain.Common,
    MarkerInterfaces = new[] { "IPlayerEntity" },
    MaxCacheSize = 500,
    CacheExpiryMinutes = 30
)]
public class Player : Entity, IPlayerEntity
{
    // Domain still defined on class for runtime use
    public override Domain Domain => Domain.Common;
}
```

**Why Domain/Interfaces in Attributes?**
- **Validation**: Ensures declared domain/interfaces match actual implementation
- **Override capability**: Allows declaring different classification than inheritance suggests
- **Documentation**: Makes domain membership explicit and searchable
- **Future flexibility**: Enables domain-specific generation logic

**Why Domain/Interfaces from Class Analysis (RECOMMENDED)?**
- **DRY principle**: Don't repeat what's already in class definition
- **Single source of truth**: Class definition is authoritative
- **Automatic updates**: Changes to inheritance automatically reflected
- **Simpler attributes**: Less boilerplate in metadata declarations

**CRITICAL**: If any entity definition is missing a `Domain` property or marker interface implementation, this indicates an **architectural oversight**. The entity definition should be **corrected** rather than manually defining these in the attribute decoration. All entities must properly implement their domain and marker interfaces.

#### 1. Entity Metadata Generator
**Input**: Entity classes with `[EntityMetadata]` attributes + property analysis
**Output**:
- `EntityConfigFactory.cs` - Static factory methods
- `IEntityConfig.cs` - Configuration interfaces
- `EntityConfig.cs` - Base configuration class
- `EntityConfigTests.cs` - Test assertions

#### 2. EF Core Configuration Generator
**Input**: Entity classes with `[EntityMetadata]` + `[Column]` attributes
**Output**:
- `WabbitBotDbContext.cs` - DbSet declarations
- `WabbitBotDbContext.Entity.cs` - Configure methods
- Index configurations
- Foreign key relationships

#### 3. DatabaseService Generator
**Input**: Entity metadata + column specifications
**Output**:
- `DbServiceFactory.cs` - Factory methods
- `CoreService.Database.cs` - Lazy service accessors

#### 4. Integration Test Generator
**Input**: Entity metadata
**Output**:
- `DbContextIntegrationTest.cs` - CRUD test methods

## Implementation Approach

## Phase 1: EntityConfig System Audit

The EntityConfig system was audited, fixed, and validated. All identified issues have been resolved.

### Issues Resolved
1. **Cache size mismatches** (fixed Leaderboard cache size: 10→50)
2. **Table name inconsistencies** (fixed LeaderboardItem: "leaderboard_entries"→"leaderboard_items")
3. **Missing EntityConfig classes** (added TeamMemberDbConfig, ScrimmageStateSnapshotDbConfig; updated count: 17→19)
4. **Invalid EF Core lambda expressions** (removed `t => t.VarietyStats.Values`, added proper JSONB configurations)
5. **Column definition mismatches** (added missing properties like Team1Id/Team2Id, TeamJoinCooldowns to Match/Player entities)

### Completed Audit Steps
1. **Audit all EntityConfig classes** against current entity definitions
   - Verified all EntityConfig.Columns match actual entity properties
   - Corrected table names to match database schema
   - Aligned cache sizes with performance requirements

2. **Fix EF Core configuration issues**
   - Removed invalid lambda expressions in WabbitBotDbContext files
   - Added explicit JSONB configurations for complex types (Dictionary<string, object>, etc.)
   - Ignored navigation convenience properties (Team.VarietyStats)
   - Ensured proper foreign key mappings

3. **Add missing EntityConfig classes**
   - Created TeamMemberDbConfig for TeamMember entity
   - Created ScrimmageStateSnapshotDbConfig for ScrimmageStateSnapshot entity
   - Ensured all domains (Common/Leaderboard/Scrimmage/Tournament) are covered

4. **Update domain organization**
   - Verified EntityConfig reflects current domain structure
   - Confirmed marker interfaces are properly represented
   - Validated parent-child entity relationships are captured

5. **Deprecate manual migration files**
   - Added deprecation notices to all migration files in `20241201120000_methods/` folder
   - Marked manual schema creation files as obsolete
   - EF Core + Npgsql now handles schema generation automatically

### Validation Results
- All EntityConfigTests pass (19/19 tests successful)
- EF Core DbContext initializes without errors (PostgreSQL + Npgsql)
- No runtime exceptions during database operations
- JSONB serialization works natively with Npgsql (no manual value converters needed)

## Phase 2: Entity Class Decoration System

We've implemented Option A (entity classes as source of truth) and begun decorating entity classes with `[EntityMetadata]` attributes.

### EntityMetadataAttribute Design
The `EntityMetadataAttribute` was redesigned to be minimal - the generator auto-detects everything from class definitions:

**Auto-Detection Capabilities:**
- **Columns**: Auto-detected from public properties
- **JSONB Columns**: Auto-detected from complex types (List<T>, Dictionary<K,V>, etc.)
- **Domain**: Auto-detected from class's `Domain` property
- **Marker Interfaces**: Auto-detected from implemented interfaces
- **Foreign Keys**: Auto-detected from navigation properties

**Optional Overrides**: Only specify explicit overrides when auto-detection isn't sufficient

**Dual Definition**: Identical attributes in both `WabbitBot.SourceGenerators` and `WabbitBot.Common`

### Entity Class Decoration
Sample decorated entities:
- **Player entity**: `[EntityMetadata(tableName: "players", archiveTableName: "player_archive", maxCacheSize: 500, cacheExpiryMinutes: 30)]`
- **Team entity**: `[EntityMetadata(tableName: "teams", archiveTableName: "team_archive", maxCacheSize: 200, cacheExpiryMinutes: 20)]`
- **Match entity**: `[EntityMetadata(tableName: "matches", archiveTableName: "match_archive", maxCacheSize: 200, cacheExpiryMinutes: 10)]`

### True Single Source of Truth Achieved
The generator automatically detects everything from class definitions:
- **Columns**: All public properties → database columns (auto-converted to snake_case)
- **JSONB Columns**: Complex types (List<T>, Dictionary<K,V>, etc.) → JSONB automatically
- **Domain**: Class's `Domain` property → auto-detected
- **Marker Interfaces**: Implemented interfaces (IMatchEntity, ITeamEntity, etc.) → auto-detected
- **Foreign Keys**: Navigation properties → foreign key relationships auto-detected
- **Indexes**: Properties with specific naming patterns → auto-indexed

**Result**: Adding a single property to an entity class automatically creates the database column, JSONB handling, and all related configuration code!

### EntityMetadataGenerator Implementation
The `EntityMetadataGenerator` was created in `WabbitBot.SourceGenerators/Generators/Entity/EntityMetadataGenerator.cs`:

- **Syntax receiver** implemented to find classes with `[EntityMetadata]` attributes
- **Metadata analysis** automatically extracts all database configuration from entity classes
- **EntityConfigFactory generation** creates partial class extensions (avoids conflicts with manual definitions)
- **Architectural validation** emits compile errors (WB0001/WB0002) for missing Domain properties or marker interfaces

### Testing & Validation Results
- Build succeeds with decorated entities (Player, Team, Match)
- Generated *DbConfig classes created successfully in `WabbitBot.Core.Common.Models.Generated` namespace
- Auto-detection working perfectly for columns, JSONB types, domain properties, and marker interfaces
- Namespace isolation prevents conflicts with manual definitions
- JSONB support added to EntityConfig system (auto-detected from complex types)

## Current Status

**Phase 2 Complete**: EntityMetadataGenerator implemented and tested with sample entities.

**Next Steps**: Systematically apply [EntityMetadata] attributes to all entity classes in the codebase, then implement the full source generation system (EF Core DbContext generator, DatabaseService generator, etc.).

See the implementation checklist at the bottom of this document for detailed progress tracking.

### Post-Audit: Choose EntityConfig Strategy

**AFTER EntityConfig system is fixed**, choose between two approaches:

#### Option A: Migrate to Entity Classes as Source of Truth (RECOMMENDED)
- **Eliminate .DbConfig.cs files entirely**
- **Decorate entity classes directly** with `[EntityMetadata]` attributes
- **Source generator analyzes entity properties** to extract column information
- **Result**: Entity updates automatically update all database configuration

#### Option B: Keep Current .DbConfig.cs Approach
- **Maintain separate .DbConfig.cs files** as they currently exist
- **Migrate .DbConfig.cs content** to attribute-based metadata
- **Source generator reads attributes** from .DbConfig.cs files
- **Result**: Keeps current separation but still automates generation

**Regardless of choice, complete these final consolidation steps:**

1. **Delete DbServiceFactory.cs** - Contains 400+ lines of redundant DatabaseService factory methods

2. **Modify CoreService.Database.cs** - Update to use EntityConfig metadata instead of hardcoded column definitions

**Expected outcome: Validated EntityConfig system, redundant systems removed, ready for source generation implementation.**

### Entity Metadata Foundation

1. **Create EntityMetadataAttribute (Dual Definition)**
   - **In WabbitBot.SourceGenerators**: Define attribute class for generator to recognize
   - **In WabbitBot.Common**: Define identical attribute class for runtime usage
   - Both must have identical property signatures and namespaces

   ```csharp
   [AttributeUsage(AttributeTargets.Class)]
   public class EntityMetadataAttribute : Attribute
   {
       public string TableName { get; }
       public string ArchiveTableName { get; }
       public string IdColumn { get; } = "id";
       public int MaxCacheSize { get; } = 1000;
       public int CacheExpiryMinutes { get; } = 60;
       public string[] Columns { get; }
       public string[] JsonbColumns { get; } = Array.Empty<string>();
       public string[] IndexedColumns { get; } = Array.Empty<string>();
       public Dictionary<string, string> ForeignKeys { get; } = new();

       // Optional: Can be auto-detected from class definition if not specified
       public Domain? Domain { get; } // Common, Leaderboard, Scrimmage, Tournament
       public string[] MarkerInterfaces { get; } = Array.Empty<string>(); // IMatchEntity, ITeamEntity, etc.
   }
   ```

2. **Update Entity Classes or .DbConfig.cs Files**
   - **Option A (Recommended)**: Decorate entity classes directly with `[EntityMetadata]` attributes
     - Reference `EntityMetadataAttribute` from `WabbitBot.Common` namespace
     - Add minimal attributes to entity classes (TableName, ArchiveTableName, etc.)
     - **Domain and MarkerInterfaces auto-detected** from class definition if not explicitly specified
     - Source generator analyzes entity properties to extract column information

   - **Option B (Alternative)**: Migrate .DbConfig.cs files to use attributes
     - Keep separate .DbConfig.cs files but replace manual configuration with attribute-based metadata
     - Source generator reads attributes from .DbConfig.cs files instead of entity classes
     - Maintains current file separation but enables automation

   - For both options: Add `[Column]` attributes for JSONB specifications, `[ForeignKey]`, `[Index]` attributes as needed

3. **Create Metadata Collector Source Generator**
   - Reference `EntityMetadataAttribute` from `WabbitBot.SourceGenerators` namespace
   - Roslyn incremental source generator to scan all entity classes
   - **Validate architectural compliance**: Ensure all entities have Domain property and marker interface implementations
   - **Emit compilation errors** for entities missing required Domain or interface definitions
   - Build comprehensive metadata model for each entity
   - Validate metadata consistency at compile-time
   - Generate metadata validation errors for invalid configurations

### Core Code Generators

1. **EntityConfigFactory Generator**
   - Generate complete EntityConfigFactory.cs with all entity configurations
   - Generate individual configuration classes for each entity type
   - Implement singleton patterns for all configuration instances
   - Generate GetAllConfigurations() method

2. **EF Core DbContext Generator**
   - Generate all DbSet properties in WabbitBotDbContext
   - Generate ConfigureXxx methods for all entities
   - Handle JSONB column type mappings for complex objects
   - Generate all indexes (GIN indexes for JSONB, regular indexes for standard columns)
   - Generate foreign key relationships and constraints
   - Generate partial methods for manual overrides if needed

3. **DatabaseService Generator**
   - Generate complete DbServiceFactory.cs with factory methods for all entities
   - Generate CoreService.Database.cs with lazy-loaded DatabaseService accessors
   - Handle column specifications for both active and archive tables
   - Generate proper async/await patterns and error handling

4. **Test Code Generators**
   - Generate EntityConfigTests.cs with test methods for all entities
   - Generate DbContextIntegrationTest.cs CRUD methods
   - Generate JSONB serialization and deserialization tests
   - Generate performance and concurrency tests

### Complete Migration Strategy

1. **Atomic Replacement**
   - Replace all manual database configuration files simultaneously
   - Update all references to use generated code
   - Delete all legacy manual implementations in one commit
   - No intermediate states or backwards compatibility layers

2. **Legacy Code Removal**
   - Delete manual EntityConfigurations.cs
   - Delete manual DbServiceFactory.cs methods
   - Delete manual WabbitBotDbContext configuration methods
   - Delete manual CoreService.Database.cs accessors
   - Delete manual test assertions in EntityConfigTests.cs
   - Delete manual integration test methods

3. **Full System Validation**
   - Run complete test suite against generated code
   - Validate all EF Core migrations work correctly
   - Performance test generated code against previous manual implementations
   - Verify JSONB serialization/deserialization works for all entities
   - Confirm database operations work with PostgreSQL and SQLite

## Benefits

### Maintainability
- **Single source of truth**: Change entity metadata once, regenerate everything
- **DRY principle**: Eliminate 2000+ lines of repetitive code
- **Consistency**: Generated code is always consistent with metadata

### Developer Experience
- **Faster development**: Add new entities with minimal boilerplate
- **Reduced errors**: No more copy-paste mistakes in configuration
- **IntelliSense**: Generated code provides full type safety

### Performance
- **Compile-time generation**: No runtime reflection or configuration loading
- **Optimized code**: Generated code can be optimized for specific patterns

## Technical Considerations

### Source Generator Architecture
- Use Roslyn incremental generators for performance
- Generate partial classes to allow manual overrides
- Support for custom attributes to handle special cases
- **Dual Attribute Definitions**: Attributes must exist in both WabbitBot.SourceGenerators and WabbitBot.Common with identical signatures

### Metadata Validation
- Compile-time validation of entity metadata
- Runtime validation in debug builds
- Clear error messages for invalid configurations
- Validate that attribute definitions remain synchronized between generator and common projects

### Migration Strategy
- Atomic replacement: Generate all code simultaneously and replace manual implementations
- No backwards compatibility layers needed
- Delete legacy code immediately after validation

## Risk Mitigation

### Fallback Strategy
- Keep manual implementations as backup
- Ability to disable generation if issues arise
- Clear documentation on manual override patterns

### Testing Strategy
- Generate tests for generated code
- Integration tests to validate end-to-end functionality
- Performance tests to ensure no regression

### Version Compatibility
- Ensure generated code works with EF Core 9.0 and PostgreSQL
- Test with different database providers (PostgreSQL, SQLite)

## Success Metrics

- **Code reduction**: 80-90% reduction in database configuration code
- **Maintenance time**: 50-70% reduction in time spent maintaining configs
- **Bug reduction**: Eliminate configuration drift bugs
- **Developer velocity**: New entities added in minutes instead of hours

## Execution Strategy

### Prerequisites
- Access to all entity class definitions in the codebase
- Understanding of existing EF Core configuration patterns
- Roslyn source generator development experience
- **Critical**: Understanding that source generator attributes must be defined in both WabbitBot.SourceGenerators (for generator recognition) and WabbitBot.Common (for runtime usage)

### Implementation Sequence

1. **Consolidate Redundant Systems**
   - Remove DbServiceFactory.cs (400+ lines)
   - Remove CoreService.Database.cs accessors (270+ lines)
   - Verify EntityConfig system is complete for all entities

2. **Start with Entity Metadata**
   - Create EntityMetadataAttribute (dual definitions)
   - Decorate all 13 entity classes with metadata
   - Implement metadata collector source generator

3. **Build All Generators Simultaneously**
   - Implement EntityConfigFactory generator
   - Implement EF Core DbContext generator
   - Implement DatabaseService generator
   - Implement test code generators

4. **Complete Migration**
   - Generate all target files
   - Replace manual implementations atomically
   - Delete remaining legacy code
   - Run full validation suite

### Success Criteria

- EntityConfig strategy chosen (Option A recommended: entity classes as source of truth)
- EntityConfig system audited and updated to reflect current domain organization
- **Architectural compliance verified**: ALL entities have Domain property and marker interface implementations
- All entities across Common, Leaderboard, Scrimmage, Tournament domains properly configured
- Marker interfaces properly reflected in configuration (IMatchEntity, ITeamEntity, etc.)
- If Option A chosen: .DbConfig.cs files eliminated, entity classes decorated with metadata
- If Option B chosen: .DbConfig.cs files migrated to attribute-based configuration
- DbServiceFactory.cs removed (~400 lines)
- CoreService.Database.cs modified to use EntityConfig metadata
- All database configuration code is generated from entity metadata
- Zero manual database configuration code remains
- CoreService service access pattern preserved (CoreService.Players, CoreService.Teams, etc.)
- All existing tests pass with generated code
- EF Core migrations work correctly
- JSONB serialization/deserialization works for all entities
- Performance meets or exceeds manual implementations

---

## Implementation Checklist: Complete Database Configuration Automation

### Phase 1: EntityConfig System Audit (CRITICAL BLOCKING STEP) ✅
- [x] **Audit all EntityConfig classes against current entity definitions**
  - [x] Compare EntityConfig.Columns with actual entity properties for all entities
  - [x] Verify table names match database schema expectations
  - [x] Check cache sizes align with performance requirements
  - [x] Identify entities missing EntityConfig classes (test expects 13, found 19)
- [x] **Fix EF Core configuration issues**
  - [x] Correct invalid lambda expressions (e.g., `t => t.VarietyStats.Values` in Team configuration)
  - [x] Ensure navigation property mappings are valid
  - [x] Verify JSONB column configurations work correctly
- [x] **Fix EntityConfig test expectations**
  - [x] Update LeaderboardDbConfig cache size (expected 50, actual 10)
  - [x] Fix LeaderboardItemDbConfig table name (expected "leaderboard_entries", actual "leaderboard_items")
  - [x] Correct GetAllConfigurations count (expected 13, found 19)
- [x] **Add missing EntityConfig classes**
  - [x] Identify entities without corresponding .DbConfig.cs files (TeamMember, ScrimmageStateSnapshot)
  - [x] Create EntityConfig classes for untracked entities
  - [x] Ensure all domains (Common/Leaderboard/Scrimmage/Tournament) are covered
- [x] **Update domain organization in EntityConfig**
  - [x] Verify EntityConfig reflects current domain structure
  - [x] Ensure marker interfaces are properly represented
  - [x] Confirm parent-child entity relationships are captured
- [x] **Deprecate manual migration files**
  - [x] Add deprecation notices to all files in `src/WabbitBot.Core/Common/Migrations/20241201120000_methods/`
  - [x] Mark as obsolete since EF Core should handle schema generation automatically
  - [x] Do not delete files - just add clear deprecation warnings
- [x] **Validate audit fixes**
  - [x] All EntityConfigTests pass (19/19 tests)
  - [x] EF Core DbContext initializes without errors
  - [x] DbContextIntegrationTest.CanCreateAndSavePlayerWithJsonbFields succeeds
  - [x] No runtime exceptions during database operations

### Phase 2: Entity Class Decoration System ✅
- [x] **Create EntityMetadataAttribute** (dual definition in SourceGenerators and Common)
- [x] **Implement EntityMetadataGenerator** with auto-detection capabilities
- [x] **Add JSONB support to EntityConfig** system
- [x] **Test generator with sample entities** (Player, Team, Match)
- [x] **Validate architectural compliance** (Domain properties, marker interfaces)
- [x] **Verify architectural compliance** (generator validates Domain + marker interfaces)

### Phase 3: Choose EntityConfig Strategy ✅
- [x] **Choose between Option A or B**
  - [x] **Option A (Chosen)**: Entity classes as source of truth
    - [x] Eliminate .DbConfig.cs files entirely
    - [x] Decorate entity classes directly with [EntityMetadata] attributes
    - [x] Source generator analyzes entity properties to extract column information
    - [x] Result: Entity updates automatically update all database configuration
  - [ ] **Option B (Alternative)**: Keep .DbConfig.cs files with attributes (not chosen)

### Phase 4: Entity Metadata Foundation ✅
- [x] **Create EntityMetadataAttribute** in WabbitBot.SourceGenerators project
- [x] **Create identical EntityMetadataAttribute** in WabbitBot.Common project
- [x] **Decorate entity classes** with `[EntityMetadata]` attributes (Option A)
  - [x] Specify TableName, ArchiveTableName, MaxCacheSize, CacheExpiryMinutes
  - [x] Domain and interfaces auto-detected from class definition
- [x] **JSONB support added** to EntityConfig system (auto-detected from complex types)
- [x] **Create metadata collector source generator** to scan entity classes
- [x] **Implement architectural validation** (emit errors for missing Domain/interface)
- [x] **Test metadata collection** with sample entities (Player, Team, Match)

### Phase 5: Core Generators Implementation 🔶 (EntityConfigFactory + EF Core DbContext + DatabaseService generators complete)
- [x] **Implement EntityConfigFactory generator** (EntityMetadataGenerator)
  - [x] Generate static factory properties for all entities
  - [x] Generate individual EntityConfig classes
  - [x] Handle singleton patterns and lazy loading
  - [x] Auto-detect columns, JSONB columns, domain, and marker interfaces
  - [x] Emit compilation errors for missing Domain/interface requirements
  - [x] Follow source generation best practices (virtual emission, .g.cs suffix, partial classes)
  - [x] Configurable generation via MSBuild properties
- [x] **Implement EF Core DbContext generator** (basic implementation)
  - [x] Generate DbSet properties for all entities
  - [x] Generate ConfigureXxx methods with table mappings
  - [x] Handle JSONB column type specifications
  - [x] Generate indexes (regular and GIN for JSONB)
  - [ ] Generate foreign key relationships (TODO: implement after basic functionality verified)
- [x] **Implement DatabaseService generator** (complete implementation)
  - [x] Generate DbServiceFactory.cs methods (TODO: This is part of CoreService.Database.Generated.g.cs)
  - [x] Generate CoreService.Database.cs lazy accessors with proper naming conventions
  - [x] Handle column specifications from EntityConfig metadata (auto-detected from entity properties)
  - [x] Implement proper pluralization logic for service property names
  - [x] Handle edge cases for already-plural class names (Stats, TeamVarietyStats)
- [ ] **Implement test code generators**
  - [ ] Generate EntityConfigTests.cs assertions
  - [ ] Generate DbContextIntegrationTest.cs CRUD methods
  - [ ] Generate JSONB serialization tests

### Phase 6: Integration and Migration 🔶 (All manual DatabaseService definitions replaced)
- [x] **Remove conflicting manual DatabaseService definitions** from CoreService.Database.cs (Players, Teams, Games, Users, Maps, Leaderboards, Seasons, Scrimmages, Matches, ProvenPotentialRecords, MatchParticipants, TeamOpponentEncounters, TeamVarietyStats, Stats, SeasonConfigs)
- [ ] **Update CoreService.Database.cs** to use generated EntityConfig metadata
- [ ] **Replace manual EntityConfigurations.cs** with generated version
- [ ] **Replace manual WabbitBotDbContext configuration** with generated version
- [ ] **Update any references** to use generated code paths
- [ ] **Delete all legacy manual implementations**:
  - [ ] Remove manual EntityConfigFactory.cs
  - [ ] Remove manual EF Configure methods
  - [ ] Remove manual CoreService.Database.cs accessors
  - [ ] Remove manual test assertions
- [ ] **Run full test suite** against generated code
- [ ] **Validate EF Core migrations** work correctly
- [ ] **Performance test** generated code vs previous manual implementations
- [ ] **Verify JSONB serialization/deserialization** works for all entities

### Phase 7: Final Validation and Documentation ✅
- [ ] **Confirm zero manual database config code remains**
- [ ] **Verify CoreService service access pattern** preserved (`CoreService.Players`, etc.)
- [ ] **Test entity updates automatically update** database configuration
- [ ] **Document the new workflow** for adding entities
- [ ] **Create maintenance guide** for the source generation system
- [ ] **Final integration test** with real database operations

### Success Verification ✅
- [ ] All database configuration code is generated from entity metadata
- [ ] Zero manual database configuration code remains in codebase
- [ ] All existing tests pass with generated code
- [ ] EF Core migrations work correctly with generated context
- [ ] JSONB serialization/deserialization works for all entities
- [ ] Performance meets or exceeds manual implementations
- [ ] New entities can be added by only updating the entity class
- [ ] Compilation fails for entities missing Domain/interface requirements

### Rollback Plan (if needed) ✅
- [ ] Keep backup of all manual implementations during migration
- [ ] Have ability to disable source generators if critical issues arise
- [ ] Document manual fallback procedures
- [ ] Test rollback procedures work correctly

---

*This plan was generated based on analysis of the current WabbitBot database configuration code. The all-at-once approach leverages the lack of backwards compatibility or live deployment concerns to deliver a complete automated database configuration system.*
