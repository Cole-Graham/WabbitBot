## WabbitBot.Contracts and Client Libraries Plan

### Goals
- Centralize API request/response DTOs for reuse by Desktop, Browser (WASM), and Mobile clients.
- Provide a lightweight HTTP client library with typed endpoints and auth hooks.
- Keep EF entities out of the contracts; map in the API layer.

### Projects
- `src/WabbitBot.Contracts/`
  - DTOs grouped by slice: `Matches`, `Leaderboards`, `Scrimmages`, `Tournaments`, `Users`, `Teams`, `Maps`.
  - Utilities: `Paged<T>`, error shapes, common enums (only if stable across clients).
- `src/WabbitBot.Client/`
  - Typed HTTP client for Web API endpoints; no runtime DI required.
  - Pluggable auth handler (JWT bearer), retry policy, and JSON options.

### DTO conventions
- `record` types where possible; immutable-by-default.
- `Guid` identifiers; `DateTime` in UTC.
- Lists as `IReadOnlyList<T>`; dictionaries as `IReadOnlyDictionary<TKey,TValue>`.
- Avoid leaking DB-only fields; prefer IDs over navigation objects.
- Versioned endpoints: `/api/v1/...`; breaking changes → new version.

### Namespaces
- `WabbitBot.Contracts.Matches`, `WabbitBot.Contracts.Teams`, etc.
- Keep enums stable; otherwise serialize as strings with `[JsonStringEnumConverter]` in clients.

### Packaging
- Pack as NuGet (internal feed or local source) to enable reuse across apps.
- Separate packages:
  - `WabbitBot.Contracts`
  - `WabbitBot.Client`

### Mapping (API layer)
- Static mappers per slice (e.g., `MatchMapper`) convert EF entities to DTOs.
- Validate nulls and navigation collections; project with `.Select(...)` to DTOs.

### Error shape
- `ProblemDetails` compatible model for errors.
- Client parses non-2xx into a typed error result.

### WabbitBot.Client design
- `WabbitBotApiClientOptions`:
  - `BaseAddress`, `AuthTokenProvider` (delegate), `JsonSerializerOptions`, `HttpMessageHandler?`.
- `WabbitBotApiClient` exposes slices:
  - `MatchesClient`, `TeamsClient`, `UsersClient`, etc.
- Each slice provides async methods with cancellation tokens.

Example skeleton:
```csharp
public sealed class WabbitBotApiClient
{
    private readonly HttpClient _http;

    public MatchesClient Matches { get; }

    public WabbitBotApiClient(WabbitBotApiClientOptions options)
    {
        var handler = options.HttpMessageHandler ?? new HttpClientHandler();
        _http = new HttpClient(handler) { BaseAddress = options.BaseAddress };
        Matches = new MatchesClient(_http, options);
    }
}

public sealed class MatchesClient
{
    private readonly HttpClient _http;
    private readonly WabbitBotApiClientOptions _options;

    public MatchesClient(HttpClient http, WabbitBotApiClientOptions options)
    {
        _http = http; _options = options;
    }

    public async Task<MatchDetailsDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v1/matches/{id}");
        await AddAuthAsync(req, ct);
        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode) return null; // map errors per guidelines
        return await res.Content.ReadFromJsonAsync<MatchDetailsDto>(_options.JsonSerializerOptions, ct);
    }

    private async Task AddAuthAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (_options.AuthTokenProvider is null) return;
        var token = await _options.AuthTokenProvider(ct);
        if (!string.IsNullOrWhiteSpace(token)) req.Headers.Authorization = new("Bearer", token);
    }
}

public sealed class WabbitBotApiClientOptions
{
    public required Uri BaseAddress { get; init; }
    public Func<CancellationToken, Task<string?>>? AuthTokenProvider { get; init; }
    public HttpMessageHandler? HttpMessageHandler { get; init; }
    public JsonSerializerOptions JsonSerializerOptions { get; init; } = new(JsonSerializerDefaults.Web);
}
```

### DTO folder sketch (Matches)
- `MatchSummaryDto.cs`
- `MatchDetailsDto.cs`
- `MatchParticipantDto.cs`
- `TeamOpponentEncounterDto.cs`
- `MatchStateSnapshotDto.cs`
- `GameSummaryDto.cs`
- Requests: `CreateMatchRequest.cs`, `StartMatchRequest.cs`, `CompleteMatchRequest.cs`
- Shared: `Paged.cs`, optionally shared enums if stable

### Aspire integration
- Reference `WabbitBot.WebApi` in `WabbitBot.AppHost`; clients point to the AppHost-provided URL in dev.
- Consider wiring an OTLP collector and Aspire dashboard for API traces.

### Roadmap
- [ ] Create `WabbitBot.Contracts` project and scaffold match DTOs,
- [ ] Create `WabbitBot.Client` with `MatchesClient` and options,
- [ ] Add basic error handling and retry (Polly) if desired,
- [ ] Pack to local NuGet source and consume from Desktop/Browser,
- [ ] Expand with Teams/Users DTOs and clients.


