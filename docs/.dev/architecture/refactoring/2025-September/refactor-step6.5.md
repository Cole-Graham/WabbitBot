#### Step 6.5: Legacy Closure and Gap Remediation - NEW

### Current State Snapshot
- `deprecated/` directory isolates legacy service implementations so they no longer compile into active builds
- Several features still call `DataServiceManager` and other DI-era utilities instead of `DatabaseService<T>`
- Error handling remains inconsistent, forcing repeated try/catch and logging logic across services

### Refactor Goals
1. Establish a unified error handling architecture that mirrors the DatabaseService pattern and removes DRY
   violations
2. Finish the migration from DataServiceManager to DatabaseService-based access across active code paths
3. Remove residual runtime wiring that still targets deprecated assemblies or DI containers
4. Align the automated test suite with the modernized architecture while retiring legacy-specific tests

### Detailed Implementation Plan

#### 6.5a. Error Handling Architecture Blueprint
- Analyze `DatabaseService<TEntity>`: a single interface (`IDatabaseService<TEntity>`) routes operations to
  Repository, Cache, or Archive components via the `DatabaseComponent` switch
- Apply the same pattern to error handling by introducing `IErrorService` (or a domain-specific variant)
  that accepts an `ErrorComponent` enum to unify logging, notification, and recovery behaviors:
```csharp
public async Task HandleAsync(ErrorContext context, ErrorComponent component)
{
    return component switch
    {
        ErrorComponent.Logging => await LogAsync(context),
        ErrorComponent.Notification => await NotifyAsync(context),
        ErrorComponent.Recovery => await RecoverAsync(context),
        _ => throw new ArgumentException($"Unsupported component: {component}", nameof(component))
    };
}
```
- Key building blocks:
  - `ErrorContext`: immutable data describing the failure (operation, severity, user impact, correlation id)
  - `ErrorComponent` enum: `Logging`, `Notification`, `Telemetry`, `Recovery`, `Audit`
  - Partial class layout (`ErrorService.Logging.cs`, `ErrorService.Notification.cs`, etc.) mirroring the
    DatabaseService organization for maintainability
  - `IErrorPolicy` abstractions allowing per-feature overrides without duplicating plumbing
- Layering decision: Keep Global/Core/DiscBot handlers as thin facades that enrich `ErrorContext` and delegate
  to the single unified `IErrorService`. Global handles cross-boundary/unhandled faults, Core adds domain context,
  and DiscBot adds Discord context. No duplicate logic in project-level handlers; all routing/policies live in
  `ErrorService` partials.
- Implementation tasks:
  1. Define `IErrorService` interface with composable methods: `HandleAsync`, `CaptureAsync`, `EscalateAsync`
  2. Create base `ErrorService` class that injects required collaborators (log writers, alert senders) via
     constructor parameters rather than runtime DI
  3. Move shared logic from existing error handlers (`CoreErrorHandler`, `GlobalErrorHandler`, others) into
     partials so those handlers delegate to the new service instead of reimplementing flows
  4. Update emitter code (CoreService, DiscBot, background jobs) to build `ErrorContext` objects and call the
     service instead of performing ad-hoc logging or notifications
  5. Document extension points (custom policies, throttling rules) so future features can plug in without
     violating DRY principles

#### 6.5b. Complete DatabaseService Migration (Step 6.4f Follow-up)
- Replace DataServiceManager usages with CoreService DatabaseService accessors:
```csharp
// OLD
var team = DataServiceManager.TeamRepository.GetByIdAsync(teamId);

// NEW
var team = await CoreService._teamData.GetByIdAsync(teamId, DatabaseComponent.Repository);
```
- Remove obsolete repository/cache/archive interfaces from active projects once replacements compile

#### 6.5c. Wiring and Event Bus Cleanup
- Search for DI container registrations or runtime factories referencing deprecated assemblies
- Convert remaining consumers to direct instantiation or event publisher patterns (per AGENTS.md guardrails)
- Ensure event contracts only share primitive payloads so the bus stays decoupled from feature classes

#### 6.5d. Test Suite Alignment
- Update unit/integration tests to target CoreService + DatabaseService workflows
- Delete or quarantine tests bound to deprecated services; log removals with rationale
- Add regression coverage for the new error handling pipeline to prove consistent behavior

#### 6.5e. Manual EF/DbConfig Cleanup
- Remove any manual EF configuration files and `.DbConfig.cs` remnants (Option A is authoritative)
- Ensure only generator outputs and unified `DatabaseService` paths are referenced

#### 6.5f. SQLite Removal
- Remove SQLite-related fields and logic from `DatabaseSettings`
- Purge any SQLite-specific configuration, docs references, and helper scripts
- Verify appsettings and environment variables reference PostgreSQL exclusively

### Deliverables Checklist
- `refactoring-logs/step6.5-log.md` updated with error architecture notes and migration status
- Implementation sketch or prototype of the new `ErrorService` (interface, enum, sample partials)
- New or updated tests validating the DatabaseService migration and error handling flows

### Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Inconsistent adoption of the new error service | Provide integration examples and update AGENTS.md guidance |

### Success Criteria
- Centralized error handling service adopted by CoreService
- Build + tests pass using only the refactored architecture
- Documentation reflects the modern structure without pointing to removed services
