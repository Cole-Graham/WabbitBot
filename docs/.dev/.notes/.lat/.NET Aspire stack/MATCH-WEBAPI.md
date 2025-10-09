## Match Web API skeleton and DTO layout

### Goals
- Provide HTTP endpoints for reading and mutating Match data without exposing EF entities.
- Keep Core no-DI rules: initialize `WabbitBotDbContextProvider` once at startup, use per-request contexts.
- Use DTOs under a `WabbitBot.Contracts` project to share shapes with desktop/browser/mobile.
- Publish domain events for lifecycle updates via CoreEventBus where appropriate. Do not perform CRUD via events.

### Project plan
- New project: `src/WabbitBot.WebApi` (Minimal API or MVC). Startup:
  - Build configuration: `appsettings*.json` + `WABBITBOT_` env vars.
  - Call `WabbitBotDbContextProvider.Initialize(configuration)`.
  - Optional: run migrations only in controlled environments (not recommended for public deployments).
  - Add basic middleware: exception handler, CORS, rate limiting, auth (JWT placeholder).

### Endpoints (v1)
Base route: `/api/v1/matches`

- `GET /api/v1/matches/{id}` → MatchDetailsDto
- `GET /api/v1/matches` (query: `parentId`, `parentType`, paging) → Paged<MatchSummaryDto>
- `POST /api/v1/matches` (CreateMatchRequest) → MatchDetailsDto
- `POST /api/v1/matches/{id}/start` (StartMatchRequest) → 202 Accepted + OperationResultDto
- `POST /api/v1/matches/{id}/complete` (CompleteMatchRequest) → MatchDetailsDto
- `GET /api/v1/matches/{id}/games` → List<GameSummaryDto>
- `GET /api/v1/matches/{id}/state` → List<MatchStateSnapshotDto>

Notes:
- Reads use EF with `.AsNoTracking()`.
- Writes validate state transitions via `MatchCore` and persist via EF, then publish Core events.
- For broader queries (leaderboards/history), prefer slice-specific endpoints.

### DTO layout (under `WabbitBot.Contracts`)

Recommended namespaces: `WabbitBot.Contracts.Matches`

Core shapes:
- `MatchSummaryDto` — list views
  - `Guid Id`, `Guid Team1Id`, `Guid Team2Id`, `TeamSize TeamSize`,
    `DateTime? StartedAt`, `DateTime? CompletedAt`, `Guid? WinnerId`, `MatchParentType? ParentType`, `Guid? ParentId`

- `MatchDetailsDto` — detail view
  - `Guid Id`, `Guid Team1Id`, `Guid Team2Id`, `TeamSize TeamSize`, `int BestOf`, `bool PlayToCompletion`,
    `DateTime? StartedAt`, `DateTime? CompletedAt`, `Guid? WinnerId`, `MatchParentType? ParentType`, `Guid? ParentId`,
    `IReadOnlyList<Guid> Team1PlayerIds`, `IReadOnlyList<Guid> Team2PlayerIds`,
    `IReadOnlyList<string> AvailableMaps`, `IReadOnlyList<string> Team1MapBans`, `IReadOnlyList<string> Team2MapBans`,
    `ulong? ChannelId`, `ulong? Team1ThreadId`, `ulong? Team2ThreadId`,
    `IReadOnlyList<MatchParticipantDto> Participants`, `IReadOnlyList<TeamOpponentEncounterDto> OpponentEncounters`,
    `IReadOnlyList<MatchStateSnapshotDto> StateHistory`

- `MatchParticipantDto`
  - `Guid TeamId`, `int TeamNumber`, `bool IsWinner`, `DateTime JoinedAt`, `IReadOnlyList<Guid> PlayerIds`

- `TeamOpponentEncounterDto`
  - `Guid TeamId`, `Guid OpponentId`, `int TeamSize`, `DateTime EncounteredAt`, `bool Won`

- `MatchStateSnapshotDto`
  - `DateTime Timestamp`, `Guid TriggeredByUserId`, `string TriggeredByUserName`,
    `int CurrentGameNumber`, `Guid? CurrentMapId`, `string? FinalScore`,
    `IReadOnlyList<string> AvailableMaps`, `IReadOnlyList<string> Team1MapBans`, `IReadOnlyList<string> Team2MapBans`,
    `bool Team1BansSubmitted`, `bool Team2BansSubmitted`, `bool Team1BansConfirmed`, `bool Team2BansConfirmed`,
    `IReadOnlyList<string> FinalMapPool`,
    `IReadOnlyDictionary<string, object> AdditionalData`

- `GameSummaryDto`
  - `Guid Id`, `Guid MatchId`, `Guid MapId`, `int GameNumber`, `TeamSize TeamSize`,
    `IReadOnlyList<Guid> Team1PlayerIds`, `IReadOnlyList<Guid> Team2PlayerIds`

- `Paged<T>` utility
  - `IReadOnlyList<T> Items`, `int Total`, `int Page`, `int PageSize`

Requests:
- `CreateMatchRequest`
  - `Guid ParentId`, `MatchParentType ParentType`, `TeamSize TeamSize`, `int BestOf`, `bool PlayToCompletion`,
    `Guid Team1Id`, `Guid Team2Id`, `IReadOnlyList<Guid> Team1PlayerIds`, `IReadOnlyList<Guid> Team2PlayerIds`,
    Optional: maps or let server compute

- `StartMatchRequest`
  - `Guid Team1Id`, `Guid Team2Id`, `IReadOnlyList<Guid> Team1PlayerIds`, `IReadOnlyList<Guid> Team2PlayerIds`

- `CompleteMatchRequest`
  - `Guid WinnerId`

- `OperationResultDto`
  - `bool Success`, `string? Message`

### Mapping guidance
- Avoid exposing EF navigation objects; map IDs and lists.
- For lists use `IReadOnlyList<T>`; for dictionaries `IReadOnlyDictionary<string, object>`.
- Use `DateTime` in UTC.
- String enums for client readability are acceptable; otherwise share enum types in Contracts if stable.

### Minimal API skeleton (excerpt)
```csharp
var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")}.json",
                 optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("WABBITBOT_")
    .Build();

WabbitBotDbContextProvider.Initialize(configuration);

var app = builder.Build();

app.MapGet("/api/v1/matches/{id}", async (Guid id) =>
{
    await using var db = WabbitBotDbContextProvider.CreateDbContext();
    var match = await db.Set<Match>()
        .AsNoTracking()
        .Include(m => m.Participants)
        .Include(m => m.OpponentEncounters)
        .Include(m => m.StateHistory)
        .FirstOrDefaultAsync(m => m.Id == id);
    if (match is null) return Results.NotFound();
    return Results.Ok(MatchMapper.ToDetailsDto(match));
});

app.MapPost("/api/v1/matches", async (CreateMatchRequest req) =>
{
    await using var db = WabbitBotDbContextProvider.CreateDbContext();
    var match = MatchCore.Factory.CreateMatch(req.ParentId, req.ParentType, req.TeamSize, req.BestOf, req.PlayToCompletion);
    match.Team1Id = req.Team1Id; match.Team2Id = req.Team2Id;
    match.Team1PlayerIds = req.Team1PlayerIds.ToList();
    match.Team2PlayerIds = req.Team2PlayerIds.ToList();
    await db.AddAsync(match);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/matches/{match.Id}", MatchMapper.ToDetailsDto(match));
});

// Mapper example (static)
static class MatchMapper
{
    public static MatchDetailsDto ToDetailsDto(Match m)
    {
        return new MatchDetailsDto(
            m.Id, m.Team1Id, m.Team2Id, m.TeamSize, m.BestOf, m.PlayToCompletion,
            m.StartedAt, m.CompletedAt, m.WinnerId, m.ParentType, m.ParentId,
            m.Team1PlayerIds, m.Team2PlayerIds,
            m.AvailableMaps, m.Team1MapBans, m.Team2MapBans,
            m.ChannelId, m.Team1ThreadId, m.Team2ThreadId,
            m.Participants.Select(p => new MatchParticipantDto(p.TeamId, p.TeamNumber, p.IsWinner, p.JoinedAt, p.PlayerIds)).ToList(),
            m.OpponentEncounters.Select(oe => new TeamOpponentEncounterDto(oe.TeamId, oe.OpponentId, oe.TeamSize, oe.EncounteredAt, oe.Won)).ToList(),
            m.StateHistory.Select(s => new MatchStateSnapshotDto(
                s.Timestamp, s.TriggeredByUserId, s.TriggeredByUserName,
                s.CurrentGameNumber, s.CurrentMapId, s.FinalScore,
                s.AvailableMaps, s.Team1MapBans, s.Team2MapBans,
                s.Team1BansSubmitted, s.Team2BansSubmitted, s.Team1BansConfirmed, s.Team2BansConfirmed,
                s.FinalMapPool, s.AdditionalData)).ToList()
        );
    }
}
```

### Auth, pagination, CORS (next)
- Add JWT bearer auth and role/permission checks per permission-handling rules.
- Add `page`, `pageSize` query params, cap limits.
- Configure CORS for browser, rate limit for public endpoints.

### Aspire orchestration
- In `WabbitBot.AppHost`:
```csharp
builder.AddProject<Projects.WabbitBot_WebApi>("api")
    .WithEnvironment("WABBITBOT_Bot__Database__Provider", "PostgreSQL")
    .WithEnvironment("WABBITBOT_Bot__Database__ConnectionString", pg.GetConnectionString());
```
- Desktop/Browser apps call `/api/v1/...` instead of DB directly.


