## Procedural Flow Example — Scrimmage (EventGenerator + EventTrigger Opt-In)

Assumptions
- Two attributes:
  - `[EventGenerator(...)]` on classes (with `defaultBus`, `generatePublishers`, `generateSubscribers`, `generateRequestResponse`, etc.).
  - `[EventTrigger(...)]` on specific methods to opt-in event generation and optionally override bus and targets.
- No naming convention required when using `[EventTrigger]` (opt-in only).
- Attribute parameters (illustrative):
  - EventGenerator: `defaultBus: EventBusType`, `generatePublishers: bool`, `generateSubscribers: bool`, `generateRequestResponse: bool`.
  - EventTrigger: `BusType: EventBusType?` (override), `targets: EventTargets = EventTargets.Global | Local | Both`.
- Code placement clarity:
  - Core business logic lives under `src/WabbitBot.Core/...` in classes like `ScrimmageCore`, `MatchCore`, `GameCore` (e.g., `GameCore.GameFlow`).
  - DiscBot mirrors Core layering:
    - App layer (library-agnostic): `src/WabbitBot.DiscBot/DiscBot/App/...` with flows `MapBanFlow`, `DeckCodeFlow`, `MatchProvisioningFlow`, `GameFlow`. Flows publish/subscribe events only (no DSharpPlus).
    - DSharpPlus layer (library-specific): `src/WabbitBot.DiscBot/DSharpPlus/...` for Commands/Interactions/Renderers that translate events to Discord API calls using `DSharpPlus.Commands`.

### Stage 1: Initial Challenge Discord Command Issued

Location: `src/WabbitBot.DiscBot/DSharpPlus/Commands/ScrimmageCommands.cs` (DiscBot only)

```csharp
// DiscBot layer (DSharpPlus.Commands)
public partial class ScrimmageCommands
{
    // DSharpPlus.Commands handler (pseudocode signature)
    public async Task ChallengeAsync(string challengerTeam, string opponentTeam)
    {
        // Light validation only; no DB work here
        // Publish a Global integration fact to Core to validate and prepare challenge container
        await DiscBotEventBus.Instance.PublishAsync(new ScrimmageChallengeRequested(
            ChallengerTeamName: challengerTeam,
            OpponentTeamName: opponentTeam,
            EventBusType: EventBusType.Global));
    }
}
```

Event (manual): `ScrimmageChallengeRequested` in Common (Global), minimal payload.

### Stage 2: Creation of the Discord container for the challenge

Location: `src/WabbitBot.DiscBot/App/ScrimmageApp.cs`

```csharp
[EventGenerator(generateSubscribers: true, triggerMode: "OptIn", defaultBus: EventBusType.DiscBot)]
public partial class ScrimmageApp
{
    // Auto-subscribed by generator to Global ScrimmageChallengeRequested
    public async Task HandleScrimmageChallengeRequestedAsync(ScrimmageChallengeRequested evt)
    {
        // App layer requests container creation; DSharpPlus Renderer performs API call
        // Event definition: manual (DiscBot-local request)
        await DiscBotEventBus.Instance.PublishAsync(new ChallengeContainerCreateRequested(
            ChallengeId: evt.ChallengeId,
            EventBusType: EventBusType.DiscBot));
    }
}
```

### Stage 3: Opponent Accepts → Create Scrimmage + Match (Hybrid)

Location: DiscBot receives interaction → publish Global fact; Core creates entities.

```csharp
// DiscBot interaction handler (button/modal)
public async Task AcceptChallengeAsync(string challengeId)
{
    // Event definition: manual (Common, Global integration fact)
    await DiscBotEventBus.Instance.PublishAsync(new ScrimmageAccepted(
        ChallengeId: challengeId,
        EventBusType: EventBusType.Global));
}

// Core side (ScrimmageCore): produce business facts and perform persistence
[EventGenerator(generatePublishers: true, triggerMode: "OptIn", defaultBus: EventBusType.Core)]
public partial class ScrimmageCore
{
    // Public method performs write operations; uses Hybrid pattern (Result + Event)
    public async Task<Result> PrepareMatchAsync(Guid challengeId)
    {
        try
        {
            // Step 1) Load challenge via validation helper
            var challenge = await ScrimmageCore.Validation.LoadChallengeAsync(challengeId);
            if (challenge is null)
                return Result.Failure("Challenge not found");

            // Step 2) Load and validate teams via TeamCore helper
            var teamsResult = await TeamCore.Validation.LoadAndValidateTeamsAsync(
                challenge.ChallengerTeamId,
                challenge.OpponentTeamId,
                challenge.TeamSize);
            
            if (!teamsResult.Success)
                return teamsResult; // Return validation failure

            var (challengerTeam, opponentTeam) = teamsResult.Data!;

            // Step 3) Build entities via factories
            var scrimmage = ScrimmageCore.Factory.Create(challengerTeam, opponentTeam, challenge.TeamSize);
            var match = MatchCore.Factory.CreateFor(scrimmage, bestOf: 1);

            // Step 4) Persist entities via repositories/services
            await DatabaseService<Scrimmage>.InsertAsync(scrimmage);
            await DatabaseService<Match>.InsertAsync(match);

            // Step 5) Publish provisioning request to both Core (local) and Global (cross-boundary)
            // Event definition: generated (Common contract), single trigger publishes to Both
            await PublishMatchProvisioningRequestedAsync(match.Id, scrimmage.Id);

            // Step 6) Return success to caller (Hybrid pattern: Result + Event)
            return Result.CreateSuccess();
        }
        catch (Exception ex)
        {
            await CoreService.ErrorHandler.CaptureAsync(ex, "Failed to prepare match", nameof(PrepareMatchAsync));
            return Result.Failure($"Failed to prepare match: {ex.Message}");
        }
    }

    // Opt-in trigger: generator emits publisher that dual-publishes (local Core + Global)
    // targets=Both implies two emits with identical payloads but different EventBusType
    [EventTrigger(BusType = EventBusType.Global, targets: EventTargets.Both)]
    public ValueTask PublishMatchProvisioningRequestedAsync(Guid matchId, Guid scrimmageId) => ValueTask.CompletedTask; // generator fills body
}
```

### Stage 4: Create Discord Thread(s) for Match (assume 1v1 → single public thread)

Location: App reacts to Global integration request; Renderer handles API.

**App Layer** (orchestrates via events):
```csharp
[EventGenerator(generateSubscribers: true, triggerMode: "OptIn", defaultBus: EventBusType.DiscBot)]
public partial class MatchProvisioningApp
{
    public async Task HandleMatchProvisioningRequestedAsync(MatchProvisioningRequested evt)
    {
        // App layer requests thread creation; Renderer handles the actual Discord API call
        await DiscBotEventBus.Instance.PublishAsync(new MatchThreadCreateRequested(
            MatchId: evt.MatchId,
            EventBusType: EventBusType.DiscBot));
    }
}
```

**Renderer Layer** (performs Discord API operations, returns Result):
```csharp
public partial class MatchRenderer
{
    // Subscribes to DiscBot-local request events
    public async Task<Result> HandleMatchThreadCreateRequestedAsync(MatchThreadCreateRequested evt)
    {
        try
        {
            var channel = await GetMatchChannelAsync();
            var thread = await channel.CreateThreadAsync($"Match {evt.MatchId}", ...);

            // Success: Publish Global confirmation event for Core/other subscribers
            await DiscBotService.PublishAsync(new MatchThreadCreatedEvent(
                MatchId: evt.MatchId,
                ThreadId: thread.Id,
                EventBusType: EventBusType.Global));

            // Return success to any direct caller
            return Result.CreateSuccess();
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(ex, "Failed to create match thread", nameof(HandleMatchThreadCreateRequestedAsync));
            return Result.Failure($"Failed to create thread: {ex.Message}");
        }
    }
}
```

### Stage 5: Create Match Container

Location: App flow requests container; Renderer composes and sends.

```csharp
public async Task RequestMatchContainerAsync(Guid matchId, ulong threadId)
{
    // App layer requests container; Renderer posts it then can emit confirmation
    await DiscBotEventBus.Instance.PublishAsync(new MatchContainerRequested(
        MatchId: matchId,
        ThreadId: threadId,
        EventBusType: EventBusType.DiscBot));

    // Optional Global confirmation (Renderer or flow can emit after completion)
    await DiscBotEventBus.Instance.PublishAsync(new MatchProvisioned(
        MatchId: matchId,
        ThreadId: threadId,
        EventBusType: EventBusType.Global));
}
```

Renderer container usage (DSharpPlus 5.0, simplified):

```csharp
// Build child components (buttons, selects, etc.)
var components = new List<DiscordComponent>
{
    // new DiscordButtonComponent(...), new DiscordStringSelectComponent(...), etc.
};

// Create a container with optional color and spoiler flag
var container = new DiscordContainerComponent(
    components: components,
    isSpoilered: false,
    color: EmbedStyling.GetInfoColor());

// Add the container to a message builder and send
var msg = new DiscordMessageBuilder()
    .AddContainerComponent(container);
await channel.SendMessageAsync(msg);
```

### Stage 6: Player DM Map Ban Flow (1v1)

Location: App publishes DM requests; Renderer sends DMs.

```csharp
[EventGenerator(generateSubscribers: true, defaultBus: EventBusType.DiscBot)]
public partial class MapBanApp
{

    public async Task StartMapBanDMsAsync(Guid matchId, ulong player1DiscordId, ulong player2DiscordId)
    {
        // App requests DMs; Renderer sends
        await DiscBotEventBus.Instance.PublishAsync(new MapBanDmStartRequested(matchId, player1DiscordId, EventBusType.DiscBot));
        await DiscBotEventBus.Instance.PublishAsync(new MapBanDmStartRequested(matchId, player2DiscordId, EventBusType.DiscBot));
    }

    // Fired by interaction callbacks when a player selects provisional bans
    public async Task OnPlayerMapBanSelectedAsync(PlayerMapBanSelected evt)
    {
        // Request Renderer to update DM preview
        await DiscBotEventBus.Instance.PublishAsync(new MapBanDmUpdateRequested(evt.MatchId, evt.PlayerId, evt.Selections, EventBusType.DiscBot));
    }

    // Fired when the player confirms their bans
    public async Task OnPlayerMapBanConfirmedAsync(PlayerMapBanConfirmed evt)
    {
        // Request Renderer to lock UI
        await DiscBotEventBus.Instance.PublishAsync(new MapBanDmConfirmRequested(evt.MatchId, evt.PlayerId, evt.Selections, EventBusType.DiscBot));
    }
}
```

Event notes (first mentions):
- `MapBanDmStartRequested(matchId, playerDiscordId)`: manual, DiscBot-local (Renderer sends DM).
- `PlayerMapBanSelected(matchId, playerId, selections)`: manual, DiscBot-local (interaction input).
- `MapBanDmUpdateRequested(matchId, playerId, selections)`: manual, DiscBot-local (Renderer updates DM).
- `PlayerMapBanConfirmed(matchId, playerId, selections)`: manual, DiscBot-local (interaction input).
- `MapBanDmConfirmRequested(matchId, playerId, selections)`: manual, DiscBot-local (Renderer locks UI).

Synchronization to Core (optional): When both players confirm, DiscBot can emit a Global fact `MatchBansConfirmed(matchId, bans)` if Core must persist bans. Otherwise, DiscBot keeps UI state local.

### Stage 7: Player DM Deck Code Submission (Game 1)

```csharp
[EventGenerator(generateSubscribers: true, defaultBus: EventBusType.DiscBot)]
public partial class DeckApp
{

    public async Task StartDeckSubmissionDMsAsync(Guid matchId, ulong player1DiscordId, ulong player2DiscordId)
    {
        // App requests DM inputs; Renderer sends DM
        await DiscBotEventBus.Instance.PublishAsync(new DeckDmStartRequested(matchId, player1DiscordId, EventBusType.DiscBot));
        await DiscBotEventBus.Instance.PublishAsync(new DeckDmStartRequested(matchId, player2DiscordId, EventBusType.DiscBot));
    }

    public async Task OnDeckSubmittedAsync(PlayerDeckSubmitted evt)
    {
        // Request Renderer to preview submission
        await DiscBotEventBus.Instance.PublishAsync(new DeckDmUpdateRequested(evt.MatchId, evt.PlayerId, evt.DeckCode, EventBusType.DiscBot));
    }

    public async Task OnDeckConfirmedAsync(PlayerDeckConfirmed evt)
    {
        // Request Renderer to lock deck code UI
        await DiscBotEventBus.Instance.PublishAsync(new DeckDmConfirmRequested(evt.MatchId, evt.PlayerId, evt.DeckCode, EventBusType.DiscBot));
    }
}
```

Event notes (first mentions):
- `DeckDMStarted(matchId, playerDiscordId)`: manual, DiscBot-local.
- `PlayerDeckSubmitted(matchId, playerId, deckCode)`: manual, DiscBot-local.
- `PlayerDeckConfirmed(matchId, playerId, deckCode)`: manual, DiscBot-local.

Gate to start Game 1: When both players have confirmed bans and deck codes, proceed to per-game flow.

### Stage 8: Start Game 1 — Per-Game Container and Random Map

```csharp
[EventGenerator(generateSubscribers: true, defaultBus: EventBusType.DiscBot)]
public partial class GameApp
{

    public async Task StartNextGameAsync(Guid matchId, int gameNumber, MapPool remaining)
    {
        // Choose a random map client-side (DiscBot) or consume one provided by Core
        var chosenMap = ChooseRandom(remaining);

        // Request per-game container/message in the match thread
        await DiscBotEventBus.Instance.PublishAsync(new GameContainerRequested(matchId, gameNumber, chosenMap, EventBusType.DiscBot));

        // Optional Global integration if Core needs authoritative state:
        // Event definition: generated via trigger to Global only
        await PublishGameStartedAsync(matchId, gameNumber, chosenMap);
    }

    [EventTrigger(BusType = EventBusType.Global, targets: EventTargets.Global)]
    public ValueTask PublishGameStartedAsync(Guid matchId, int gameNumber, string chosenMap) => ValueTask.CompletedTask; // generator
}
```

Event notes:
- `GameStarted(matchId, gameNumber, chosenMap)`: generated, Global (optional; only if Core/others need it).

### Stage 9: Replay Submission and Winner Determination

```csharp
public async Task OnReplaySubmittedAsync(GameReplaySubmitted evt)
{
    // Event definition: manual, DiscBot-local (upload+parse trigger)
    // Parse the replay(s), determine winner; parsing module can be local or Core-integrated
    var result = await ReplayParser.DetermineWinnerAsync(evt.ReplayFileIds);

    // Update the per-game container with the winner
    // ... update message/embed ...

    // Broadcast result for Core/state consumers
    // Event definition: generated via trigger to Global (fact)
    await PublishGameCompletedAsync(evt.MatchId, evt.GameNumber, result.WinnerTeamId);
}

[EventTrigger(BusType = EventBusType.Global, targets: EventTargets.Global)]
public ValueTask PublishGameCompletedAsync(Guid matchId, int gameNumber, Guid winnerTeamId) => ValueTask.CompletedTask; // generator
```

Event notes:
- `GameReplaySubmitted(matchId, gameNumber, replayFileIds)`: manual, DiscBot-local.
- `GameCompleted(matchId, gameNumber, winnerTeamId)`: generated, Global (for Core stats/ratings).

### Stage 10: Loop Until Victory Condition

```csharp
public async Task ContinueOrFinishAsync(Guid matchId, MatchSeries series)
{
    if (series.HasWinner)
    {
        await FinalizeMatchAsync(matchId, series.WinnerTeamId);
        return;
    }

    // Request next game deck codes via DM
    await StartDeckSubmissionDMsAsync(matchId, series.Player1DiscordId, series.Player2DiscordId);
}
```

### Stage 11: Final Summary in Match Thread

```csharp
public async Task<Result> FinalizeMatchAsync(Guid matchId, Guid winnerTeamId)
{
    try
    {
        // Update the last game container with winner and append match summary
        var updateResult = await UpdateMatchSummaryInThreadAsync(matchId, winnerTeamId);
        
        if (!updateResult.Success)
            return updateResult; // Return failure if UI update fails

        // Broadcast final fact for persistence/rating updates (Global event for Core)
        // Event definition: generated via trigger to Global (fact)
        await PublishMatchCompletedAsync(matchId, winnerTeamId);

        // Return success (Hybrid pattern: Result + Event)
        return Result.CreateSuccess();
    }
    catch (Exception ex)
    {
        await DiscBotService.ErrorHandler.CaptureAsync(ex, "Failed to finalize match", nameof(FinalizeMatchAsync));
        return Result.Failure($"Failed to finalize match: {ex.Message}");
    }
}

[EventTrigger(BusType = EventBusType.Global, targets: EventTargets.Global)]
public ValueTask PublishMatchCompletedAsync(Guid matchId, Guid winnerTeamId) => ValueTask.CompletedTask; // generator
```

Event notes:
- `MatchCompleted(matchId, winnerTeamId)`: generated, Global (Core persists, updates ratings/leaderboards).

**Pattern Note**: This method demonstrates the Hybrid pattern:
- Returns `Result` for immediate caller feedback (can handle errors locally)
- Publishes `MatchCompletedEvent` for Global subscribers (Core leaderboards, ratings, etc.)
- Event is only published on success (failure returns `Result.Failure` without event)

Notes on responsibilities
- DiscBot: UI, threads, containers; emits/consumes DiscBot or Global events; no domain writes.
- Core: repositories/services for domain writes; emits Core facts and precise Global integration requests.
- Global facts: minimal payloads, stable contracts, explicit cross-boundary routing.

### Event Contracts (Minimal Payloads)

All events include `EventBusType`, `EventId`, and `Timestamp` per `IEvent`.

- `MatchProvisioningRequested(matchId: Guid, scrimmageId: Guid)`
  - Type: generated via EventTrigger (targets Both: local Core + Global)
  - Purpose: Signals DiscBot to create threads/containers; allows Core-local listeners to react.

- `GameStarted(matchId: Guid, gameNumber: int, chosenMap: string)`
  - Type: generated (optional Global)
  - Purpose: Announces a new game started, for analytics or state trackers.

- `GameCompleted(matchId: Guid, gameNumber: int, winnerTeamId: Guid)`
  - Type: generated (Global)
  - Purpose: Records a single-game result for standings, ratings, and history.

- `MatchCompleted(matchId: Guid, winnerTeamId: Guid)`
  - Type: generated (Global)
  - Purpose: Final match result for persistence, rating updates, and leaderboards.

- DiscBot-local UI events (manual, local only):
  - `MapBanDMStarted(matchId: Guid, playerDiscordId: ulong)`
  - `PlayerMapBanSelected(matchId: Guid, playerId: Guid | ulong, selections: string[])`
  - `PlayerMapBanConfirmed(matchId: Guid, playerId: Guid | ulong, selections: string[])`
  - `DeckDMStarted(matchId: Guid, playerDiscordId: ulong)`
  - `PlayerDeckSubmitted(matchId: Guid, playerId: Guid | ulong, deckCode: string)`
  - `PlayerDeckConfirmed(matchId: Guid, playerId: Guid | ulong, deckCode: string)`

Design Considerations for PrepareMatchAsync (Result + Event Hybrid Pattern)

**Recommended Approach** - Hybrid pattern with Result return and Event publishing:

```csharp
public async Task<Result> PrepareMatchAsync(Guid challengeId)
{
    try
    {
        // Step 1: Load and validate
        var challenge = await ScrimmageCore.Validation.LoadChallengeAsync(challengeId);
        if (challenge is null)
            return Result.Failure("Challenge not found");

        var teamsResult = await TeamCore.Validation.LoadAndValidateTeamsAsync(
            challenge.ChallengerTeamId, challenge.OpponentTeamId, challenge.TeamSize);
        
        if (!teamsResult.Success)
            return teamsResult; // Propagate validation failure

        var (challenger, opponent) = teamsResult.Data!;

        // Step 2: Create entities
        var scrimmage = ScrimmageCore.Factory.Create(challenger, opponent, challenge.TeamSize);
        var match = MatchCore.Factory.CreateFor(scrimmage, bestOf: 1);

        // Step 3: Persist
        await DatabaseService<Scrimmage>.InsertAsync(scrimmage);
        await DatabaseService<Match>.InsertAsync(match);

        // Step 4: Publish event (targets Both: Core + Global)
        await PublishMatchProvisioningRequestedAsync(match.Id, scrimmage.Id);

        // Step 5: Return success
        return Result.CreateSuccess();
    }
    catch (Exception ex)
    {
        await CoreService.ErrorHandler.CaptureAsync(ex, "Failed to prepare match", nameof(PrepareMatchAsync));
        return Result.Failure($"Failed to prepare match: {ex.Message}");
    }
}
```

**Why Hybrid Pattern**:
- ✅ Caller gets immediate Result feedback (can handle errors)
- ✅ Subscribers get event notification (DiscBot creates threads, etc.)
- ✅ Event only published on success (failure returns Result.Failure without event)
- ✅ Errors logged via ErrorService for monitoring
- ✅ Clean separation: Result for control flow, Event for side effects
