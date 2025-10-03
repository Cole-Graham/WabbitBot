## Intra-boundary validation pipelines (Core) with delegates

### Goal
- Compose validation steps (team lookup, challenge checks, factory validation) without DI, returning `Result` as in the Hybrid pattern.
- Keep usage strictly inside Core; cross-boundary signals remain Events.

### Where this fits today
Core interface for scrimmage creation:

```csharp
    // 11:23:src/WabbitBot.Core/Common/Interfaces/IScrimmageCore.cs
    public interface IScrimmageCore : ICore
    {
        /// <summary>
        /// Creates a new scrimmage challenge
        /// </summary>
        Task<Result<Scrimmage>> CreateScrimmageAsync(
            Guid challengerTeamId,
            Guid opponentTeamId,
            List<Guid> challengerRosterIds,
            List<Guid> opponentRosterIds,
            TeamSize teamSize,
            int bestOf = 1);

        /// <summary>
        /// Accepts a scrimmage challenge
        /// </summary>
        Task<Result> AcceptScrimmageAsync(Guid scrimmageId);
    }
```

Factory building scrimmage entities:

```csharp
    // 34:52:src/WabbitBot.Core/Scrimmages/ScrimmageCore.cs
            public static Scrimmage CreateScrimmage(
                Guid team1Id,
                Guid team2Id,
                List<Guid> team1RosterIds,
                List<Guid> team2RosterIds,
                TeamSize teamSize,
                int bestOf = 1)
            {
                var scrimmage = new Scrimmage
                {
                    Team1Id = team1Id,
                    Team2Id = team2Id,
                    Team1RosterIds = team1RosterIds,
                    Team2RosterIds = team2RosterIds,
                    TeamSize = teamSize,
                    BestOf = bestOf,
                    ChallengeExpiresAt = DateTime.UtcNow.AddHours(24), // 24 hour challenge window
                };
                return scrimmage;
            }
```

### Delegate pattern
Define a small `ValidationStep<TContext>` and a pipeline executor that short-circuits on failure. No DI required.

```csharp
public delegate Task<Result> ValidationStep<TContext>(TContext context, CancellationToken ct);

public static class ValidationPipeline
{
    public static async Task<Result> RunValidationAsync<TContext>(
        TContext context,
        CancellationToken ct,
        params ValidationStep<TContext>[] steps)
    {
        foreach (var step in steps)
        {
            var result = await step(context, ct);
            if (!result.Success)
                return result;
        }
        return Result.CreateSuccess();
    }
}
```

### Concrete example: CreateScrimmage flow
Context mirrors `IScrimmageCore.CreateScrimmageAsync` inputs; validation steps are intra-Core.

```csharp
public sealed class CreateScrimmageContext
{
    public Guid ChallengerTeamId { get; init; }
    public Guid OpponentTeamId { get; init; }
    public List<Guid> ChallengerRosterIds { get; init; } = [];
    public List<Guid> OpponentRosterIds { get; init; } = [];
    public TeamSize TeamSize { get; init; }
    public int BestOf { get; init; } = 1;
}

static ValidationStep<CreateScrimmageContext> EnsureDifferentTeams = async (ctx, ct) =>
{
    if (ctx.ChallengerTeamId == ctx.OpponentTeamId)
        return Result.Failure("Teams must be different");
    return await Task.FromResult(Result.CreateSuccess());
};

static ValidationStep<CreateScrimmageContext> EnsureRosterCounts = async (ctx, ct) =>
{
    var required = (int)ctx.TeamSize;
    if (ctx.ChallengerRosterIds.Count < required || ctx.OpponentRosterIds.Count < required)
        return Result.Failure($"Each roster must have at least {required} players");
    return await Task.FromResult(Result.CreateSuccess());
};

static ValidationStep<CreateScrimmageContext> EnsureTeamsExist = async (ctx, ct) =>
{
    var team1 = await DatabaseService<Team>.GetByIdAsync(ctx.ChallengerTeamId, ct);
    var team2 = await DatabaseService<Team>.GetByIdAsync(ctx.OpponentTeamId, ct);
    if (team1 is null || team2 is null)
        return Result.Failure("One or both teams do not exist");
    return Result.CreateSuccess();
};

public static async Task<Result<Scrimmage>> CreateScrimmageValidatedAsync(CreateScrimmageContext ctx, CancellationToken ct)
{
    var vr = await ValidationPipeline.RunValidationAsync(
        ctx,
        ct,
        EnsureDifferentTeams,
        EnsureRosterCounts,
        EnsureTeamsExist);

    if (!vr.Success)
        return Result<Scrimmage>.Failure(vr.ErrorMessage);

    var scrimmage = ScrimmageCore.Factory.CreateScrimmage(
        ctx.ChallengerTeamId,
        ctx.OpponentTeamId,
        ctx.ChallengerRosterIds,
        ctx.OpponentRosterIds,
        ctx.TeamSize,
        ctx.BestOf);

    return Result<Scrimmage>.CreateSuccess(scrimmage);
}
```

### Benefits
- **Composability**: Add/remove checks without nested control flow.
- **Testability**: Steps are isolated functions over a context.
- **No DI**: Matches the project’s no-runtime-DI rule.
- **Hybrid-friendly**: Callers still return `Result` and can publish events on success.


