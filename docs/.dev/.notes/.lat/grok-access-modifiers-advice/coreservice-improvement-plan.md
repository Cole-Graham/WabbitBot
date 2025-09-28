## CoreService Improvement Plan

### Scope and Intent
- Objective: simplify and harden `CoreService` as the static, cross-cutting infrastructure hub for Core slice code.
- Non-goals: backward compatibility, legacy behavior preservation, or live data safety; no timelines included.
- Constraints: no runtime DI; retain event-bus-first, vertical slice architecture; preserve source generator patterns.

### Current Observations
- `CoreService` exposes static service-locator wiring (`EventBus`, `ErrorHandler`, `DbContextFactory`) and a one-time
  initializer. This aligns with our "no DI" rule and event-driven design.
- `CoreService` declares an abstract method while not being marked `abstract`. This is invalid and should be removed or
  the type made abstract. We prefer removing abstract members from `CoreService` entirely.
- `CoreService.Database` partial holds instance fields for `DatabaseService<T>` that rely on a private initializer and
  null-forgiving accessors. This is error-prone and mixes concerns into an instance shape we don't want.
- Cores (e.g., `MatchCore`, `TeamCore`) inherit `CoreService` but only use static members; inheritance is unnecessary.
- Partials like `Factory`, `Accessors`, `Validation` are cohesive; they can remain, ideally static where appropriate.
- Entity definitions have been reorganized: Match, Game, and related entities are now consolidated under `Common` domain (all inherit `IMatchEntity`), and DbContext files are organized by domain (Common, Leaderboard, Scrimmage, Tournament). `WabbitBotDbContext.Game.cs` should be deleted and folded into `WabbitBotDbContext.Match.cs`. DbContext column configurations are out of date due to entity changes.

### Design Principles (Reasoning)
- Keep `CoreService` static-only to clearly signal “infrastructure entry points,” not “base class for domain logic.”
- Favor lazy, typed accessors over nullable instance fields to eliminate null-forgiving and ordering assumptions.
- Shift enforcement of domain contracts to per-core interfaces (`ICore`, `IMatchCore`, `ITeamCore`) rather than an
  abstract `CoreService`. This avoids inheritance coupling and fits vertical slices and source generators.
- Provide small, reliable helpers for DB context acquisition, event publishing, and error capture to reduce repetition.

### Goals
- Make `CoreService` static-only and responsibility-focused:
  - Static initialization of global wiring.
  - Lazy, typed `DatabaseService<T>` accessors with no null-forgiving.
  - Utilities for DB context lifecycle, event publish, standardized error capture.
- Remove inheritance on cores; keep access to `CoreService` via static members.
- Introduce interfaces for core contracts; keep default behaviors out of interfaces when they need state.

---

## Plan

### 1. Clarify and Lock Down `CoreService` Shape
- Convert `CoreService` to `public static partial class CoreService`.
  - Apply `static` to all partial declarations.
  - Remove all instance fields/methods.
- Remove `abstract` members from `CoreService`. Validation contracts move to per-core interfaces.
- Keep and harden `InitializeServices(ICoreEventBus, IErrorService, IDbContextFactory<WabbitBotDbContext>)`:
  - Validate non-null args; keep `Lazy<T>` with `ExecutionAndPublication`.
  - Expose read-only static properties: `EventBus`, `ErrorHandler`, `DbContextFactory`.

### 2. Replace Instance `DatabaseService<T>` Fields with Typed Lazy Accessors
- Remove per-entity instance fields (`_playerData`, `_teamData`, …) and private `InitializeDatabaseServices()` from `CoreService.Database.cs`.
- Repurpose `CoreService.Database.cs` as the partial file for all static database-related code:
  - Introduce static, typed, lazy accessors:
    - `public static DatabaseService<Player> Players { get; }` initialized via `Lazy<DatabaseService<Player>>`.
    - Same for `Team`, `Game`, `User`, `Map`, and any other active entities.
  - Construction rules:
    - Define tables/columns once in these accessors (centralized, explicit).
    - Do not permit null-forgiving; everything lazily initializes on first use.
- Optionally add `CoreService.Database.For<TEntity>()` if a generic path is desired later; start explicit.

### 3. Provide Safe DB Context Utilities
- Add static helpers:
  - `Task WithDbContext(Func<WabbitBotDbContext, Task> work)`
  - `Task<T> WithDbContext<T>(Func<WabbitBotDbContext, Task<T>> work)`
- Ensure acquisition from `DbContextFactory`, `await using` lifecycle, and standardized exception capture via
  `ErrorHandler`.

### 4. Standardize Event Publishing and Error Handling Helpers
- Add:
  - `Task PublishAsync<TEvent>(TEvent evt)` as pass-through to `EventBus.PublishAsync` with basic null checks.
  - `Task<Result> TryAsync(Func<Task> op, string operationName)` and
    `Task<Result<T>> TryAsync<T>(Func<Task<T>> op, string operationName)`.
- Goals: reduce repeated try/catch blocks across cores and ensure consistent error messages and logging context.

### 5. Detach Cores from Inheritance; Introduce Interfaces
- Remove `: CoreService` from core classes (e.g., `MatchCore`, `TeamCore`).
- Define minimal contracts:
  - `ICore` (optional marker + common signatures like `InitializeAsync`, `ValidateAsync` if truly cross-cutting).
  - `IMatchCore` exposing match operations currently public (e.g., `StartMatchAsync`, `CompleteMatchAsync`, …).
  - `ITeamCore` exposing team operations (e.g., `AddPlayer`, `RemovePlayer`, `UpdateRating`, …).
- Implement interfaces on existing concrete cores. Keep partials (`Factory`, `Accessors`, `Validation`) as-is.
  - Where nested partials only contain static members, declare them `static partial` to clarify intent.

### 6. Migrate Per-Core Data Access to `CoreService` Accessors
- Replace private per-core `DatabaseService<T>` fields with direct use of `CoreService` accessors:
  - Example: `_teamData.UpdateAsync(...)` → `CoreService.Teams.UpdateAsync(...)`.
  - Example: `_gameData.CreateAsync(...)` → `CoreService.Games.CreateAsync(...)`.
- Remove constructors or dormant fields that previously tried to hold `DatabaseService<T>` instances.
- Eliminate null-forgiving operators introduced by the old pattern.

### 7. Validation and Result Patterns
- Keep domain validations in core partials (`Validation`), not in `CoreService`.
- Where common validation emerges (e.g., “entity exists” guards), use small static helpers (extension methods or
  plain static methods) that take IDs and use `CoreService` accessors.
- Maintain the `Result` pattern and unify error text via `TryAsync` helpers.

### 8. Source Generator and Event Bus Alignment
- Ensure event publishing sites use `CoreService.PublishAsync(...)` wrappers (thin pass-through) to improve consistency.
- Keep event payloads primitive/ID-centric to respect bus isolation.
- Confirm generated code for event bindings remains compatible (no runtime DI).

### 9. Testing Hooks
- Preserve existing `SetTestServices(...)` shape as static-only test hooks:
  - Permit swapping `EventBus`, `ErrorHandler`, and `DbContextFactory` for tests.
- Add internal test hooks for typed database accessors if required (e.g., factory delegates for `DatabaseService<T>`).

### 10. DbContext Reorganization and Entity Alignment
- Organize `WabbitBotDbContext` partial files by domain: `Common/`, `Leaderboard/`, `Scrimmage/`, `Tournament/`.
- Delete `WabbitBotDbContext.Game.cs` and fold its configurations into `WabbitBotDbContext.Match.cs` (since Game entities are now child-entities of Match under `IMatchEntity`).
- Update all DbContext configurations to match the new consolidated entity definitions (e.g., Match.cs now includes all Match and Game entities). Ensure column mappings, relationships, and indexes are current.

### 11. Cleanup and Dead Code Removal
- Remove any now-unused instance fields, constructors, and private initializers across partials.
- Delete legacy comments about null-forgiving "yikes!"—the new lazy pattern removes the need.
- Ensure all cores compile without inheriting `CoreService`.

---

## Acceptance Criteria
- `CoreService` is `static partial` across all declarations; no instance members remain.
- No abstract or virtual members in `CoreService`; domain contracts live in interfaces implemented by cores.
- All `DatabaseService<T>` access is via `CoreService` static, typed, lazy accessors; no null-forgiving operators.
- Cores no longer inherit `CoreService` but can call `CoreService.*` statically.
- Standard helpers exist for DB context usage, event publishing, and error capture; repeated try/catch reduced.
- Unit/integration tests can replace global services via static test hooks without runtime DI.
- `WabbitBotDbContext` partial files are organized by domain (Common/, Leaderboard/, Scrimmage/, Tournament/).
- `WabbitBotDbContext.Game.cs` is deleted and configurations folded into `WabbitBotDbContext.Match.cs`.
- DbContext configurations are updated to match consolidated entity definitions (all Match/Game entities under IMatchEntity).

## Core Implementation Checklist

### ✅ Completed
- [x] MatchCore (`src\WabbitBot.Core\Common\Models\Common\Match\MatchCore.cs`) - Detached from inheritance, implements IMatchCore, static partials, CoreService accessors
- [x] TeamCore (`src\WabbitBot.Core\Common\Models\Common\Team\TeamCore.cs`) - Detached from inheritance, implements ITeamCore, static partials, CoreService accessors

### ⏳ Remaining
- [x] MapCore (`src\WabbitBot.Core\Common\Models\Common\Map\MapCore.cs`) - Create IMapCore interface, detach from inheritance, implement interface, static partials, CoreService accessors
- [x] PlayerCore (`src\WabbitBot.Core\Common\Models\Common\Player\PlayerCore.cs`) - Create IPlayerCore interface, detach from inheritance, implement interface, static partials, CoreService accessors
- [x] UserCore (`src\WabbitBot.Core\Common\Models\Common\User\UserCore.cs`) - Create IUserCore interface, detach from inheritance, implement interface, static partials, CoreService accessors
- [x] LeaderboardCore (`src\WabbitBot.Core\Leaderboards\LeaderboardCore.cs`) - Create ILeaderboardCore interface, detach from inheritance, implement interface, static partials, CoreService accessors
- [x] LeaderboardCore.Season (`src\WabbitBot.Core\Leaderboards\LeaderboardCore.Season.cs`) - Ensure partial class is static, update accessors
- [x] ScrimmageCore (`src\WabbitBot.Core\Scrimmages\ScrimmageCore.cs`) - Create IScrimmageCore interface, detach from inheritance, implement interface, static partials, CoreService accessors
- [x] ScrimmageCore.PPR (`src\WabbitBot.Core\Scrimmages\ScrimmageCore.PPR.cs`) - Ensure partial class is static, update accessors
- [x] TournamentCore (`src\WabbitBot.Core\Tournaments\TournamentCore.cs`) - Create ITournamentCore interface, detach from inheritance, implement interface, static partials, CoreService accessors

## Core Architecture Polish Checklist

### ✅ Completed
- [x] CoreService architecture - Static-only with lazy accessors, helpers, and interfaces

### ✅ Completed Polish Tasks
- [x] CoreService.TrySync overload - Added `Task<Result> TrySync(Action op, string operationName)` for synchronous operations
- [x] CoreService.WithDbContext non-generic overload - Added `Task WithDbContext(Action<WabbitBotDbContext> work)` for fire-and-forget operations
- [x] Interface XML documentation - All interface members already have XML docs for better IntelliSense
- [x] MapCore interface - IMapCore interface members have XML docs
- [x] PlayerCore interface - IPlayerCore interface members have XML docs
- [x] UserCore interface - IUserCore interface members have XML docs
- [x] LeaderboardCore interface - ILeaderboardCore interface members have XML docs
- [x] ScrimmageCore interface - IScrimmageCore interface members have XML docs
- [x] TournamentCore interface - ITournamentCore interface members have XML docs

## Out of Scope (Explicit)
- Runtime DI, container registration, or lifetime management.
- Backward compatibility shims or dual-path behavior.
- Cross-project refactors outside Core unless strictly required by the above changes.