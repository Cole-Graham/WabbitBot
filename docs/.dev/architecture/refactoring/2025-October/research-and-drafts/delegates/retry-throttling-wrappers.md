## Retry/throttling wrappers for Discord I/O (DiscBot DSharpPlus layer)

### Goal
- Wrap fragile Discord API calls with a local retry/throttle policy using delegates: `Func<Task<Result>>`.
- Keep it strictly in `src/WabbitBot.DiscBot/DSharpPlus/` and avoid DI.

### Context in current code
Renderer and handler patterns already isolate DSharpPlus calls and return `Result`:

```csharp
// 24:53:src/WabbitBot.DiscBot/DSharpPlus/Renderers/MatchRenderer.cs
public static async Task<Result> RenderMatchThreadAsync(
    DiscordClient client,
    DiscordChannel channel,
    Guid matchId)
{
    try
    {
        var threadName = $"Match {matchId:N}"; // Use short GUID format
        var thread = await channel.CreateThreadAsync(
            threadName,
            DiscordAutoArchiveDuration.Day,
            DiscordChannelType.PublicThread);

        await DiscBotService.PublishAsync(new MatchThreadCreated(
            matchId,
            thread.Id));

        return Result.CreateSuccess("Match thread created");
    }
    catch (Exception ex)
    {
        await DiscBotService.ErrorHandler.CaptureAsync(
            ex,
            $"Failed to render match thread for {matchId}",
            nameof(RenderMatchThreadAsync));
        return Result.Failure($"Failed to create match thread: {ex.Message}");
    }
}
```

### Delegate-based retry helper
Implement a small retry with backoff and an optional throttle gate. Keep conservative defaults.

```csharp
public static class IoPolicy
{
    public static async Task<Result> RetryAsync(
        Func<Task<Result>> operation,
        int maxAttempts = 3,
        TimeSpan? initialDelay = null,
        Func<int, TimeSpan>? backoff = null,
        CancellationToken ct = default)
    {
        if (operation is null) return Result.Failure("operation was null");
        var delay0 = initialDelay ?? TimeSpan.FromMilliseconds(300);
        var backoffFn = backoff ?? (attempt => TimeSpan.FromMilliseconds(delay0.TotalMilliseconds * Math.Pow(2, attempt - 1)));

        Exception? lastEx = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var r = await operation();
                if (r.Success) return r;
                // Non-success: retry
            }
            catch (Exception ex)
            {
                lastEx = ex;
                await DiscBotService.ErrorHandler.CaptureAsync(ex, $"Retry attempt {attempt} failed", nameof(RetryAsync));
            }

            if (attempt < maxAttempts)
            {
                var wait = backoffFn(attempt);
                await Task.Delay(wait, ct);
            }
        }

        return Result.Failure(lastEx is null ? "Operation failed after retries" : $"{lastEx.GetType().Name}: {lastEx.Message}");
    }
}
```

### Concrete usage in a renderer
Wrap `CreateThreadAsync` under retry without modifying higher-level flow:

```csharp
public static async Task<Result> RenderMatchThreadWithRetryAsync(
    DiscordClient client,
    DiscordChannel channel,
    Guid matchId,
    CancellationToken ct = default)
{
    return await IoPolicy.RetryAsync(async () =>
    {
        var threadName = $"Match {matchId:N}";
        var thread = await channel.CreateThreadAsync(
            threadName,
            DiscordAutoArchiveDuration.Day,
            DiscordChannelType.PublicThread);

        await DiscBotService.PublishAsync(new MatchThreadCreated(matchId, thread.Id));
        return Result.CreateSuccess();
    },
    maxAttempts: 3,
    initialDelay: TimeSpan.FromMilliseconds(400),
    backoff: attempt => TimeSpan.FromMilliseconds(400 * Math.Pow(2, attempt - 1)),
    ct: ct);
}
```

### Benefits
- Resilience: handles transient Discord/HTTP hiccups.
- Local-only: remains in the DSharpPlus layer.
- Uniform: same helper covers send, edit, thread creation, and DM sends.


