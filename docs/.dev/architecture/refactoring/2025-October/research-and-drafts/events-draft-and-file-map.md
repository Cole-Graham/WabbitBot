# Events Draft and File Map — 2025-October (Development)

## Purpose
Define event categories, manual vs. generated policy, attribute schema, and map current files per bus.

## Categories
- Core Business Facts (Core): domain state changes (e.g., match started/completed, team rating updated), idempotent.
- Core Module Signals (Core): module-internal notifications (e.g., configuration changed), not forwarded globally.
- DiscBot Interaction Signals (DiscBot): Discord UI/interaction-centric notifications (e.g., scrimmage accepted), DiscBot-only.
- Global Lifecycle/Boundary (Global): startup/shutdown/error broadcasts and cross-boundary facts.
- Request–Response (Any→Any): async query pattern only for cross-boundary needs; prefer direct calls intra-boundary.

## Manual vs Generated Policy
- Manual (explicit code):
  - Complex payloads or stability-critical events (e.g., match lifecycle, leaderboard updates).
  - Global lifecycle events.
  - UI request/interaction events in DiscBot App/Renderer (local orchestration only).
  - Events requiring custom serialization or nuanced versioning.
- Generated (via [EventGenerator] + [EventTrigger]):
  - Thin facts raised from opt-in methods with simple payloads.
  - Publisher helpers emitted for trigger methods.
  - Optional request–response scaffolding when explicitly enabled on the generator.

## Attribute Schema (Proposed)
- [EventGenerator(defaultBus: EventBusType, generatePublishers: bool = true, generateSubscribers: bool = true, generateRequestResponse: bool = false)] on classes.
- [EventTrigger(BusType: EventBusType? = null, targets: EventTargets = EventTargets.Local | EventTargets.Global | EventTargets.Both)] on specific methods.

Notes:
- Event triggering is opt-in via [EventTrigger]; no naming convention required.
- `targets: Both` means dual-publish using the same payload to the local `defaultBus` and to Global.

## Bus Routing Rules
- Routing is set via the `EventBusType` property on each event.
- Core events set `EventBusType = EventBusType.Core`; DiscBot events set `EventBusType = EventBusType.DiscBot`.
- Cross-boundary facts explicitly set `EventBusType = EventBusType.Global`.
- `targets: Both` on a trigger emits both a local (defaultBus) and a Global event.
- Global events live in Common; published by boundaries and listened to across projects.

## DiscBot Layering (Mirrors Core)
- App layer (library-agnostic): `src/WabbitBot.DiscBot/DiscBot/App/...`
  - Apps (e.g., `MapBanApp`, `DeckApp`, `MatchProvisioningApp`, `GameApp`) publish/subscribe events only.
  - Apps NEVER call DSharpPlus; they coordinate via DiscBot-local request events and Global facts.
- DSharpPlus layer (library-specific): `src/WabbitBot.DiscBot/DSharpPlus/...`
  - Commands/Interactions/Renderers adapt events to Discord API using `DSharpPlus.Commands`.
  - Renderers subscribe to DiscBot-local "Requested" events (e.g., `MatchThreadCreateRequested`) and perform API calls.

## Request–Response Guidance
- Use only when caller and callee span different buses.
- Correlate via EventId; set timeouts at call sites; handlers remain idempotent.

## Error Propagation
- Use Global BoundaryErrorEvent for cross-boundary failures; keep intra-boundary errors local to logs and error handlers.

## File Map (Current)

### Common (Global)
- `src/WabbitBot.Common/Events/StartupEvents.cs`: StartupInitiatedEvent, SystemReadyEvent, ApplicationReadyEvent, ApplicationShuttingDownEvent, GlobalErrorHandlingReadyEvent, CriticalStartupErrorEvent, BoundaryErrorEvent.

### Core (Core)
- `src/WabbitBot.Core/Common/Events/ConfigurationEvents.cs`: ConfigurationChangedEvent, ServerIdSetEvent, ChannelConfiguredEvent, RoleConfiguredEvent.
- `src/WabbitBot.Core/Common/Events/MatchEvents.cs`: MatchCreatedEvent, MatchStartedEvent, MatchCompletedEvent, MatchCancelledEvent, MatchForfeitedEvent, MatchPlayerJoinedEvent, MatchPlayerLeftEvent.
- `src/WabbitBot.Core/Common/Events/TeamEvents.cs`: Team-specific facts (inspect during refinement).
- `src/WabbitBot.Core/Common/Events/GameEvents.cs`: Game-specific facts (inspect during refinement).
- `src/WabbitBot.Core/Common/Events/MapEvents.cs`: Map-specific facts (inspect during refinement).
- `src/WabbitBot.Core/Common/Events/PlayerEvents.cs`: Player-specific facts (inspect during refinement).
- `src/WabbitBot.Core/Common/Events/ServerEvents.cs`: Server-specific facts (inspect during refinement).
- `src/WabbitBot.Core/Common/Events/UserEvents.cs`: User-specific facts (inspect during refinement).
- `src/WabbitBot.Core/Leaderboards/SeasonEvents.cs`: SeasonEndedEvent, SeasonRatingDecayAppliedEvent, TeamRegisteredForSeasonEvent, TeamResultAddedEvent, SeasonValidationEvent, SeasonTeamsUpdatedEvent, TeamRatingUpdatedEvent, ApplyTeamRatingChangeEvent.
- `src/WabbitBot.Core/Tournaments/TournamentEvents.cs`: TournamentStatusChangedEvent.

### DiscBot (DiscBot)
- App (proposed apps): `src/WabbitBot.DiscBot/DiscBot/App/...`
  - Example DiscBot-local request events (manual, local-only) used by Renderers:
    - `ChallengeContainerCreateRequested(challengeId)`
    - `MatchThreadCreateRequested(matchId)`
    - `MatchContainerRequested(matchId, threadId)`
    - `MapBanDmStartRequested(matchId, playerDiscordId)` / `MapBanDmUpdateRequested(...)` / `MapBanDmConfirmRequested(...)`
    - `DeckDmStartRequested(matchId, playerDiscordId)` / `DeckDmUpdateRequested(...)` / `DeckDmConfirmRequested(...)`
    - `GameContainerRequested(matchId, gameNumber, chosenMap)`
- DSharpPlus: keep `DiscordEventBus.cs` and readiness events; commands/interaction handlers and renderers subscribe to the above DiscBot-local requests to perform API operations.

## Draft Decisions to Carry into Implementation
- Keep business-critical Core events manual; allow generated helpers for publishers/subscribers.
- Allow generation only when method payloads are primitives/DTOs; skip generation for complex domains.
- For generated events, emit the appropriate default `EventBusType` (Core/DiscBot) based on the boundary; use Global only when explicitly intended to cross boundaries.
- Do not auto-forward DiscBot events; require explicit Global marking.

## Event Trigger Detection (Finalized)
- Behavior: Methods explicitly marked with `[EventTrigger]` are the only generation triggers.
- Implications: No accidental generation on unrelated public methods; intent is explicit at method level.

## Follow-ups
- Evaluate whether to introduce [EventTrigger] for per-method opt-in.
- Provide short code references under the architecture doc’s terms for discoverability.

---

