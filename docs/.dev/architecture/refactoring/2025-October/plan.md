# 2025-October Architecture Plan — Event System Refinement & DiscBot Refactor (Development)

This plan finalizes two remaining architectural pillars:
- Event system refinement across buses, handlers, and source generation
- DiscBot project refactor and organization around `DSharpPlus 5.0` and Commands

Mode: Development (BIG-BANG; no legacy/back-compat; no migration steps; test steps omitted)

## Body Overview

### Goals
- Solidify event roles, boundaries, and source-generated wiring using `EventGenerator` (class) and `EventTrigger` (method) to ensure consistent, testable, decoupled communication.
- Restructure `WabbitBot.DiscBot` to mirror Core layering: App flows (library-agnostic) and DSharpPlus adapters/renderers (library-specific), and enforce `DSharpPlus.Commands` only.

### Non-Goals
- Runtime dependency injection (explicitly avoided by project rules).
- Database CRUD through events (events are communication only).

### Scope
- Core and DiscBot internal buses and their forwarding to Global bus.
- Thin orchestration flows vs. Core services/commands (handlers remain thin; business logic lives in Core).
- DiscBot layering: App flows under `DiscBot/App/...` and DSharpPlus commands/interactions/renderers/component-models under `DSharpPlus/...`.
- Component models organized by domain in `DSharpPlus/ComponentModels/` (`ScrimmageComponents.cs`, `MatchComponents.cs`, `GameComponents.cs`).
- Primary UI pattern: Containers (`DiscordContainerComponent`) for modern, rich displays; embeds (`DiscordEmbed`) reserved for future simple interaction responses.
- Source generation touchpoints: `EventGenerator` + `EventTrigger` with support for dual-publish (`targets: Both`).

### Constraints and Rules
- Keep vertical slice boundaries intact; use Global/Core/DiscBot buses for cross-boundary communication only.
- Flows orchestrate; Core commands/services execute domain logic (no CRUD through events).
- DiscBot App flows must not call DSharpPlus; all Discord API calls live under `src/WabbitBot.DiscBot/DSharpPlus/` (or `DiscordBot.cs`) via renderers/adapters.
- Component models (containers and embeds) are POCO classes organized by domain in `DSharpPlus/ComponentModels/` files; do not create separate files per model.
- Use `DSharpPlus.Commands`; do not use `CommandsNext` or `SlashCommands`.
- Avoid runtime DI; prefer static accessors or factories consistent with existing code.

### Result vs Event Usage Guidelines

**Purpose**: Clarify when to use `Result<T>`/`Result` pattern vs Events to avoid confusion and maintain consistent architecture.

#### Use Result<T> or Result When:
1. ✅ Calling a method within the same project boundary (Core → Core, DiscBot → DiscBot)
2. ✅ You need immediate success/failure feedback in the calling code
3. ✅ The caller needs to make decisions based on the outcome
4. ✅ The operation is synchronous or async but needs a return value
5. ✅ **Example**: `var result = await MatchCore.CreateMatchAsync(...); if (!result.Success) { return result; }`

#### Use Events (Fire-and-Forget) When:
1. ✅ Notifying other components that something happened (past tense facts)
2. ✅ Cross-boundary communication (Core ↔ DiscBot)
3. ✅ One-to-many communication (multiple subscribers need to know)
4. ✅ Decoupling is more important than immediate feedback
5. ✅ The caller doesn't need to wait for or react to the outcome
6. ✅ **Example**: `await CoreEventBus.PublishAsync(new MatchCompletedEvent(matchId, winnerId))`

#### Use Request-Response Events When:
1. ✅ Cross-boundary query where direct reference isn't possible (DiscBot needs Core data)
2. ✅ You need data from another boundary and can't use direct method calls
3. ⚠️ **AVOID** for intra-boundary calls (use direct method calls with Result instead)
4. ⚠️ **AVOID** as a general pattern (prefer direct calls when possible)
5. ✅ **Example**: `var response = await EventBus.RequestAsync<AssetResolveRequested, AssetResolved>(request)` (only for cross-boundary queries)

#### Hybrid Pattern (Result + Event):
Many operations should do **BOTH** - return Result for the immediate caller AND publish events for interested subscribers:

```csharp
public async Task<Result> CompleteMatchAsync(Guid matchId, Guid winnerId)
{
    // Perform the operation
    var updateResult = await UpdateMatchInDatabase(matchId, winnerId);
    
    if (!updateResult.Success)
        return Result.Failure("Failed to update match");
    
    // Success: Publish event for subscribers (leaderboards, notifications, etc.)
    await CoreService.PublishAsync(new MatchCompletedEvent(matchId, winnerId));
    
    // AND return Result for immediate caller feedback
    return Result.CreateSuccess();
}
```

**When to use Hybrid**:
- ✅ Core business operations that others need to know about
- ✅ State changes that trigger downstream processing
- ✅ Operations where both immediate feedback and broadcast are needed

**When NOT to use Hybrid**:
- ❌ Simple queries (just return Result<T>)
- ❌ Pure calculations (just return Result<T>)
- ❌ Operations that don't affect system state

#### Renderer/Handler Return Values:
Renderers and Handlers should follow this pattern:

```csharp
// Renderers: Return Result for local error handling
public async Task<Result> CreateMatchThreadAsync(Guid matchId)
{
    try
    {
        var channel = await GetChannelAsync();
        var thread = await channel.CreateThreadAsync(...);
        
        // Publish confirmation event for subscribers
        await DiscBotService.PublishAsync(new MatchThreadCreatedEvent(matchId, thread.Id));
        
        // Return success to caller
        return Result.CreateSuccess();
    }
    catch (Exception ex)
    {
        await DiscBotService.ErrorHandler.CaptureAsync(ex, ...);
        return Result.Failure($"Failed to create thread: {ex.Message}");
    }
}
```

- ✅ Renderers return `Task<Result>` for operation outcome
- ✅ On success: return Result.CreateSuccess() AND publish confirmation event
- ✅ On failure: return Result.Failure() AND log via ErrorService
- ✅ Event subscribers receive confirmation only on success
- ✅ Caller gets immediate Result for error handling

#### Error Handling Matrix:

| Scenario | Result.Failure | ErrorService.CaptureAsync | Event (Success/Fact) | BoundaryErrorEvent |
|----------|----------------|---------------------------|----------------------|-------------------|
| Method succeeds (intra-boundary) | ❌ No | ❌ No | ✅ Yes (if state changed) | ❌ No |
| Method fails (intra-boundary) | ✅ Yes | ✅ Yes | ❌ No | ❌ No |
| Method succeeds (cross-boundary call) | ✅ Yes | ❌ No | ✅ Yes | ❌ No |
| Method fails (cross-boundary call) | ✅ Yes | ✅ Yes | ❌ No | ✅ Yes (if affects other boundaries) |
| Event handler fails | N/A | ✅ Yes | ❌ No | ✅ Yes (if cross-boundary) |
| Validation failure (expected) | ✅ Yes | ❌ No | ❌ No | ❌ No |
| Unexpected exception | ✅ Yes | ✅ Yes | ❌ No | ✅ Yes (if cross-boundary) |

**Key Principles**:
1. **Result** = immediate outcome for the caller (success/failure + data/error)
2. **Events** = broadcast facts for interested parties (what happened, not whether it succeeded)
3. **ErrorService** = logging and monitoring (all exceptions)
4. **BoundaryErrorEvent** = cross-boundary failures that other systems need to know about

#### Decision Tree:

```
Is this within the same project boundary?
├─ YES → Use direct method call returning Result<T>
│         Publish event if state changed and others need to know
│
└─ NO (cross-boundary)
    ├─ Is this a command/notification?
    │   └─ Use fire-and-forget event (Global bus)
    │
    ├─ Is this a query for data?
    │   ├─ Can you call a service method directly? → YES → Use method call with Result<T>
    │   └─ Need to decouple completely? → Use Request-Response event (rarely needed)
    │
    └─ Is this an error that affects other boundaries?
        └─ Publish BoundaryErrorEvent + return Result.Failure
```

### High-Level Deliverables
- Consolidated event wiring model with `EventGenerator`/`EventTrigger`, clear request–response guidance, and `targets: Both` dual-publish semantics.
- DiscBot project structure aligned to features and boundaries with App flows and DSharpPlus renderers.
- Minimal, precise source generation hooks and documentation references.

---

## Progress Summary

**Overall Status:** ✅ ALL STEPS COMPLETE (1-7)! Asset management fully integrated with renderers.

- ✅ **Step 1:** DiscBot Refactor & Organization (5/5 items) - COMPLETED
- ✅ **Step 2:** DiscBot Apps (6/6 items) - COMPLETED
- ✅ **Step 3:** DSharpPlus Layer (13/13 items) - COMPLETED (all renderer asset integration complete)
- ✅ **Step 4:** Event Contracts (7/7 items) - COMPLETED (all events defined including asset events)
- ✅ **Step 5:** Wiring & Startup (14/14 items) - COMPLETED (temp storage + URL policy enforcement implemented)
- ✅ **Step 6:** Source Generation (8/8 items) - COMPLETED
- ✅ **Step 7:** Error Handling (5/5 items) - COMPLETED

**Key Achievements:**
- Event-driven DiscBot architecture with App/DSharpPlus separation
- 9 Global events + 15 DiscBot-local events defined and integrated
- Commands, interactions, and renderers implemented for scrimmage flow
- Events organized by domain (ScrimmageEvents, MatchEvents, GameEvents)
- **WabbitBot.Host composition root** created to orchestrate Core + DiscBot startup
- **DiscBotEventBus and DiscBotBootstrap** implemented for clean initialization
- **FileSystemService** integrated into CoreService with proper dependency management
- **EventGenerator/EventTrigger attributes** defined and applied to App classes and Core orchestrators
- **Hybrid Result + Event pattern** adopted across handlers and renderers
- **EventTriggerGenerator** auto-generates event publishers with dual-publish support (Local, Global, Both)
- **ComponentFactoryGenerator** auto-generates factory methods for POCO component models
- **Container-first UI pattern** established; embeds reserved for future simple responses
- **VisualBuildResult DTO** for fluent component/embed rendering with attachment support
- **DSharpPlus native support** for `EnableV2Components()` and `AddContainerComponent()` confirmed
- **Component model documentation** complete with POCO architecture and domain organization
- **Legacy error handling removed** - CoreErrorHandler, ICoreErrorHandler, and CoreHandler base class eliminated
- **GlobalEventBus request-response** pattern implemented with correlation ID tracking
- **Asset events** defined for cross-boundary asset resolution and file ingestion (AssetEvents.cs)
- **CDN metadata tracking** added to FileSystemService for Discord CDN URL management
- **FileSystem events** added for upload/delete notifications (FileSystemEvents.cs)
- **Dynamic file discovery** - FileSystemService no longer assumes file extensions
- **Temp storage** for DiscBot attachments with automatic cleanup (DiscBotTempStorage)
- **URL policy enforcement** prevents internal file paths in Discord visuals (AssetUrlValidator)

**Asset Management System:** ✅ COMPLETE - Full end-to-end implementation including GlobalEventBus request-response, asset events, CDN metadata tracking, FileSystem events, temp storage, URL policy enforcement, AssetResolver service, file attachment handling, and CDN capture utilities. Fully integrated with renderers.

---

## Checklist (DiscBot-first)

1. DiscBot Refactor & Organization (Development) ✅ COMPLETED
   - [x] 1a. Create initial directories (analyze first; then add if missing): `App/Events`, `App/Interfaces`, `App/Services`, `DSharpPlus/Commands`, `DSharpPlus/Handlers`, `DSharpPlus/Renderers`, `DSharpPlus/ComponentModels`.
   - [x] 1b. Keep legacy under `WabbitBotDiscBot_deprecated/`; do not reference during new wiring.
   - [x] 1c. Use `App/Services/DiscBot/DiscBotService.cs` as service locator for `IDiscBotEventBus` and `IErrorService` (no DI).
   - [x] 1d. Relocate and reuse `DiscordClientProvider.cs` to `src/WabbitBot.DiscBot/DSharpPlus/DiscordClientProvider.cs`; update namespace accordingly.
   - [x] 1e. Keep `DiscBot.cs` commented/unreferenced; rely on Core `Program.cs` to orchestrate startup by calling into DiscBot bootstrap within `DSharpPlus` (no DSharpPlus code in Core).

2. DiscBot Apps (Development) ✅ COMPLETED
   - [x] 2a. Add `MatchApp : IMatchApp, IDiscBotApp` with `[EventGenerator(generateSubscribers: true, defaultBus: EventBusType.DiscBot)]`; handle `MatchProvisioningRequested` → publish `MatchThreadCreateRequested` (DiscBot-local). Includes map ban flow.
   - [x] 2b. Integrated into MatchApp.
   - [x] 2c. Integrated into GameApp.
   - [x] 2d. Add `GameApp : IGameApp, IDiscBotApp` with `StartNextGameAsync` and `[EventTrigger(BusType = EventBusType.Global, targets: EventTargets.Global)]` for `GameStarted` when needed. Includes deck submission flow.
   - [x] 2e. Define `IDiscBotApp` marker and per-app interfaces (`IMatchApp`, `IScrimmageApp`, `IGameApp`); public methods are event-driven entrypoints.
   - [x] 2f. Ensure Apps publish/subscribe only via `DiscBotService.EventBus`; no DSharpPlus or DB access.

3. DSharpPlus Layer — Commands/Interactions/Renderers/ComponentModels (Development) ✅ COMPLETED
   - [x] 3a. Commands: Implement `ScrimmageCommands` using `DSharpPlus.Commands` only; translate inputs into Global/DiscBot events via `DiscBotService.PublishAsync`. Commands typically return `Task` (framework-handled responses).
   - [x] 3b. Interactions: Implement handlers for buttons/modals that publish DiscBot-local interaction events (e.g., `PlayerMapBanSelected`). Handlers return `Task` and use interaction responses for user feedback.
   - [x] 3c. **Renderers**: Return `Task<Result>` for operation outcomes. Subscribe to DiscBot-local "Requested" events and perform Discord API operations (threads, containers, DMs). On success: return `Result.CreateSuccess()` AND publish confirmation events (e.g., `MatchProvisioned` Global). On failure: return `Result.Failure(errorMessage)` AND log via `ErrorService` (no confirmation event published).
   - [x] 3d. Place DSharpPlus-specific code strictly under `src/WabbitBot.DiscBot/DSharpPlus/`.
   - [x] 3e. **ComponentModels organization**: All visual models (container models and embed models) are organized by domain in `DSharpPlus/ComponentModels/`:
        - `ScrimmageComponents.cs` - scrimmage-related visual models (e.g., `ChallengeContainer`)
        - `MatchComponents.cs` - match-related visual models (e.g., `MatchContainer`)
        - `GameComponents.cs` - game-related visual models (e.g., `GameContainer`)
        Component models are POCO classes with no inheritance and minimal DSharpPlus dependencies. **File-level documentation added** explaining POCO architecture, domain organization, and visual patterns.
   - [x] 3f. **Containers vs Embeds distinction**:
        - **Containers** (`DiscordContainerComponent`): Modern, advanced Discord UI pattern used for the vast majority of our displays. Containers support rich component layouts, interactive elements, and optional color theming. **This is our primary UI pattern.**
        - **Embeds** (`DiscordEmbed`): Simple, legacy Discord UI pattern with a narrowly defined role for simple interaction responses according to Discord best practices. Currently **not in use** - no embed models defined; container models only.
        Visual models expose ONE of: `DiscordContainerComponent Container { get; }` (current standard) OR `DiscordEmbed Embed { get; }` (future simple responses only). **Documented** in all component model files.
   - [x] 3g. Asset display: CDN-first strategy implemented via `DiscBotService.AssetResolver` (in `DiscBotService.AssetResolver.cs`). Uses GlobalEventBus request-response to query Core. Returns either CDN URL (preferred) or AttachmentHint for local upload with `attachment://` reference. Never exposes internal file paths.
   - [x] 3h. Temp storage: `DiscBotService.TempStorage` (in `DiscBotService.TempStorage.cs`) manages `data/tmp/discord` directory with periodic cleanup (15 min interval, 1 hour retention). Initialized in DiscBotBootstrap.
   - [x] 3i. Renderer fallback: `DiscordMessageBuilderExtensions.WithVisual()` (now async) automatically loads and attaches files when AttachmentHint present. Reads from shared `AppContext.BaseDirectory/data/` paths. Files referenced via `attachment://canonicalFileName`.
   - [x] 3j. Asset resolution: `DiscBotService.AssetResolver.ResolveAssetAsync()` queries Core via `AssetResolveRequested/AssetResolved` events. Returns tuple of `(cdnUrl, attachmentHint)`. Helpers: `ResolveMapThumbnailAsync()`, `ResolveDivisionIconAsync()`.
   - [x] 3k. CDN capture: `CdnCapture.CaptureFromMessageAsync()` (in `DSharpPlus/Utilities/CdnCapture.cs`) extracts CDN URLs from sent messages and reports via `FileCdnLinkReported` event to Core's FileSystemService for caching.
   - [x] 3l. VisualBuildResult API: `VisualBuildResult.FromContainer(container, attachment: hint)` accepts optional `AttachmentHint`. ComponentFactory generators return `VisualBuildResult`. Renderers use `await builder.WithVisual(visual)` for automatic attachment handling.

4. Event Contracts — Manual + Generated (Development) ✅ COMPLETED
   - [x] 4a. Manual Global events (minimal payloads): `ScrimmageChallengeRequested`, `ScrimmageAccepted`, `ScrimmageDeclined`, `ScrimmageCancelled`, `MatchProvisioned`, `MatchCompleted`, `GameStarted`, `GameCompleted`. **Note:** Organized by domain in `ScrimmageEvents.cs`, `MatchEvents.cs`, `GameEvents.cs` (not in Common; defined in owning projects).
   - [x] 4b. DiscBot-local request events (manual): All verified present - `MatchThreadCreateRequested`, `MatchContainerRequested`, `MapBanDmStartRequested`, `MapBanDmUpdateRequested`, `MapBanDmConfirmRequested`, `DeckDmStartRequested`, `DeckDmUpdateRequested`, `DeckDmConfirmRequested`, `GameContainerRequested` and interaction events.
   - [x] 4c. Generated via `[EventTrigger]` documentation: Placeholders documented in code for `MatchProvisioningRequested` (targets Both from Core), `GameStarted`, `GameCompleted`, `MatchCompleted`. Actual generation in step 6.
   - [x] 4d. Asset resolve (Request–Response, Global): `AssetResolveRequested(assetType, id, requestId)` → Core resolves via `FileSystemService` → `AssetResolved(assetType, id, canonicalFileName, cdnUrl?, relativePathUnderAppBase?, correlationId)`. **Events defined in `AssetEvents.cs`**
   - [x] 4e. File ingest (Global): `FileIngestRequested(tempFilePath, kind, metadata, requestId)` → Core validates/saves via `FileSystemService` → `FileIngested(canonicalFileName, kind, metadata, cdnUrl?, correlationId)`. **Events defined in `AssetEvents.cs`**
   - [x] 4f. CDN link report (Global): `FileCdnLinkReported(canonicalFileName, cdnUrl, sourceMessageId, channelId)`. **Event defined in `AssetEvents.cs`**
   - [x] 4g. Resolve preference: `AssetResolved` prefers returning `cdnUrl` when available. **Implemented in `FileSystemService.ResolveAsset()`**

5. Wiring & Startup (Development) ✅ COMPLETED (core items; asset-related deferred)
   - [x] 5a. Created `WabbitBot.Host` composition root with `Program.cs` that initializes Common infrastructure (GlobalEventBus, ErrorService) and calls DiscBot bootstrap in `WabbitBot.DiscBot.DSharpPlus` (no Discord API calls in Core).
   - [x] 5b. In `DiscBotBootstrap`, build the `DiscordClient` with `DSharpPlus 5.0` using `Commands` only; register command classes; set required gateway intents.
   - [x] 5c. Use `DiscordClientProvider.SetClient(client)` within the bootstrap; expose provider usage for Renderers and interactions.
   - [x] 5d. Initialize `DiscBotService` static internals (event bus, error handler) via public `Initialize` method called from Host (separate from test hook), avoiding DI.
   - [x] 5e. Register DSharpPlus interaction callbacks (`HandleComponentInteractionCreated`, `HandleModalSubmitted`) that delegate to handlers publishing DiscBot-local interaction events.
   - [x] 5f. Initialize a single `FileSystemService` instance during Core startup (inside `InitializeCoreServices`), passing explicit `CoreEventBus` and shared `IErrorService` rather than using defaults.
   - [x] 5g. Expose the `FileSystemService` shared instance via `CoreService.FileSystem` static property for use by Core features; avoid runtime DI and avoid multiple instances.
   - [x] 5h. Ensure directory roots exist and remain under `AppContext.BaseDirectory` via FileSystemService constructor; configuration override capability exists but not yet wired.
   - [x] 5i. Architecture enforces FileSystem operations in Core only; DiscBot emits events/commands; Core features invoke `FileSystemService` methods as needed.
   - [x] 5j. FileSystemService events: `ThumbnailUploadedEvent`, `ThumbnailDeletedEvent`, `DivisionIconUploadedEvent`, `DivisionIconDeletedEvent` published on successful operations. **Implemented in `FileSystemEvents.cs`**. These are infrastructure facts (not database CRUD), allowing downstream subscribers to react to file system state changes.
   - [x] 5k. DiscBot temp storage: `DiscBotTempStorage` static service manages `data/tmp/discord` directory for attachment downloads. Initialized in `DiscBotBootstrap.InitializeServicesAsync()` with periodic cleanup (15 min interval, 1 hour retention). **Implemented in `DiscBotTempStorage.cs`**.
   - [x] 5l. URL policy enforcement: `AssetUrlValidator` validates all embed/container URLs to prevent internal file path exposure. Only HTTPS CDN URLs or `attachment://` URIs permitted. Integrated into `DiscordMessageBuilderExtensions.WithVisual()` for automatic validation. **Implemented in `AssetUrlValidator.cs`**.
   - [x] 5m. FileSystemService metadata: Lightweight `CdnMetadata` record added with `RecordCdnMetadata()` and `GetCdnMetadata()` methods. Thread-safe dictionary cache in memory. **Implemented in `FileSystemService.CdnMetadata.cs`**
   - [x] 5n. Idempotency: Last-write-wins semantics ensure reporting an already-known CDN URL is safe. **Implemented in `RecordCdnMetadata()`**

6. Source Generation Touchpoints (Development) ✅ COMPLETED (8/8 items)
   - [x] 6a. Apply `[EventGenerator]` to App flow classes and Core orchestrators that should auto-wire publishers/subscribers.
   - [x] 6b. Apply `[EventTrigger]` to explicit opt-in methods with `BusType`/`targets` per drafts; support `targets: Both` when crossing boundaries.
   - [x] 6c. Implement source generator to replace stub implementations; verify generators avoid runtime DI and emit minimal boilerplate only. **EventTriggerGenerator** created and tested - generates async partial methods with proper bus routing (Local/Global/Both), context-aware using statements, and null-safe event creation.
   - [x] 6d. Re-implement `ComponentFactoryGenerator` (formerly `EmbedFactoryGenerator`) to scan POCO component models marked with `[GenerateComponentFactory]` in `DSharpPlus/ComponentModels/` domain files (`ScrimmageComponents.cs`, `MatchComponents.cs`, `GameComponents.cs`). No dependency on deprecated `BaseEmbed`. **ComponentFactoryGenerator** created - generates static `ComponentFactory` class with `Build{ModelName}` methods, argument null checking, and theme metadata comments. `EmbedFactoryGenerator` marked obsolete.
   - [x] 6e. Theme support deferred - `Theme` property added to `GenerateComponentFactoryAttribute` for metadata/documentation purposes; actual theming implementation deferred to future enhancement when visual styling utilities are implemented.
   - [x] 6f. Factory methods updated to return `VisualBuildResult` DTO with `Container`, `Embed`, and `Attachment` fields. Generated factories use `VisualBuildResult.FromContainer(model.ComponentType, attachment: null)` for current POCO models. Branch logic for embed pattern prepared but not in use (no embed models defined).
   - [x] 6g. **DTOs defined** in `VisualBuildResult.cs`: `AttachmentHint` record (`CanonicalFileName`, `ContentType`, helper `ForImage()`), and `VisualBuildResult` record with factory methods `FromContainer()` and `FromEmbed()`.
   - [x] 6h. **Renderer integration complete**: `DiscordMessageBuilderExtensions.WithVisual()` extension method created to consume `VisualBuildResult`. Discovery: DSharpPlus `BaseDiscordMessageBuilder<T>` already provides `EnableV2Components()` and `AddContainerComponent()` - no custom extensions needed for these. Only custom `WithVisual()` helper implemented for fluent DTO consumption.

7. Error Handling & Naming Alignment (Development) ✅ COMPLETED (5/5 items)
   - [x] 7a. **Removed** `CoreErrorHandler`, `ICoreErrorHandler`, and `CoreHandler` base class entirely (not just deprecated). All files deleted.
   - [x] 7b. Updated `ConfigurationHandler` to use direct `ICoreEventBus` and `IErrorService` dependencies instead of inheriting from `CoreHandler`.
   - [x] 7c. System already standardized on `IErrorService` usage via `CoreService.ErrorHandler`. `BoundaryErrorEvent` already exists in `StartupEvents.cs`.
   - [x] 7d. DiscBot already uses `DiscBotService.ErrorHandler` (type `IErrorService`). No legacy error handlers exist in DiscBot.
   - [x] 7e. All singleton/static patterns removed. `ConfigurationHandler` now uses `CoreService.ErrorHandler` directly.


