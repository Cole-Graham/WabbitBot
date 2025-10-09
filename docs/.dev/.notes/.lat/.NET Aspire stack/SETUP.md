## .NET Aspire + Avalonia plan (Desktop, Browser now; Mobile-ready foundation)

### Scope
- Stand up .NET Aspire to orchestrate local dev for API + Postgres + dashboard.
- Add Avalonia UI for Desktop and Browser now; keep structure ready for later Mobile.
- Reuse existing Core/EF model via `WabbitBotDbContextProvider` (no runtime DI).

### Prerequisites
- .NET 9 SDK installed.
- Postgres available locally or via container.
- Templates:
  - `dotnet new install Microsoft.DotNet.Aspire.Templates`,
  - `dotnet new install Avalonia.Templates`,
  - Keep using solution-level `WabbitBot.Core` and `WabbitBot.Common` for shared logic.

### Configuration conventions
- Database config uses `Bot:Database` bound to `DatabaseSettings` in `WabbitBot.Core`:
  - `Provider`: "PostgreSQL",
  - `ConnectionString`: `Host=localhost;Port=5432;Database=wabbitbot;Username=wabbitbot;Password=...`,
  - Optional: `MaxPoolSize`, legacy `Path` (unused for Postgres).
- Environment prefix: `WABBITBOT_` (e.g., `WABBITBOT_Bot__Database__ConnectionString`).

### Solution layout (incremental)
- Keep existing projects intact.
- Add new projects:
  - AppHost: Orchestrates local dev using .NET Aspire.
  - Avalonia.Desktop: Cross-desktop app (Win/macOS/Linux).
  - Avalonia.Browser: WASM browser app.
  - (Later) Avalonia.Mobile: Android/iOS.

Target tree (new only):
```
src/
  WabbitBot.AppHost/              # .NET Aspire orchestrator
  WabbitBot.Avalonia.Desktop/     # Avalonia desktop app
  WabbitBot.Avalonia.Browser/     # Avalonia WebAssembly app
  # WabbitBot.Avalonia.Mobile/    # (later) Android/iOS
```

### Step 1 — Create AppHost (Aspire)
Commands (PowerShell):
```
cd src; dotnet new aspire-apphost -n WabbitBot.AppHost
```
Edit `WabbitBot.AppHost/Program.cs` to register:
- Postgres container (for local dev),
- Web API (future),
- Avalonia apps as projects (run/debug grouping),
- Aspire dashboard.

Minimal example skeleton (adjust names):
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Postgres for local dev (optional if you run your own)
var pg = builder.AddPostgres("pg")
    .WithImage("postgres", "16").WithContainerName("wabbitbot-pg")
    .WithVolume("wabbitbot-pg-data", "/var/lib/postgresql/data")
    .WithEnvironment("POSTGRES_USER", "wabbitbot")
    .WithEnvironment("POSTGRES_PASSWORD", "password123")
    .WithEnvironment("POSTGRES_DB", "wabbitbot");

// Connection string binding for other projects
var pgConn = pg.GetConnectionString();

// Register UI projects (these are solution project refs)
builder.AddProject<Projects.WabbitBot_Avalonia_Desktop>("desktop")
    .WithEnvironment("WABBITBOT_Bot__Database__Provider", "PostgreSQL")
    .WithEnvironment("WABBITBOT_Bot__Database__ConnectionString", pgConn);

builder.AddProject<Projects.WabbitBot_Avalonia_Browser>("browser")
    .WithEnvironment("WABBITBOT_Bot__Database__Provider", "PostgreSQL")
    .WithEnvironment("WABBITBOT_Bot__Database__ConnectionString", pgConn);

builder.Build().Run();
```

Notes:
- Use the same env key shape your Host uses: `WABBITBOT_Bot__Database__...`.
- If you do not want Aspire to run Postgres, remove the container block and set `WABBITBOT_Bot__Database__ConnectionString` to your local instance.

### Step 2 — Create Avalonia Desktop project
Commands:
```
cd src; dotnet new avalonia.app -n WabbitBot.Avalonia.Desktop
```
Project setup:
- Reference `WabbitBot.Core` and `WabbitBot.Common`.
- On startup, build configuration and call `WabbitBotDbContextProvider.Initialize(configuration)` once.
- Use MVVM; keep UI thin; call your Web API if/when added; otherwise read-only queries can use EF directly.

App bootstrap (pseudo):
```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables("WABBITBOT_")
    .Build();

WabbitBotDbContextProvider.Initialize(configuration);
// then start Avalonia app
```

### Step 3 — Create Avalonia Browser (WASM) project
Commands:
```
cd src; dotnet new avalonia.browser -n WabbitBot.Avalonia.Browser
```
Project setup:
- Reference `WabbitBot.Core` and `WabbitBot.Common` only for DTOs/view models that do not require platform services.
- Browsers cannot connect directly to Postgres; all data must flow via a Web API. Until the API exists, mock or stub services.
- Keep the same MVVM surface; swap implementations for data access (HTTP client instead of EF).

WASM caveats:
- No direct DB access; CORS, auth (JWT), and pagination required.
- Keep payload sizes small; lazy-load lists.

### Step 4 — Prepare for Mobile expansion (foundation only)
- Do not create mobile projects yet; keep shared assemblies UI‑agnostic:
  - `WabbitBot.Contracts` (DTOs and API shapes),
  - `WabbitBot.Client` (API client abstraction),
  - `WabbitBot.ViewModels` (MVVM without platform dependencies).
- Later commands:
```
cd src; dotnet new avalonia.android -n WabbitBot.Avalonia.Android
cd src; dotnet new avalonia.ios -n WabbitBot.Avalonia.iOS
```
- Mobile specifics: navigation stack, permissions, offline cache; prefer API access via `HttpClient` and shared DTOs.

### Step 5 — Web API (recommended before Browser/Mobile data)
- Add `WabbitBot.WebApi` minimal API project that:
  - Initializes `WabbitBotDbContextProvider` from `appsettings*.json` and `WABBITBOT_` env vars,
  - Exposes read endpoints (leaderboards, users) first,
  - Adds auth (JWT), rate limits, and pagination later.
- Aspire: `builder.AddProject<Projects.WabbitBot_WebApi>("api").WithReference(pg);`
- UI projects consume the API; desktop can optionally use EF for admin tooling.

### Step 6 — Local dev orchestration via Aspire
- Single startup: run `WabbitBot.AppHost`; it will:
  - Start Postgres (if configured),
  - Launch `WabbitBot.WebApi` (when added),
  - Launch `WabbitBot.Avalonia.Desktop` and/or provide URLs for `WabbitBot.Avalonia.Browser`.
- Dashboard provides health/logs; configure OpenTelemetry later.

### Step 7 — Build/Run commands (PowerShell)
```
cd "C:\Users\coleg\Projects\WabbitBot";
dotnet build; 
cd src; dotnet run --project WabbitBot.AppHost;
```

### Environment variable mapping examples
- Provider:
```
WABBITBOT_Bot__Database__Provider=PostgreSQL
```
- Connection string (Aspire wiring or manual):
```
WABBITBOT_Bot__Database__ConnectionString=Host=localhost;Port=5432;Database=wabbitbot;Username=wabbitbot;Password=password123
```

### Notes and guardrails
- No runtime DI in Core; keep `WabbitBotDbContextProvider` init at process start.
- Events are communication only; do not perform CRUD via events.
- Browser/mobile must use API (no direct DB access).
- Keep DTOs separate from EF entities (`WabbitBot.Contracts`).
- Use MVVM and avoid UI‑layer database coupling.

### Optional: Observability via Aspire
- Add `ServiceDefaults` class library to centralize telemetry defaults; reference from API.
- Configure OTEL exporters (console locally, OTLP later).
- Surface health checks in API; dashboard will display rollup.

### Roadmap checklist
- [ ] Create `WabbitBot.AppHost` and wire Postgres + env vars,
- [ ] Create `WabbitBot.Avalonia.Desktop` (init provider, basic window),
- [ ] Create `WabbitBot.Avalonia.Browser` (stub data services),
- [ ] Add `WabbitBot.WebApi` for shared data access (recommended),
- [ ] Introduce `WabbitBot.Contracts` and `WabbitBot.Client` for API surface,
- [ ] Add telemetry defaults and dashboard views in Aspire,
- [ ] Later: add Android/iOS projects and hook into AppHost.


