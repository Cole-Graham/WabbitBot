## State machine guards/actions with delegates (Matches/Games)

### Goal
- Use simple delegates for guards and actions to keep transitions explicit and testable.
- Apply to existing `MatchCore.State.MatchState` and `MatchCore.State.GameState` mechanics.

### Current state definitions
Match state transitions and helpers:

```csharp
    // 18:27:src/WabbitBot.Core/Common/Models/Common/MatchCore.State.cs
                private static readonly Dictionary<MatchStatus, List<MatchStatus>> _validTransitions = new()
                {
                    [MatchStatus.Created] = new() { MatchStatus.InProgress, MatchStatus.Cancelled },
                    [MatchStatus.InProgress] = new() { MatchStatus.Completed, MatchStatus.Cancelled, MatchStatus.Forfeited },
                    [MatchStatus.Completed] = new(),
                    [MatchStatus.Cancelled] = new(),
                    [MatchStatus.Forfeited] = new(),
                };
```

Game state transitions mirror this:

```csharp
    // 132:140:src/WabbitBot.Core/Common/Models/Common/MatchCore.Game.cs
                private static readonly Dictionary<GameStatus, List<GameStatus>> _validTransitions = new()
                {
                    [GameStatus.Created] = new() { GameStatus.InProgress, GameStatus.Cancelled },
                    [GameStatus.InProgress] = new() { GameStatus.Completed, GameStatus.Cancelled, GameStatus.Forfeited },
                    [GameStatus.Completed] = new(),
                    [GameStatus.Cancelled] = new(),
                    [GameStatus.Forfeited] = new(),
                };
```

### Delegate types
```csharp
public delegate bool Guard<in T>(T aggregate);
public delegate Task<Result> ActionAsync<in T>(T aggregate, CancellationToken ct);
```

### Applying to Match transitions
Define guard/action maps for specific transitions; keep logic local, test easily.

```csharp
public static class MatchTransitions
{
    public static readonly Dictionary<(MatchStatus From, MatchStatus To), Guard<Match>> Guards = new()
    {
        [(MatchStatus.Created, MatchStatus.InProgress)] = m => m.StartedAt is null,
        [(MatchStatus.InProgress, MatchStatus.Completed)] = m => m.WinnerId.HasValue,
    };

    public static readonly Dictionary<(MatchStatus From, MatchStatus To), ActionAsync<Match>> Actions = new()
    {
        [(MatchStatus.Created, MatchStatus.InProgress)] = async (m, ct) =>
        {
            var snapshot = MatchCore.Accessors.GetCurrentSnapshot(m);
            if (snapshot.Team1BansConfirmed && snapshot.Team2BansConfirmed)
                return Result.CreateSuccess();
            return Result.Failure("Both teams must confirm bans before starting");
        },
        [(MatchStatus.InProgress, MatchStatus.Completed)] = async (m, ct) =>
        {
            if (!m.WinnerId.HasValue)
                return Result.Failure("Winner must be set before completing match");
            return Result.CreateSuccess();
        },
    };
}
```

Execution helper that blends existing `TryTransition` with guards/actions:

```csharp
public static async Task<Result> ExecuteTransitionAsync(
    Match match,
    MatchStatus to,
    Guard<Match>? guard,
    ActionAsync<Match>? action,
    CancellationToken ct)
{
    if (guard is not null && !guard(match))
        return Result.Failure($"Guard rejected transition to {to}");

    if (action is not null)
    {
        var ar = await action(match, ct);
        if (!ar.Success)
            return ar;
    }

    // Defer to existing state write path
    var ok = new MatchCore.State.MatchState().TryTransition(
        match, to, Guid.Empty, "system");
    return ok ? Result.CreateSuccess() : Result.Failure("Invalid transition");
}
```

Usage example (Created → InProgress):

```csharp
var r = await ExecuteTransitionAsync(
    match,
    MatchStatus.InProgress,
    MatchTransitions.Guards[(MatchStatus.Created, MatchStatus.InProgress)],
    MatchTransitions.Actions[(MatchStatus.Created, MatchStatus.InProgress)],
    CancellationToken.None);
```

### Applying to Game transitions
Reuse the same delegate types; wire guards such as deck submissions confirmation:

```csharp
public static class GameTransitions
{
    public static readonly Dictionary<(GameStatus From, GameStatus To), Guard<Game>> Guards = new()
    {
        [(GameStatus.Created, GameStatus.InProgress)] = g => MatchCore.Accessors.IsReadyToStart(g),
    };
}
```

### Benefits
- **Explicitness**: Guards and actions are first-class, easy to audit.
- **Testability**: Unit tests can invoke guards/actions without bus or database access.
- **Compatibility**: Leaves existing `TryTransition` writes untouched; delegates orchestrate preconditions.


