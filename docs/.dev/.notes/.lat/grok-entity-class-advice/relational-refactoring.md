# Domain-Wide Architectural & Entity Refactoring Plan

## Executive Summary

The current entity architecture across multiple domains exhibits architectural drift, including relational integrity gaps, fragile cross-domain dependencies, and violations of the project's "pure procedural" design pattern. These issues introduce risks of data anomalies, complicate logic, and impede maintainability. This proposal outlines a comprehensive, project-wide refactoring to normalize entity relationships, reorganize domains, and enforce a consistent, scalable architecture.

**Goal:** Refactor all data domains to implement robust entity relationships, a logical domain structure, and strict adherence to the project's architectural principles.
**Benefit:** Eliminates data integrity risks, simplifies queries, improves performance and maintainability, and establishes a clear, consistent architectural pattern across the entire codebase.

## Architectural Mandate: No Production Data or Backwards Compatibility

This application is in initial development with no production deployments. Consequently, **there are no concerns regarding data migration or backwards compatibility.** This refactoring should be executed as a "green-field" implementation, prioritizing the ideal architectural state.

## Current Problems

1.  **Architectural Incoherency:** The `Match` domain is structured as a top-level vertical slice, but functionally acts as a shared component for `Scrimmage` and `Tournament`, creating confusion.
2.  **Fragile Cross-Domain Data Dependencies:** Vertical slices query each other's data directly from the database, creating a hidden, brittle coupling to their internal schemas.
3.  **Violations of Pure Procedural Design:** Numerous entity classes contain behavioral logic (constructors, derived properties), violating the "dumb" entity principle.
4.  **Unclear Domain Grouping:** Entity classes do not explicitly declare their domain affiliation via a clear inheritance structure.
5.  **Disorganized Business Logic:** Logic within `EntityCore` classes is often scattered in nested static classes (`Factory`, `Accessors`).
6.  **Inefficient Service Instantiation:** `DatabaseService` instances are created ad-hoc, leading to scattered configuration and runtime overhead.

## Proposed Solution Overview

### Core Principles

1.  **Consistent Domain File Structure:** All domains, whether shared components (`Common`) or vertical slices (`Leaderboard`, etc.), will follow the same internal file structure.
    *   **Entity Definitions (`[Domain].cs`):** All entity classes for a single domain will be consolidated into one file (e.g., `Leaderboard.cs` will contain `Leaderboard`, `LeaderboardItem`, `Season`, and `SeasonGroup`).
    *   **Business Logic (`[Domain]Core.cs`):** All business logic for a domain will reside in a separate `[Domain]Core.cs` file. Logic will be moved out of entity definition files.
2.  **Domain Re-organization (`Match` to Common):** The `Match` domain, being a shared component, will be moved into the `Common` project to align its location with its architectural role.
3.  **Robust Entity Structure (Refined Option 3):** The entity architecture will be standardized to provide clear, queryable domain metadata without deep inheritance.
    *   **Abstract Base `Entity`:** A single `abstract class Entity` will be the base for all entities.
    *   **`Domain` Enum:** A `Domain` enum will be added to the base `Entity` class, allowing for powerful, database-level filtering by domain.
    *   **Marker Interfaces:** Lightweight marker interfaces (e.g., `ITeamEntity`, `ILeaderboardEntity`) will be used for domain-specific typing and compile-time checks.
4.  **Standardized Relational Modeling:** To ensure robust and efficient data access, all entity relationships will be modeled using a consistent, best-practice pattern.
    *   **Dual Properties for Navigation:** For every one-to-many or one-to-one relationship, the dependent entity will expose both a scalar foreign key property (e.g., `public Guid ParentId { get; set; }`) and a `virtual` navigation property (e.g., `public virtual Parent Parent { get; set; }`). This provides EF Core with the necessary information for efficient querying while offering developers convenient object graph traversal.
    *   **Collection Properties:** All collection-based navigation properties (the "many" side of a one-to-many) will be defined as `public virtual ICollection<T> { get; set; }`.
5.  **Standardized Cross-Domain Queries (Summary Models):** To eliminate brittle database dependencies, when one domain needs to read data from another, it will do so through stable, read-only "summary" models defined in `Common`.
6.  **Hybrid Logic for Shared Domains (DI Exception):** For shared components like `Match` that are consumed by multiple vertical slices, a hybrid logic pattern will be adopted. This is a deliberate, valuable exception to the strict no-DI policy.
    *   **Shared Base Handler (in Common):** A `static` handler class in `Common` will contain the reusable, core logic (e.g., validation, persistence) for the shared entity.
    *   **Slice-Specific Handlers:** Each vertical slice (`ScrimmageCore`, `TournamentCore`) will have its own handler method that calls the shared base handler and then executes its own unique, slice-specific logic.
7.  **Organizing Large Logic Files:** When a `[Domain]Core.cs` file becomes too large, it will be broken up using `partial class` definitions. The main file will contain the primary business logic, while partials will be organized by sub-domain or concern (e.g., `LeaderboardCore.Season.cs`).
8.  **Efficient and Centralized Data Service Access (`DbServiceFactory`):** A static `DbServiceFactory` will be introduced in `Common` to centralize `DatabaseService` configuration and instantiation. All `EntityCore` classes will interact with `DatabaseService` through its consistently `Result<T>`-returning API, as defined in the Foundational Service Refactoring Plan.

---

## Foundational & Infrastructure Refactoring

1.  **`Entity` Class and `Domain` Enum:** The base `Entity.cs` file in `Common` will be updated.
    ```csharp
    public enum Domain
    {
        Common,
        Leaderboard,
        Match,
        Scrimmage,
        Tournament,
        // ... etc. for all domains
    }

    public abstract class Entity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public abstract Domain Domain { get; } // Force entities to declare their domain
    }
    ```
2.  **Marker Interfaces:** A new file in `Common` will define all domain marker interfaces.
    ```csharp
    // WabbitBot.Common/Models/DomainMarkers.cs
    public interface ITeamEntity { }
    public interface ILeaderboardEntity { }
    public interface IMatchEntity { }
    // ... one marker interface per domain
    ```
3.  **`DbServiceFactory`:** A new static `DbServiceFactory` will be created in `Common` to provide configured instances of `DatabaseService<T>`.

---

## Cross-Domain Logic Handling (Hybrid Pattern)

This pattern will be implemented for the `Match` domain.

1.  **Shared Base Handler (in `MatchCore.cs`):**
    ```csharp
    // WabbitBot.Core/Common/Models/Match/MatchCore.cs
    public partial class MatchCore : CoreService
    {
        // ... existing logic ...
        public static class CompletionHandler
        {
            public static async Task<Result<Match>> ValidateAndPersistAsync(Match match, Guid winnerId, DatabaseService<Match> matchData)
            {
                if (match is null) return Result<Match>.Failure("Match not found.");
                
                match.WinnerId = winnerId;
                match.CompletedAt = DateTime.UtcNow;
                var updateResult = await matchData.UpdateAsync(match, DatabaseComponent.Repository);

                if (!updateResult.Success) return Result<Match>.Failure("Failed to persist match completion.");
                return Result<Match>.Success(match);
            }
        }
    }
    ```
2.  **Slice-Specific Handlers (in `ScrimmageCore.cs`, `TournamentCore.cs`):**
    ```csharp
    // WabbitBot.Core/Scrimmages/ScrimmageCore.cs
    public async Task<Result> HandleScrimMatchCompletedAsync(Guid matchId, Guid winnerId)
    {
        var match = await _matchData.GetByIdAsync(matchId, DatabaseComponent.Repository);
        
        // 1. Call Shared Handler
        var persistResult = await MatchCore.CompletionHandler.ValidateAndPersistAsync(match!, winnerId, _matchData);
        if (!persistResult.Success) return persistResult;

        // 2. Execute Slice-Specific Logic
        await ApplyScrimEloBonusAsync(persistResult.Data!, winnerId);

        // 3. Publish Slice-Specific Event
        await EventBus.PublishAsync(new ScrimMatchEndedEvent { ... });
        return Result.CreateSuccess();
    }
    ```
---

## Implementation Plan

1.  **Foundational Refactoring:**
    *   [✅] Complete all tasks outlined in the Foundational Service Refactoring Plan (Phase 1). This includes enhancing `CoreService` thread-safety, standardizing `DatabaseService` error handling to consistently return `Result<T>`, implementing testability hooks, consolidating archive support, and ensuring `EntityCore` classes retrieve `DbContext` instances from an `IDbContextFactory` instead of `CoreService.DbContext` (which will be removed).

2.  **Domain & File Re-organization:**
    *   [✅] Create the new `[Domain]Core.cs` files where needed (TeamCore, PlayerCore, UserCore created/updated; LeaderboardCore split into partials; ScrimmageCore and TournamentCore present).
    *   [✅] Move logic from entity files into their new, separate `Core` files (completed for Team, Player, User, Leaderboard, Tournament; continuing verification for any remaining computed props).
    *   [✅] Consolidate entity definitions per domain (Team, Match, Leaderboard already consolidated into single files; verified no stragglers to remove at this time).

3.  **Entity & Logic Refactoring:**
    *   [✅] Systematically update each entity to inherit from `Entity`, implement its marker, and declare its `Domain` (verified across major domains).
    *   [✅] Refactor entities to be "dumb" data containers (Player, User, Team, Leaderboard, Tournament updated; Match and Map already data-only).
    *   [ ] **Implement the dual FK/navigation property pattern for all relationships.**
    *   [ ] **Standardize all collection properties to `virtual ICollection<T>`.**
    *   [ ] **Remove all constructors from EntityCore classes, and replace with Create method if one doesn't exist already.**

4.  **Hybrid Handler Implementation:**
    *   [ ] Create the shared `MatchCore.CompletionHandler`.
    *   [ ] Refactor `ScrimmageCore` and `TournamentCore` to use the new hybrid pattern for handling match completion.

5.  **EF Core & Database Schema:**
    *   [ ] Delete all existing migrations and generate a single new one.

6.  **Application Logic Update:**
    *   [ ] Rewrite data access logic to use the new structures (in progress; many Core classes updated to use Result-returning `DatabaseService`).
    *   [ ] Implement stable "summary" models for all cross-domain data queries.

## Success Metrics

*   **Architectural Compliance:** All domains follow the consistent file structure. The `Match` domain is in `Common`. All entities declare their `Domain` and implement a marker interface. The hybrid handler pattern is correctly implemented.
*   **Code Quality:** `DatabaseService` configuration is centralized. Shared logic is reused effectively. Entity definitions and business logic are clearly separated.
*   **Validation:** The application builds successfully, and a new, clean database is created from a single initial migration.

---

## Appendix: Target File Structure
```
WabbitBot.Core
├── Common
│   └── Models
│       ├── Match                   // Shared component domain
│       │   ├── Match.cs            // Contains ALL Match-related entity definitions (Match, Game, etc.)
│       │   ├── MatchCore.cs        // Contains ALL Match-related business logic (MatchCore, GameCore)
│       │   └── Match.DbConfig.cs
│       ├── Team                    // Shared component domain
│       │   ├── Team.cs             // Contains ALL Team-related entity definitions (Team, Stats, etc.)
│       │   ├── TeamCore.cs         // Contains ALL Team-related business logic (TeamCore, StatsCore)
│       │   └── Team.DbConfig.cs
│       ├── Player                  // Shared component domain
│       │   ├── Player.cs
│       │   ├── PlayerCore.cs
│       │   └── Player.DbConfig.cs
│       ├── User                    // Shared component domain
│       │   ├── User.cs
│       │   ├── UserCore.cs
│       │   └── User.DbConfig.cs
│       │
│       ├── Leaderboard             // Vertical slice ENTITY DEFINITIONS ONLY
│       │   ├── Leaderboard.cs      // Contains ALL Leaderboard entities (Leaderboard, Season, etc.)
│       │   └── Leaderboard.DbConfig.cs
│       ├── Scrimmage               // Vertical slice ENTITY DEFINITIONS ONLY
│       │   ├── Scrimmage.cs
│       │   └── Scrimmage.DbConfig.cs
│       └── Tournament              // Vertical slice ENTITY DEFINITIONS ONLY
│           ├── Tournament.cs
│           └── Tournament.DbConfig.cs
│
├── Leaderboards                  // Vertical Slice LOGIC for Leaderboard feature
│   ├── LeaderboardCore.cs        // Main logic file
│   ├── LeaderboardCore.Season.cs // Example of partial class for organizing large logic files
│   └── LeaderboardEvents.cs
│
├── Matches                       // This directory will be REMOVED
│
├── Scrimmages                    // Vertical Slice LOGIC for Scrimmage feature
│   ├── ScrimmageCore.cs
│   └── ScrimmageEvents.cs
│
└── Tournaments                   // Vertical Slice LOGIC for Tournament feature
    ├── TournamentCore.cs
    └── TournamentEvents.cs
```

---

## Appendix: Target Entity Class Hierarchy

This diagram illustrates the complete entity class inheritance structure after the refactoring. All entities will inherit from the single `abstract class Entity` and implement their respective domain's marker interface for clear type identification.

```
[abstract class Entity]
    │
    ├─ (implements ICommonEntity)
    │   ├── class Team : Entity, ITeamEntity
    │   ├── class Stats : Entity, ITeamEntity
    │   ├── class TeamMember : Entity, ITeamEntity
    │   ├── class TeamVarietyStats : Entity, ITeamEntity
    │   │
    │   ├── class Match : Entity, IMatchEntity
    │   ├── class Game : Entity, IMatchEntity
    │   ├── class GameStateSnapshot : Entity, IMatchEntity
    │   ├── class MatchParticipant : Entity, IMatchEntity
    │   ├── class TeamOpponentEncounter : Entity, IMatchEntity
    │   ├── class MatchStateSnapshot : Entity, IMatchEntity
    │   │
    │   ├── class Player : Entity, IPlayerEntity
    │   │
    │   ├── class User : Entity, IUserEntity
    │   │
    │   └── class Map : Entity, IMapEntity
    │
    ├─ (implements ILeaderboardEntity)
    │   ├── class Leaderboard : Entity, ILeaderboardEntity
    │   ├── class LeaderboardItem : Entity, ILeaderboardEntity
    │   ├── class Season : Entity, ILeaderboardEntity
    │   ├── class SeasonGroup : Entity, ILeaderboardEntity
    │   └── class SeasonConfig : Entity, ILeaderboardEntity
    │
    ├─ (implements IScrimmageEntity)
    │   ├── class Scrimmage : Entity, IScrimmageEntity
    │   └── class ProvenPotentialRecord : Entity, IScrimmageEntity
    │
    └─ (implements ITournamentEntity)
        ├── class Tournament : Entity, ITournamentEntity
        └── class TournamentStateSnapshot : Entity, ITournamentEntity

```
---

## Appendix: Example Relational Implementation

This example demonstrates the target pattern for the `Leaderboard` -> `LeaderboardItem` relationship.

#### 1. Entity Definitions (`Leaderboard.cs`)
```csharp
public class Leaderboard : Entity, ILeaderboardEntity
{
    // ... other properties
    
    // Collection navigation property
    public virtual ICollection<LeaderboardItem> Rankings { get; set; } = new List<LeaderboardItem>();
    
    public override Domain Domain => Domain.Leaderboard;
}

public class LeaderboardItem : Entity, ILeaderboardEntity
{
    // ... other properties
    
    // Scalar Foreign Key property
    public Guid LeaderboardId { get; set; }
    
    // Reference Navigation property
    public virtual Leaderboard Leaderboard { get; set; } = null!;
    
    public override Domain Domain => Domain.Leaderboard;
}
```

#### 2. EF Core Configuration (`WabbitBotDbContext.Leaderboard.cs`)
```csharp
modelBuilder.Entity<Leaderboard>()
    .HasMany(l => l.Rankings)
    .WithOne(li => li.Leaderboard) // Specifies the inverse navigation property
    .HasForeignKey(li => li.LeaderboardId); // Specifies the FK property
```

---

## Progress Update

We have successfully completed all tasks outlined in the **Foundational Service Refactoring Plan**. This initial phase has established a robust, thread-safe, and consistently error-handling core infrastructure, providing a solid foundation for the remaining refactoring efforts.

Specifically, the following key foundational tasks have been completed:

*   **CoreService Enhancements:** `CoreService.cs` has been updated to use `Lazy<T>` for thread-safe initialization of `EventBus`, `ErrorHandler`, and `IDbContextFactory`. Legacy `CoreService.DbContext` references have been removed.
*   **DatabaseService Standardization:** `IDatabaseService.cs` and `DatabaseService.cs` have been refactored to consistently return `Result<T>` or `Result<T?>` for all operations, replacing `ArgumentException` throws with `Result.Failure` and ensuring sanitized error messages.
*   **Testability Hooks:** `SetTestServices` and `SetTestDbContextFactory` internal static methods have been added to `CoreService.cs` to facilitate isolated testing.
*   **EntityCore Updates:** All identified `EntityCore` classes (`SeasonCore`, `ScrimmageCore`, `MatchCore`, `TournamentCore`, `LeaderboardCore`, and the merged `TeamCore`) have been modified to:
    *   Retrieve `DbContext` instances via their constructors, preparing for `DbServiceFactory` integration.
    *   Consistently handle `Result<T>` return types from `DatabaseService` calls.
    *   All compilation errors and linter warnings in these `EntityCore` classes have been resolved.

This completes Phase 1 of our overall refactoring. We are now ready to proceed with the next steps of the **Domain-Wide Architectural & Entity Refactoring Plan**.
