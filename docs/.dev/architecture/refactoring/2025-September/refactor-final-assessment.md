# Refactor Progress Assessment

## Design Intent
- The refactor targets a JSONB-first EF Core stack, unified per-entity services, and no runtime dependency
  injection (see `refactor-steps.md`, Step 6.* and onward).
- Step 6.5 introduces loose coupling between application and schema versioning, using compatibility checks,
  feature flags, and drift monitoring (`refactor-step6.5.md`).
- Step 7 consolidates domain logic into a single partial `CoreService` orchestrator, built on event-driven
  handoffs (`refactor-step7.md`).

## Current Progress
- Steps 1 through 6.4 are mostly implemented: entities inherit `Entity`, DbContext configures JSONB indexes,
  and `DatabaseService<TEntity>` exists (`src/WabbitBot.Common/Data/Service/DatabaseService.cs`).
- Step 6.4 Phase 3 is incomplete; the plan notes this (`refactor-steps.md`, Step 6.4f), and code still
  references `DataServiceManager` (e.g., `src/WabbitBot.Core/Common/Services/TeamService.cs`).
- Step 6.5 is only in planning: the log is empty, and helper classes (`ApplicationInfo`, `SchemaVersionTracker`,
  etc.) have not been created.
- Step 7 has not started in code: `CoreService` does not inherit `BackgroundService` and remains a stub,
  with partial files as placeholders.
- Legacy `TeamService`, `MatchService`, and similar classes remain, violating the new "service" naming
  restrictions and indicating pending migrations.

## Risks & Gaps
- Documentation contains corrupted characters, especially in `refactor-step6.5.md`, which may confuse future
  edits or generators.
- Step 6.4 documentation overstates progress by claiming `DataServiceManager` removal, but production code
  still depends on it.
- Direct-instantiation policy is only partially enforced: `Program.cs` still wires dependencies through
  static singletons.
- `CoreService.Database` uses null-forgiving accessors and hard-coded column arrays; this is temporary
  scaffolding and should be replaced with configuration objects.

## Recommended Next Actions
1. Finish Step 6.4 Phase 3: replace all `DataServiceManager` usage with `DatabaseService<T>` accessors and
   remove the legacy manager.
2. Implement Step 6.5 artifacts (`ApplicationInfo`, `SchemaVersionTracker`, feature flagging, compatibility
   tests) and log the work in `refactoring-logs/step6.5-log.md`.
3. Execute Step 7: make `CoreService` inherit `BackgroundService`, flesh out the partial files, migrate
   callers to it, and eliminate surplus `*Service` classes.
4. Clean corrupted characters in refactor documentation files to ensure clarity for future sessions.
