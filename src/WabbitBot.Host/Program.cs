using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Events;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.Utilities;

namespace WabbitBot.Host;

public static class Program
{
    // Static instances for global access
    private static readonly GlobalEventBus GlobalEventBus;
    private static readonly CoreEventBus CoreEventBus;
    private static readonly ErrorService ErrorService;
    private static readonly GlobalErrorHandler GlobalErrorHandler;
    private static BotConfigurationService? ConfigurationService;

    // Static constructor for initialization of dependencies
    static Program()
    {
        // Initialize in dependency order
        GlobalEventBus = new GlobalEventBus();

        // Make GlobalEventBus available to other projects
        GlobalEventBusProvider.Initialize(GlobalEventBus);

        CoreEventBus = new CoreEventBus(GlobalEventBus);
        ErrorService = new ErrorService();
        GlobalErrorHandler = new GlobalErrorHandler(GlobalEventBus);
    }

    public static async Task Main()
    {
        try
        {
            // Initialize event buses first
            await CoreEventBus.InitializeAsync();
            await GlobalErrorHandler.Initialize();

            // Load modern configuration
            var configuration = BuildConfiguration();

            // Echo current environment for visibility
            var currentEnv =
                configuration["ASPNETCORE_ENVIRONMENT"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Development";
            Console.WriteLine($"🌍 Current environment: {currentEnv}");

            // Fail fast on missing critical secrets (Discord bot token)
            var token = configuration["Bot:Token"];
            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException(
                    "Missing Bot:Token. Provide it via user-secrets (Dev), .env file, or environment variables (Production)."
                );

            var botOptions =
                configuration.GetSection(BotOptions.SectionName).Get<BotOptions>()
                ?? throw new InvalidOperationException("Failed to load bot configuration");

            // Create configuration service
            var optionsWrapper = Options.Create(botOptions);
            ConfigurationService = new BotConfigurationService(configuration, optionsWrapper);

            // Initialize static configuration provider
            Common.Configuration.ConfigurationProvider.Initialize(ConfigurationService);

            // Validate configuration
            ConfigurationService.ValidateConfiguration();

            // Initialize core business logic
            await InitializeCoreAsync(configuration);

            // Initialize DiscBot (Discord client bootstrap)
            await InitializeDiscBotAsync(ConfigurationService, currentEnv);

            // Seed development data after Discord is connected (Development only)
            if (string.Equals(currentEnv, "Development", StringComparison.OrdinalIgnoreCase))
            {
                await SeedDevelopmentDataAsync();
            }

            // Publish startup event (allow startup events to carry configuration references)
            await GlobalEventBus.PublishAsync(new StartupInitiatedEvent(botOptions, ConfigurationService));

            // This will trigger any remaining event handlers
            await GlobalEventBus.PublishAsync(new SystemReadyEvent());

            // Signal that the application is fully ready
            await GlobalEventBus.PublishAsync(new ApplicationReadyEvent());

            // Keep the application running
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            // Handle startup errors through global error handler
            await GlobalErrorHandler.HandleError(ex);
            throw;
        }
    }

    private static IConfiguration BuildConfiguration()
    {
        // 1) Load .env first so its values are present as environment variables
        //    On VPS: .env file in app directory OR systemd EnvironmentFile directive
        //    The .env file is optional; environment variables can be provided directly
        try
        {
            Env.Load();
        }
        catch (FileNotFoundException)
        {
            // .env is optional - environment variables may be set via systemd or shell
            Console.WriteLine("⚠️  No .env file found; using system environment variables only.");
        }

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            // 2) Base JSON (checked in)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            // 3) Environment JSON (Dev overrides only; file is optional/ignored in git)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);

        // 4) User Secrets (local dev only)
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            builder.AddUserSecrets(typeof(Program).Assembly, optional: true);
        }

        // 5) Environment variables (supports Bot__Token, etc.)
        builder.AddEnvironmentVariables();

        // (Optional) command-line args could be added here if you ever pass overrides

        var config = builder.Build();

        // Persist the resolved environment in config so we can print it even if only set via default
        var dict = new Dictionary<string, string?> { ["ASPNETCORE_ENVIRONMENT"] = environment };
        return new ConfigurationBuilder().AddConfiguration(config).AddInMemoryCollection(dict).Build();
    }

    private static async Task InitializeCoreAsync(IConfiguration configuration)
    {
        try
        {
            // Initialize the DbContext provider
            WabbitBotDbContextProvider.Initialize(configuration);

            // Determine environment
            var environment =
                configuration["ASPNETCORE_ENVIRONMENT"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? "Production";

            // Create DbContext and initialize database
            using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
            {
                var useEnsureCreated =
                    configuration.GetValue<bool?>("Bot:Database:UseEnsureCreated")
                    ?? configuration.GetValue<bool>("Database:UseEnsureCreated");
                var runMigrationsOnStartup =
                    configuration.GetValue<bool?>("Bot:Database:RunMigrationsOnStartup")
                    ?? configuration.GetValue<bool>("Database:RunMigrationsOnStartup");

                if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
                {
                    if (useEnsureCreated)
                    {
                        await dbContext.Database.EnsureCreatedAsync();
                    }
                    else
                    {
                        await dbContext.Database.MigrateAsync();
                    }
                }
                else
                {
                    if (runMigrationsOnStartup)
                    {
                        await dbContext.Database.MigrateAsync();
                    }
                }

                // Validate schema version compatibility (after any migration path)
                var versionTracker = new SchemaVersionTracker(dbContext);
                var currentSchemaVersion = await versionTracker.GetCurrentSchemaVersionAsync();
                if (!string.Equals(currentSchemaVersion, "000-0.0", StringComparison.Ordinal))
                {
                    await versionTracker.ValidateCompatibilityAsync();
                }

                await GlobalEventBus.PublishAsync(new DatabaseInitializedEvent());
            }

            // Initialize core services with their dependencies
            await InitializeCoreServices();

            // Publish core services initialized event
            var initializedServices = new[] { "Database", "DatabaseServices", "CoreServices" };
            await CoreEventBus.PublishAsync(new CoreServicesInitializedEvent(initializedServices));

            // Signal that the core system is ready
            await CoreEventBus.PublishAsync(new CoreStartupCompletedEvent());

            // Initialize features
            var features = new[] { "Tournaments", "Matches", "Leaderboards" };
            foreach (var feature in features)
            {
                await GlobalEventBus.PublishAsync(new CoreFeatureReadyEvent(feature));
            }

            // Start archive retention on a timer from configuration
            _ = Task.Run(async () =>
            {
                var retentionOptions = Common.Configuration.ConfigurationProvider.GetSection<RetentionOptions>(
                    RetentionOptions.SectionName
                );
                var retention = TimeSpan.FromDays(Math.Max(1, retentionOptions.ArchiveRetentionDays));
                var interval = TimeSpan.FromHours(Math.Max(1, retentionOptions.JobIntervalHours));
                while (true)
                {
                    try
                    {
                        await CoreService.RunArchiveRetentionAsync(retention);
                    }
                    catch (Exception ex)
                    {
                        await ErrorService.CaptureAsync(
                            ex,
                            "Archive retention timer failure",
                            nameof(InitializeCoreAsync)
                        );
                    }
                    await Task.Delay(interval);
                }
            });
        }
        catch (Exception ex)
        {
            await ErrorService.CaptureAsync(ex, "Program startup error", nameof(InitializeCoreAsync));
            throw;
        }
    }

    /// <summary>
    /// Initializes core services with their dependencies
    /// </summary>
    private static async Task InitializeCoreServices()
    {
        try
        {
            // Initialize CoreService with event bus and error handler (must be done first)
            CoreService.Initialize(CoreEventBus, ErrorService);

            // Database services setup
            CoreService.RegisterRepositoryAdapters();
            CoreService.RegisterCacheProviders();
            CoreService.RegisterArchiveProviders();

            // Get storage configuration
            var storageOptions = Common.Configuration.ConfigurationProvider.GetSection<StorageOptions>(
                StorageOptions.SectionName
            );

            // Initialize FileSystemService with explicit dependencies
            CoreService.InitializeFileSystemService(storageOptions, CoreEventBus, ErrorService);

            // Initialize Core event handlers (subscribe to CoreEventBus)
            Core.Scrimmages.ScrimmageHandler.Initialize();
            Core.Common.Models.Common.GameHandler.Initialize();
            Core.Common.Models.Common.MatchHandler.Initialize();

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            await ErrorService.CaptureAsync(ex, "Failed to initialize core services", nameof(InitializeCoreServices));
            throw;
        }
    }

    /// <summary>
    /// Initializes DiscBot by calling the bootstrap entry point in WabbitBot.DiscBot.DSharpPlus.
    /// This keeps all DSharpPlus code out of Core.
    /// </summary>
    private static async Task InitializeDiscBotAsync(IBotConfigurationService config, string environment)
    {
        try
        {
            // Create DiscBotEventBus instance
            var discBotEventBus = new DiscBot.DiscBotEventBus(GlobalEventBus);

            // Initialize DiscBot services first
            await DiscBot.App.DiscBotBootstrap.InitializeServicesAsync(discBotEventBus, ErrorService);

            // Start the Discord client
            await DiscBot.App.DiscBotBootstrap.StartDiscordClientAsync(config, environment);
        }
        catch (Exception ex)
        {
            await ErrorService.CaptureAsync(ex, "Failed to initialize DiscBot", nameof(InitializeDiscBotAsync));
            throw;
        }
    }

    /// <summary>
    /// Seeds development data by fetching actual Discord user information.
    /// Must be called after Discord client is connected.
    /// </summary>
    private static async Task SeedDevelopmentDataAsync()
    {
        try
        {
            Console.WriteLine("🌱 Preparing to seed development data with Discord user info...");

            // Get Discord client from DiscBot service
            var client = DiscBot.App.Services.DiscBot.DiscBotService.Client;

            // Define test Discord user IDs (same as before)
            var alphaTeamUserIds = new List<ulong> { 1348719242882584689, 1348724033306366055 };
            var bravoTeamUserIds = new List<ulong> { 1348724778906681447, 1348725467422916749 };

            // Fetch Discord user info
            var alphaResult = await DiscBot.App.Utilities.DiscordUserInfoFetcher.FetchUserInfosPartialAsync(
                client,
                alphaTeamUserIds
            );
            var bravoResult = await DiscBot.App.Utilities.DiscordUserInfoFetcher.FetchUserInfosPartialAsync(
                client,
                bravoTeamUserIds
            );

            // Check if we got any users
            if (!alphaResult.Success && !bravoResult.Success)
            {
                Console.WriteLine("⚠️  Could not fetch any Discord user info, using placeholder data...");
                using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
                {
                    await DevelopmentDataSeeder.SeedWithDefaultDataAsync(dbContext);
                }
                return;
            }

            // Seed with actual Discord data (or empty list for failed fetches)
            using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
            {
                await DevelopmentDataSeeder.SeedAsync(
                    dbContext,
                    alphaResult.Data ?? new List<Common.Models.DiscordUserInfo>(),
                    bravoResult.Data ?? new List<Common.Models.DiscordUserInfo>()
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Failed to seed with Discord data, falling back to placeholder data: {ex.Message}");
            await ErrorService.CaptureAsync(
                ex,
                "Failed to seed development data with Discord info",
                nameof(SeedDevelopmentDataAsync)
            );

            // Fallback to placeholder seeding
            try
            {
                using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
                {
                    await DevelopmentDataSeeder.SeedWithDefaultDataAsync(dbContext);
                }
            }
            catch (Exception fallbackEx)
            {
                await ErrorService.CaptureAsync(
                    fallbackEx,
                    "Failed to seed development data with placeholder data",
                    nameof(SeedDevelopmentDataAsync)
                );
            }
        }
    }
}
