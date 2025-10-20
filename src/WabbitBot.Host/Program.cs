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
            WabbitBot.Common.Configuration.ConfigurationProvider.Initialize(ConfigurationService);

            // Validate configuration
            ConfigurationService.ValidateConfiguration();

            // Initialize core business logic
            await InitializeCoreAsync(configuration);

            // Initialize DiscBot (Discord client bootstrap)
            await InitializeDiscBotAsync(ConfigurationService, currentEnv);

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
                if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
                {
                    // Development: Use EnsureCreated for rapid prototyping
                    // This creates the database from scratch without migrations
                    await dbContext.Database.EnsureCreatedAsync();

                    // Seed development data
                    await DevelopmentDataSeeder.SeedAsync(dbContext);
                }
                else
                {
                    // Production/Staging: Use migrations for proper schema versioning
                    await dbContext.Database.MigrateAsync();

                    // Validate schema version compatibility
                    var versionTracker = new SchemaVersionTracker(dbContext);
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
                var retentionOptions =
                    WabbitBot.Common.Configuration.ConfigurationProvider.GetSection<RetentionOptions>(
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
            var storageOptions =
                WabbitBot.Common.Configuration.ConfigurationProvider.GetSection<WabbitBot.Common.Configuration.StorageOptions>(
                    WabbitBot.Common.Configuration.StorageOptions.SectionName
                );

            // Initialize FileSystemService with explicit dependencies
            CoreService.InitializeFileSystemService(storageOptions, CoreEventBus, ErrorService);

            // Initialize Core event handlers (subscribe to CoreEventBus)
            WabbitBot.Core.Scrimmages.ScrimmageHandler.Initialize();
            WabbitBot.Core.Common.Models.Common.GameHandler.Initialize();
            WabbitBot.Core.Common.Models.Common.MatchHandler.Initialize();

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
            var discBotEventBus = new WabbitBot.DiscBot.DiscBotEventBus(GlobalEventBus);

            // Initialize DiscBot services first
            await WabbitBot.DiscBot.App.DiscBotBootstrap.InitializeServicesAsync(discBotEventBus, ErrorService);

            // Start the Discord client
            await WabbitBot.DiscBot.App.DiscBotBootstrap.StartDiscordClientAsync(config, environment);
        }
        catch (Exception ex)
        {
            await ErrorService.CaptureAsync(ex, "Failed to initialize DiscBot", nameof(InitializeDiscBotAsync));
            throw;
        }
    }
}
