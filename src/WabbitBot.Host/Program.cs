using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorService;
using WabbitBot.Common.Utilities;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common.Utilities;

namespace WabbitBot.Host;

public static class Program
{
    // Static instances for global access
    private static readonly IGlobalEventBus GlobalEventBus;
    private static readonly ICoreEventBus CoreEventBus;
    private static readonly IErrorService ErrorService;
    private static readonly IGlobalErrorHandler GlobalErrorHandler;
    private static IBotConfigurationService? ConfigurationService;

    // Static constructor for initialization of dependencies
    static Program()
    {
        // Initialize in dependency order
        GlobalEventBus = new GlobalEventBus();

        // Make GlobalEventBus available to other projects
        GlobalEventBusProvider.Initialize(GlobalEventBus);

        CoreEventBus = new CoreEventBus(GlobalEventBus);
        ErrorService = new WabbitBot.Common.ErrorService.ErrorService();
        GlobalErrorHandler = new WabbitBot.Common.Events.GlobalErrorHandler(GlobalEventBus);
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
            var botOptions = configuration.GetSection(BotOptions.SectionName).Get<BotOptions>()
                ?? throw new InvalidOperationException("Failed to load bot configuration");

            // Create configuration service
            var optionsWrapper = Options.Create(botOptions);
            ConfigurationService = new BotConfigurationService(configuration, optionsWrapper);

            // Initialize static configuration provider
            WabbitBot.Common.Configuration.ConfigurationProvider.Initialize(ConfigurationService);

            // Initialize thumbnail utility
            ThumbnailUtility.Initialize(configuration);

            // Validate configuration
            ConfigurationService.ValidateConfiguration();

            // Initialize core business logic
            await InitializeCoreAsync(configuration);

            // Initialize DiscBot (Discord client bootstrap)
            await InitializeDiscBotAsync(ConfigurationService);

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
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables("WABBITBOT_")
            .Build();
    }

    private static async Task InitializeCoreAsync(IConfiguration configuration)
    {
        try
        {
            // Initialize the DbContext provider
            WabbitBotDbContextProvider.Initialize(configuration);

            // Create DbContext, run migrations, and validate schema version
            using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
            {
                await dbContext.Database.MigrateAsync();

                var versionTracker = new SchemaVersionTracker(dbContext);
                await versionTracker.ValidateCompatibilityAsync();

                await GlobalEventBus.PublishAsync(new DatabaseInitializedEvent());
            }

            // Initialize core services with their dependencies
            await InitializeCoreServices();

            // Publish core services initialized event
            var initializedServices = new[] { "Database", "DatabaseServices", "CoreServices", };
            await CoreEventBus.PublishAsync(new CoreServicesInitializedEvent(initializedServices));

            // Signal that the core system is ready
            await CoreEventBus.PublishAsync(new CoreStartupCompletedEvent());

            // Initialize features
            var features = new[] { "Tournaments", "Matches", "Leaderboards", };
            foreach (var feature in features)
            {
                await CoreEventBus.PublishAsync(new CoreFeatureReadyEvent(feature));
            }

            // Start archive retention on a timer from configuration
            _ = Task.Run(async () =>
            {
                var retentionOptions = WabbitBot.Common.Configuration.ConfigurationProvider
                    .GetSection<RetentionOptions>(RetentionOptions.SectionName);
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
                        await ErrorService.CaptureAsync(ex, "Archive retention timer failure", nameof(InitializeCoreAsync));
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
            // Database services setup
            CoreService.RegisterRepositoryAdapters();
            CoreService.RegisterCacheProviders();
            CoreService.RegisterArchiveProviders();

            // Initialize FileSystemService with explicit dependencies
            CoreService.InitializeFileSystemService(CoreEventBus, ErrorService);

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
    private static async Task InitializeDiscBotAsync(IBotConfigurationService config)
    {
        try
        {
            // Create DiscBotEventBus instance
            var discBotEventBus = new WabbitBot.DiscBot.DiscBotEventBus(GlobalEventBus);

            // Initialize DiscBot services first
            await WabbitBot.DiscBot.DSharpPlus.DiscBotBootstrap.InitializeServicesAsync(discBotEventBus, ErrorService);

            // Start the Discord client
            await WabbitBot.DiscBot.DSharpPlus.DiscBotBootstrap.StartDiscordClientAsync(config);
        }
        catch (Exception ex)
        {
            await ErrorService.CaptureAsync(ex, "Failed to initialize DiscBot", nameof(InitializeDiscBotAsync));
            throw;
        }
    }
}
