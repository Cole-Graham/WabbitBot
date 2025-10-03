# Foundational Service Refactoring Plan

## Executive Summary

The current implementation of core services, particularly `CoreService` and `DatabaseService`, exhibits several foundational issues. These include reliance on mutable global static state, inconsistent error handling, and challenges in testability. These problems introduce risks of race conditions, unpredictable behavior, and difficulties in maintaining code quality as the application scales.

**Goal:** Refactor core services to ensure thread-safe initialization, consistent error propagation, improved testability, and overall architectural robustness.
**Benefit:** Eliminates risks associated with global state, standardizes error handling, simplifies testing, and provides a stable foundation for all domain-specific logic.

## Architectural Mandate: No Production Data or Backwards Compatibility

This application is in initial development with no production deployments. Consequently, **there are no concerns regarding data migration or backwards compatibility.** This refactoring should be executed as a "green-field" implementation, prioritizing the ideal architectural state.

## Current Problems

1.  **Global Static State in `CoreService` (Service Locator Pitfalls):** The static `CoreService.EventBus`, `ErrorHandler`, and `DbContext` are mutable global state. In a multi-threaded environment (like a Discord bot), this creates risks of uninitialized access, `InvalidOperationException`s, or race conditions during concurrent initialization or usage. The current initialization lacks thread-safety.
2.  **Inconsistent Error Handling and Result Propagation:** While `Result<T>` is used, `DatabaseService` methods (e.g., `GetByIdAsync`) inconsistently throw `ArgumentException` on null results instead of returning a `Result.Failure`. In `CoreService`, exceptions bubble up to `ErrorHandler.CaptureAsync` but return `Result.Failure(ex.Message)`, potentially exposing internal details.
3.  **Testing and Scalability Edges:** The reliance on static `CoreService` dependencies makes unit testing challenging, requiring hacks or indirect manipulation to inject test doubles for `EventBus`, `ErrorHandler`, or `DbContext`.

## Proposed Solution Overview

### Core Principles

1.  **Thread-Safe Static Initialization:** All static members of `CoreService` will be initialized in a thread-safe manner, ensuring consistent and predictable behavior in concurrent environments.
2.  **Consistent `Result<T>` Usage:** `DatabaseService` methods will be refactored to consistently return `Result<T>` for all operations, including `Get` methods, to unify error propagation and avoid unexpected exceptions.
3.  **Sanitized Error Messages:** Error messages returned by `Result.Failure` will be sanitized to prevent exposure of internal exception details to higher layers or end-users.
4.  **Testability Improvements:** Mechanisms will be introduced to facilitate the injection of test doubles for static dependencies of `CoreService`, improving the testability of `EntityCore` classes.
5.  **`IDbContextFactory` for Context Management:** `DbContext` instances will be managed via `IDbContextFactory` (wrapped in a static factory) to ensure short-lived, scoped contexts, preventing leaks and improving multi-threading stability.

---

## Implementation Plan

1.  **Enhance `CoreService` Thread-Safety and `DbContext` Management:**
    *   [✅] Modify `CoreService.cs` to implement thread-safe, lazy initialization for `EventBus`, `ErrorHandler`, and `DbContext` using `Lazy<T>` or a `lock` mechanism.
    *   [✅] Introduce an `IDbContextFactory` (e.g., `WabbitBotDbContextFactory`) to manage `DbContext` instance creation. The `CoreService.DbContext` static property will be removed, and `EntityCore` classes will acquire `DbContext` instances via this factory.

2.  **Standardize `DatabaseService` Error Handling:**
    *   [✅] Refactor `IDatabaseService.cs` and `DatabaseService.cs` methods (especially `GetByIdAsync`, `GetByStringIdAsync`, `GetByNameAsync`) to always return `Result<TEntity?>` instead of throwing `ArgumentException` on null results.
    *   [✅] Update all callers of `DatabaseService` methods to correctly handle the `Result<TEntity?>` return type, checking `Result.Success` and `Result.Data is not null`.
    *   [✅] Ensure `Result.Failure` messages are sanitized to hide internal exception details.

3.  **Implement Testability Hooks for `CoreService`:**
    *   [✅] Add `internal set` accessors or a dedicated static `SetTestServices` method to `CoreService` for `EventBus` and `ErrorHandler` properties, allowing unit tests to inject mock implementations.
    *   [✅] Introduce a static `SetTestDbContextFactory` method in `CoreService` (or a dedicated `DbContextFactory` class) for injecting test `DbContext` factories.

4.  **Update `EntityCore` Classes:**
    *   [✅] Modify all existing `EntityCore` classes (e.g., `MatchCore`, `TeamCore`, `LeaderboardCore`) to retrieve `DbContext` instances from the `IDbContextFactory` instead of relying on the static `CoreService.DbContext` property.
    *   [✅] Update `EntityCore` logic to handle the new `Result<TEntity?>` return types from `DatabaseService` consistently.

## Success Metrics

*   **Thread-Safety:** `CoreService` initialization is robust and thread-safe.
*   **Error Consistency:** `DatabaseService` consistently returns `Result<T>` with sanitized error messages, eliminating `ArgumentException` for expected null results.
*   **Testability:** `CoreService` dependencies can be easily mocked or replaced in unit tests, allowing for isolated testing of `EntityCore` logic.
*   **DbContext Management:** `DbContext` instances are managed via a factory, promoting short-lived contexts and reducing memory leaks.
*   **Architectural Alignment:** The core service layer is stable, predictable, and fully aligned with the "no-DI, pure procedural" architecture with controlled testability hooks.
