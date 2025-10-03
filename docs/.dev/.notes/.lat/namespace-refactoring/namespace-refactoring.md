# WabbitBot Namespace Refactoring Plan

## Executive Summary

This document outlines a comprehensive \"big-bang\" refactoring plan to align all C# namespaces in the WabbitBot project exactly with the underlying directory structure. The goal is to achieve a 1:1 mapping where the namespace for each file precisely reflects its relative path from the project root (e.g., a file at `src/WabbitBot.Core/Common/Models/Match/Match.cs` would have `namespace WabbitBot.Core.Common.Models.Match;`). 

This refactoring will:
- Eliminate namespace inconsistencies currently present across projects.
- Improve code maintainability and IDE support (e.g., better IntelliSense, refactoring tools).
- Ensure generated code from source generators aligns with the new structure.
- Preserve the vertical slice architecture and event-driven design principles.

The plan is designed for execution in a single, coordinated pass to minimize partial states and compilation errors. It assumes a development environment with full project backup and version control (Git) for rollback.

**Important Assumptions:**
- All changes will be committed in a single feature branch.
- Testing will be performed post-refactor using existing unit/integration tests.
- No runtime behavior changes; only structural.
- Source generators will be updated to produce code matching new namespaces.

## 1. Current State Analysis

### Project Overview
The WabbitBot solution consists of several key projects:
- **WabbitBot.Common**: Shared infrastructure (data, events, models, utilities). ~50 files.
- **WabbitBot.Core**: Business logic slices (Leaderboards, Scrimmages, Tournaments, Common utilities). ~70 files.
- **WabbitBot.DiscBot**: Discord integration layer (DSharpPlus-specific and DiscBot abstractions). ~43 files.
- **WabbitBot.SourceGenerators**: Compile-time code generation for events, commands, embeds, database. ~23 files.
- **WabbitBot.Generator.Shared**: Shared utilities for generators and analyzers. ~9 files.
- **WabbitBot.Analyzers**: Roslyn analyzers for code quality. ~3 files.
- Test projects: WabbitBot.Common.Tests, WabbitBot.Analyzers.Tests, etc.

No explicit `<RootNamespace>` in .csproj files, so namespaces are manually declared and default to project name + relative path if not specified. However, explicit declarations show inconsistencies.

### Key Inconsistencies Identified
Based on codebase analysis (grep for `namespace` declarations):

#### WabbitBot.Common
- Directories: Attributes/, Data/(with subdirs Service/, Schema/, Interfaces/), Events/(EventInterfaces/), Models/, root files (e.g., Configuration.cs).
- Current Namespaces:
  - Root/Configuration.cs: `WabbitBot.Common.Configuration` (mismatch; should be `WabbitBot.Common`). ✅
  - Attributes/*.cs: `WabbitBot.Common.Attributes` (matches). 
  - Data/Service/*.cs: `WabbitBot.Common.Data.Service` (matches).
  - Data/Interfaces/*.cs: `WabbitBot.Common.Data.Interfaces` (matches).
  - Data/Schema/Migrations/*.cs: `WabbitBot.Common.Data.Schema.Migrations` (matches).
  - Events/*.cs: `WabbitBot.Common.Events` (matches).
  - Events/EventInterfaces/*.cs: `WabbitBot.Common.Events.EventInterfaces` (matches).
  - Models/*.cs: `WabbitBot.Common.Models` (matches).
- Issues: Inconsistent root namespace (e.g., Configuration.cs); some files use partial classes across namespaces. ✅

#### WabbitBot.Core
- Directories: Common/(BotCore/, Commands/, Config/, Database/, Events/, Handlers/, Interfaces/, Models/(Common/(Map/, Match/, Player/, Team/, User/)/, Leaderboard/, Scrimmage/, Tournament/), Services/), Leaderboards/, Scrimmages/(Data/, ScrimmageRating/), Tournaments/, root (Program.cs).
- Current Namespaces:
  - Root/Program.cs: `WabbitBot.Core` (matches).
  - Common/Database/*.cs: `WabbitBot.Core.Common.Database` (matches).
  - Common/Models/*.cs: `WabbitBot.Core.Common.Models` (partial match; subdirs add more). ✅
  - Common/Models/Common/Match/*.cs: `WabbitBot.Core.Common.Models` (incomplete; should include .Common.Match). ✅
  - Leaderboards/*.cs: `WabbitBot.Core.Leaderboards` (matches).
  - Scrimmages/*.cs: `WabbitBot.Core.Scrimmages` (matches).
  - Common/Services/*.cs: `WabbitBot.Core.Common.Services` (matches).
  - Common/BotCore/*.cs: `WabbitBot.Core.Common.BotCore` (matches).
  - Common/Commands/*.cs: `WabbitBot.Core.Common.Configuration` (typo/mismatch; should be .Commands). ✅
- Issues: Redundant \"Common.Common\" in Models paths (e.g., Models/Common/Match → namespace WabbitBot.Core.Common.Models.Common.Match). Incomplete nesting in some declarations. Typo in ConfigurationCommands.cs.

#### WabbitBot.DiscBot
- Directories: DiscBot/(Base/, ErrorHandling/, Events/, Services/), DSharpPlus/(Attributes/, Commands/, Embeds/, Interactions/), root (DiscBot.cs).
- Current Namespaces:
  - Many files have commented-out namespaces (e.g., `// namespace WabbitBot.DiscBot.DSharpPlus.Attributes`), indicating ongoing transition.
  - Active: `WabbitBot.DiscBot.DSharpPlus.Interactions`, `WabbitBot.DiscBot.DSharpPlus`, `WabbitBot.DiscBot.DSharpPlus.Commands`, `WabbitBot.DiscBot.DiscBot.Events`.
- Issues: Incomplete/mixed active and commented namespaces. Inconsistent between DiscBot/ and DSharpPlus/ subprojects.

#### WabbitBot.SourceGenerators
- Directories: Attributes/, Generators/(Command/, CrossBoundary/, Database/, Embed/, Event/), Shared/, Templates/, Utils/.
- Current Namespaces:
  - Mostly match: `WabbitBot.SourceGenerators.Generators.Event`, `WabbitBot.SourceGenerators.Templates`, etc.
  - Generated code hardcodes targets like `namespace WabbitBot.Core.Common.Events`, `WabbitBot.DiscBot.DSharpPlus.Commands`, `WabbitBot.Core.Common.Database`.
- Issues: Hardcoded target namespaces in templates will break post-refactor unless updated.

#### WabbitBot.Generator.Shared & WabbitBot.Analyzers
- Directories: Analyzers/, Metadata/, Utils/ (Shared); Analyzers/, Descriptors/ (Analyzers).
- Current Namespaces: Fully match (e.g., `WabbitBot.Generator.Shared.Analyzers`).
- Issues: None major; minor updates needed for any cross-references.

#### Tests
- Namespaces generally match project + .Tests (e.g., `WabbitBot.Common.Tests`).
- Issues: Will need using updates for refactored namespaces.

### Dependency Impacts
- **Inter-project References:** Core depends on Common; DiscBot on Core/Common; Generators/Analyzers on Shared/Common.
- **Using Statements:** ~500+ across codebase; systematic replacement needed (e.g., old `WabbitBot.Core.Common.Models` → new nested).
- **Generated Code:** Source generators output files in obj/generated/ with hardcoded namespaces; must update templates (e.g., EventTemplates.cs, DbContextGenerator.cs).
- **Events & Interfaces:** Event buses (CoreEventBus, GlobalEventBus) reference interfaces across namespaces; update IEvent implementations.
- **Analyzers:** May flag namespace mismatches; update diagnostic rules if needed.
- **Build/Tests:** Expect compilation errors until all usings/namespaces synced; tests may fail due to type resolution.

## 2. Proposed Directory Structure Changes

To achieve clean 1:1 namespaces without excessive depth or redundancy, recommend minor restructuring. These changes reduce nesting while preserving logical grouping (vertical slices, shared Common).

### General Principles
- Flatten redundant folders (e.g., remove \"Common\" under Models if it duplicates).
- Group related files logically but avoid deep nesting (>4 levels).
- No major architectural shifts; maintain vertical slices (e.g., Leaderboards/ as top-level in Core).
- Move deprecated files to src/deprecated/ if not already.

### Specific Recommendations
#### WabbitBot.Common
- Move root Configuration.cs to Configuration/ (create dir) for `WabbitBot.Common.Configuration`.
- Data/ remains; ensure subdirs like Service/, Schema/Migrations/ stay.
- Events/EventInterfaces/ → Events/Interfaces/ (flatten; namespace WabbitBot.Common.Events.Interfaces).
- Models/ root files stay; no sub-Common.

#### WabbitBot.Core
- **Critical Flatten:** Models/Common/ → Models/ (move Map/, Match/, Player/, Team/, User/ directly under Models/).
  - New path: Common/Models/Match/Match.cs → namespace `WabbitBot.Core.Common.Models.Match`.
  - Avoids `...Models.Common.Match`.
- Common/Events/ stay; but ensure no redundancy.
- Scrimmages/ScrimmageRating/ → Scrimmages/Rating/ (rename for clarity; namespace .Scrimmages.Rating).
- Tournaments/Data/Interface/ → Tournaments/Interfaces/ (standardize).
- Leaderboards/Data/Interface/ → Leaderboards/Interfaces/.
- Root Program.cs stays `WabbitBot.Core`.

#### WabbitBot.DiscBot
- Uncomment and standardize all namespaces.
- Merge DiscBot/ and DSharpPlus/ if overlap; else keep separate.
- Embeds/ under DSharpPlus/ → namespace `WabbitBot.DiscBot.DSharpPlus.Embeds`.
- Interactions/ under DSharpPlus/ stay.

#### WabbitBot.SourceGenerators
- No structural changes; namespaces already good.
- Update templates to dynamically infer or hardcode new target namespaces.

#### Other Projects
- No changes needed for Shared/Analyzers/Tests; minor moves if any root files.

### Post-Change Directory Example (Core)
```
src/WabbitBot.Core/
├── Common/
│   ├── BotCore/
│   │   ├── CoreEventBus.cs  # WabbitBot.Core.Common.BotCore
│   ├── Models/  # Flattened
│   │   ├── Match/
│   │   │   └── Match.cs  # WabbitBot.Core.Common.Models.Match
│   │   ├── Map/
│   │   │   └── Map.cs  # WabbitBot.Core.Common.Models.Map
│   │   └── ... (Player, Team, User, Leaderboard, Scrimmage, Tournament)
│   ├── Services/
│   │   └── Core/
│   │       └── CoreService.cs  # WabbitBot.Core.Common.Services.Core
│   └── ... (Commands, Config, Database, Events, Handlers, Interfaces)
├── Leaderboards/
│   ├── LeaderboardCore.cs  # WabbitBot.Core.Leaderboards
│   └── Interfaces/  # Moved from Data/Interface
└── Scrimmages/
    ├── ScrimmageCore.cs  # WabbitBot.Core.Scrimmages
    └── Rating/  # Renamed from ScrimmageRating
        └── RatingCalculator.cs  # WabbitBot.Core.Scrimmages.Rating
```

Estimated Changes: ~20 file/directory moves; no deletions.

## 3. Proposed Namespace Convention

- **Rule:** For a file at `<project_root>/<dir1>/<dir2>/File.cs`, namespace = `<ProjectName>.<dir1>.<dir2>`.
  - Root files: `<ProjectName>`.
  - Use PascalCase for dirs (already mostly true; rename if needed, e.g., scrimmages → Scrimmages).
- **Examples:**
  - WabbitBot.Common/Data/Service/DatabaseService.cs → `WabbitBot.Common.Data.Service`
  - WabbitBot.Core/Common/Models/Match/Match.cs → `WabbitBot.Core.Common.Models.Match`
  - WabbitBot.DiscBot/DSharpPlus/Commands/MapCommandsDiscord.cs → `WabbitBot.DiscBot.DSharpPlus.Commands`
  - Generated files: Dynamically set based on target (e.g., events in Core → `WabbitBot.Core.Common.Events`).
- **Exceptions:** None; enforce strictly. For partial classes, use consistent namespace across files.
- **Using Statements:** Prefer explicit full namespaces initially; optimize later with global usings if needed.

## 4. Big-Bang Execution Plan

Execute in a single session/branch to avoid incremental breaks. Use IDE (VS/Cursor) for bulk operations where possible (e.g., Find/Replace, Rename Namespace).

### Preparation (1-2 hours)
1. **Backup & Branch:**
   - `git stash` any uncommitted changes.
   - `git checkout -b refactor/namespaces-1to1`
   - Create full project zip backup.
   - Run `dotnet build` and `dotnet test` baseline.

2. **Inventory Files:**
   - Use script/tool to list all .cs files and current namespaces (e.g., grep or IDE search).
   - Document ~500 files; categorize by project.

3. **Update .csproj Files (Optional):**
   - Add `<RootNamespace>WabbitBot.Common</RootNamespace>` to each .csproj for default fallback (but explicit still needed).
   - No other changes.

### Phase 1: Directory Restructuring (2-3 hours)
1. **Move/Rename Directories/Files:**
   - Use `git mv` for all moves to preserve history.
   - Implement proposed changes (e.g., flatten Core/Models/Common → Models/).
   - Specific moves:
     - Core: mv Common/Models/Common/* Common/Models/
     - Core: mv Scrimmages/ScrimmageRating/ Scrimmages/Rating/
     - Common: mv Configuration.cs Configuration/Configuration.cs (create dir)
     - DiscBot: Ensure all subdirs consistent (no moves if minimal).
     - Delete empty dirs post-move.
   - Validate: Run `dotnet build` (expect errors due to paths).

2. **Handle Deprecated:**
   - Move any non-deprecated from src/deprecated/ if needed; otherwise ignore.

### Phase 2: Namespace Declarations Update (3-4 hours)
1. **Bulk Update Namespaces:**
   - For each project, use IDE \"Rename Namespace\" or Find/Replace:
     - Pattern: Search `namespace OldNs;` → `namespace NewNs;`
     - Ensure 1:1 based on new paths.
   - Examples:
     - Change `WabbitBot.Core.Common.Models` (incomplete) to full nested (e.g., `WabbitBot.Core.Common.Models.Match` for Match files).
     - Uncomment all in DiscBot.
     - Fix typos (e.g., ConfigurationCommands.cs to .Commands).
   - Partial classes: Align all to the primary file's namespace.
   - Generated files: Delete obj/generated/* pre-build; updates come in Phase 3.

2. **Root Files:**
   - Set to project name (e.g., Program.cs → `WabbitBot.Core`).

### Phase 3: Source Generators & Generated Code (1-2 hours)
1. **Update Templates:**
   - Edit files like EventTemplates.cs, DbContextGenerator.cs, CommandGenerator.cs:
     - Replace hardcoded namespaces (e.g., `WabbitBot.Core.Common.Events` → new if changed; most stay but verify).
     - Use dynamic inference where possible (e.g., from attribute metadata).
   - Specific:
     - EventBoundaryGenerator.cs: Update event class namespaces.
     - DbContextGenerator.cs: Target `WabbitBot.Core.Common.Database`.
     - CommandGenerator.cs: Target `WabbitBot.DiscBot.DSharpPlus.Commands`.

2. **Rebuild Generators:**
   - `dotnet build` to regenerate; verify output namespaces match new convention.

### Phase 4: Using Statements & Dependencies (4-5 hours)
1. **Global Search/Replace:**
   - Use IDE or tool (e.g., grep/sed) to find all `using OldNs;` → `using NewNs;`.
   - Cross-project: Update Core usings for Common changes; DiscBot for Core.
   - Events/Interfaces: Ensure IEvent, ICoreEventBus etc. resolve (e.g., update GlobalEventBus.cs references).
   - Analyzers/Tests: Update usings in test projects.

2. **Symbol Resolution:**
   - Fix any fully-qualified names (e.g., in comments, strings, attributes).
   - Event bus subscriptions: Update handler methods if namespaces affect type matching.

3. **Project References:**
   - Verify .csproj ProjectReferences; no changes expected, but check for any conditional includes.

### Phase 5: Validation & Testing (2-3 hours)
1. **Build & Lint:**
   - `dotnet clean && dotnet build` (fix any remaining errors).
   - Run `read_lints` or IDE diagnostics; address namespace-related warnings.
   - Ensure no circular dependencies introduced.

2. **Run Tests:**
   - `dotnet test` all projects.
   - Manual smoke tests: Start bot, trigger events/commands to verify runtime (e.g., match creation, leaderboard query).

3. **Generated Code Check:**
   - Inspect obj/generated/ for correct namespaces.
   - Rebuild after changes.

### Phase 6: Documentation & Cleanup (1 hour)
1. **Update Docs:**
   - Revise AGENTS.md, CONFIGURATION.md, docs/Architecture/* to reference new namespaces.
   - Add section to this MD on post-refactor notes.

2. **Commit & Review:**
   - `git add . && git commit -m \"refactor: align namespaces 1:1 with directory structure\"`
   - Create PR; self-review diffs.
   - Update .gitignore if new generated paths.

### Risks & Mitigations
- **Compilation Breaks:** Use incremental IDE fixes; rollback to backup if >50 errors.
- **Generated Code Mismatch:** Test generators in isolation first.
- **Event Bus Breaks:** Verify IEvent implementations pre/post.
- **IDE Support:** Use VS 2022+ with Roslyn; disable analyzers temporarily if noisy.
- **Time Overrun:** Prioritize Core/DiscBot (highest impact); defer minor test updates.
- **Data/Behavior:** No schema changes; focus on code structure.

### Estimated Total Effort: 13-20 hours
- Single developer session or paired.
- Post-execution: Monitor for 1 week; hotfix any regressions.

This plan ensures a thorough, one-shot refactor while minimizing disruption to the project's architecture and functionality.
