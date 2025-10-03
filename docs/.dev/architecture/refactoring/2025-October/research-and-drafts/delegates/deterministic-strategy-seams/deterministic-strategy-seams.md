## Deterministic strategy seams for tests (Core/DiscBot)

### Goal
- Pass simple delegates like `Func<MapPool, string>` (or arrays) to make selection logic deterministic in tests, without DI.

### Existing usage to anchor
`GameApp` currently chooses a random map internally:

```csharp
    // 53:64:src/WabbitBot.DiscBot/App/GameApp.cs
    public async Task StartNextGameAsync(Guid matchId, int gameNumber, string[] remainingMaps)
    {
        // Choose a random map from remaining pool
        var chosenMap = ChooseRandomMap(remainingMaps);

        // Request per-game container creation; Renderer will create and post it
        await DiscBotService.PublishAsync(new GameContainerRequested(matchId, gameNumber, chosenMap));

        // Publish GameStarted to Global for Core/analytics
        await PublishGameStartedAsync(matchId, gameNumber, chosenMap);
    }
```

### Delegate seam
Inject a selection function as a method parameter (defaulting to current behavior). This avoids runtime DI and stays local.

```csharp
public static class MapSelection
{
    public static string ChooseRandom(string[] remainingMaps)
    {
        if (remainingMaps is null || remainingMaps.Length == 0)
            throw new InvalidOperationException("No maps available for selection");
        var random = new Random();
        return remainingMaps[random.Next(remainingMaps.Length)];
    }
}

public async Task StartNextGameAsync(
    Guid matchId,
    int gameNumber,
    string[] remainingMaps,
    Func<string[], string>? chooseMap = null)
{
    var selector = chooseMap ?? MapSelection.ChooseRandom;
    var chosenMap = selector(remainingMaps);
    await DiscBotService.PublishAsync(new GameContainerRequested(matchId, gameNumber, chosenMap));
    await PublishGameStartedAsync(matchId, gameNumber, chosenMap);
}
```

### Test example
In tests, supply a deterministic selector without touching global state:

```csharp
string DeterministicPick(string[] maps) => maps[0];

await gameApp.StartNextGameAsync(
    matchId: Guid.NewGuid(),
    gameNumber: 1,
    remainingMaps: new[] { "Echeneis", "Lagoon" },
    chooseMap: DeterministicPick);
```

### Other seams
- **Core**: In `MatchCore` helpers like `IsReadyToStart`, accept a delegate for readiness policy when needed, defaulting to current logic.
- **Renderer**: For asset resolution, pass small delegates that pick CDN vs attachment preference for A/B tests; default to `DiscBotService.AssetResolver` behavior.

### Benefits
- **Deterministic tests**: No randomness in assertions.
- **No DI**: Pure method parameters, defaulting to current behavior.
- **Minimal surface**: Only affects call sites that need determinism.


