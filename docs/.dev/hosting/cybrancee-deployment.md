
## 1) Create the bot server & open the panel

* Buy a **Discord Bot Hosting** plan, then log into the **Cybrancee panel** they email you. Their guide shows the panel flow and that it’s the same for C# as for other languages. ([Cybrancee][1])

## 2) Upload your build

* In your project root:

  ```bash
  dotnet publish -c Release -o out
  ```
* In the Cybrancee panel, grab your **SFTP** creds (Settings) and upload the contents of `out/` to your server space. Their guide walks through using SFTP. ([Cybrancee][1])

## 3) Pick the runtime & startup

* In **Startup**:

  * Choose a **Docker Image** for .NET (e.g., .NET 8/9) from the dropdown. ([Cybrancee][1])
  * Set the startup/entry to run your app, e.g.:

    ```
    dotnet WabbitBot.dll
    ```

  (They show picking language image + “bot file”/entry for other stacks; same idea for C#.) ([Cybrancee][1])

## 4) Provide secrets on Cybrancee

You’ve got two workable options there:

**Option A — `.env` file (what their docs emphasize):**

* Create a file named `.env` in the panel file manager and add:

  ```
  ASPNETCORE_ENVIRONMENT=Production
  Bot__Token=your-prod-token
  Bot__Database__ConnectionString=Host=...;Database=...;Username=...;Password=...;
  ```
* Since ASP.NET Core **doesn’t read `.env` by default**, add a tiny loader in `Program.cs`:

  ```csharp
  using DotNetEnv; // dotnet add package DotNetEnv
  var builder = WebApplication.CreateBuilder(args);
  Env.Load(); // loads .env into environment variables
  // builder.Configuration already reads env vars; no other change needed
  ```
* Cybrancee’s KB explains the `.env` approach (aimed at bots) and placing it in the panel. ([Cybrancee][2])

**Option B — Panel environment variables (if exposed in your server’s Startup/Variables UI):**

* Some Cybrancee bot servers expose startup variables; if you see that UI, add:

  * `ASPNETCORE_ENVIRONMENT = Production`
  * `Bot__Token = ...`
  * `Bot__Database__ConnectionString = ...`
* ASP.NET Core will pick them up automatically (no code change).

> Tip: Their article explicitly says the steps are “almost identical for … **C#**,” and shows the Docker Image selection and startup file fields you’ll use. ([Cybrancee][1])

## 5) Start it

* Go back to **Console** → **Start**. If you used `.env`, you should see your app boot with those values. (Their guide’s final step shows starting the bot.) ([Cybrancee][1])

---

If you want, I can generate a tiny **production-ready `Program.cs` snippet** (with `Env.Load()` and a quick fail-fast check for `Bot:Token` & the DB conn string) so you can paste it in and go.

[1]: https://cybrancee.com/learn/knowledge-base/how-to-host-a-discord-bot/ "How to Host a Discord Bot"
[2]: https://cybrancee.com/learn/knowledge-base/how-to-use-a-env-file-for-your-discord-bot/ "How to Use a .env File For Your Discord Bot"


Here’s a production-safe **Program.cs snippet** that works perfectly for Cybrancee (or any host that uses a `.env` file).
It automatically loads your `.env` file, binds settings, and fails fast if secrets are missing:

```csharp
using DotNetEnv;                      // dotnet add package DotNetEnv
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Load .env file first (for Cybrancee)
Env.Load();

// Build configuration (same order as ASP.NET defaults)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

// Retrieve critical settings
string? botToken = builder.Configuration["Bot:Token"];
string? connString = builder.Configuration["Bot:Database:ConnectionString"];

// Fail fast if missing
if (string.IsNullOrWhiteSpace(botToken))
    throw new InvalidOperationException("Missing Bot:Token. Set it in your .env or environment variables.");
if (string.IsNullOrWhiteSpace(connString))
    throw new InvalidOperationException("Missing Bot:Database:ConnectionString. Set it in your .env or environment variables.");

// Optional: log environment info
Console.WriteLine($"Running in {builder.Environment.EnvironmentName} mode.");

// Example startup (replace with your bot logic)
var app = builder.Build();
app.MapGet("/", () => "WabbitBot is running 🐇");
app.Run();
```

**How it works**

* `Env.Load()` pulls your `.env` values into the process environment.
* `builder.Configuration.AddEnvironmentVariables()` makes them visible to ASP.NET Core’s configuration system.
* Missing secrets trigger explicit errors instead of silent failures.
* Works equally well on Cybrancee, Docker, or any standard host.
