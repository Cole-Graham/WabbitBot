## Example: Delegate seam around `IsReadyToStart` (Core Game state)

### Current implementation in Core
`IsReadyToStart` is derived from submitted and confirmed deck codes:

```csharp
// 108:123:src/WabbitBot.Core/Common/Models/Common/MatchCore.Game.cs
public static bool AreDeckCodesSubmitted(Game game)
{
    var snapshot = GetCurrentSnapshot(game);
    return !string.IsNullOrEmpty(snapshot.Team1DeckCode) && !string.IsNullOrEmpty(snapshot.Team2DeckCode);
}

public static bool AreDeckCodesConfirmed(Game game)
{
    var snapshot = GetCurrentSnapshot(game);
    return snapshot.Team1DeckConfirmed && snapshot.Team2DeckConfirmed;
}

public static bool IsReadyToStart(Game game)
{
    return AreDeckCodesSubmitted(game) && AreDeckCodesConfirmed(game);
}
```

### Delegate seam for deterministic tests and policy toggles
Rather than changing Core code, expose a small wrapper that allows a caller (tests or flows) to swap readiness policy without DI.

```csharp
// Simple policy delegate. Default mirrors current Accessors behavior.
public static class GameReadinessPolicy
{
    public static bool Default(Game game)
        => MatchCore.Accessors.AreDeckCodesSubmitted(game)
        && MatchCore.Accessors.AreDeckCodesConfirmed(game);
}

public static class GameReadiness
{
    public static bool IsReady(Game game, Func<Game, bool>? policy = null)
    {
        var p = policy ?? GameReadinessPolicy.Default;
        return p(game);
    }
}
```

### Usage in state-machine guard
Wire the guard to the wrapper for clarity and easy test control:

```csharp
public static class GameTransitions
{
    public static readonly Dictionary<(GameStatus From, GameStatus To), Guard<Game>> Guards = new()
    {
        [(GameStatus.Created, GameStatus.InProgress)] = g => GameReadiness.IsReady(g),
    };
}
```

### Deterministic test example
Force readiness true/false regardless of underlying snapshot content.

```csharp
// Always-ready policy for positive-path tests
static bool AlwaysReady(Game g) => true;

// Never-ready policy for negative-path tests
static bool NeverReady(Game g) => false;

// Arrange
var game = new Game { /* minimal fields set */ };

// Act + Assert
Assert.True(GameReadiness.IsReady(game, AlwaysReady));
Assert.False(GameReadiness.IsReady(game, NeverReady));
```

### Why this helps
- Keeps Core logic unchanged while enabling deterministic tests.
- Avoids runtime DI; uses method parameters only.
- Guard logic remains explicit and auditable.


