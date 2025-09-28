using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Models;
using WabbitBot.Common.ErrorService;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Database;
using WabbitBot.Common.Data;
using WabbitBot.Common.Utilities;
using WabbitBot.Core.Common.Utilities;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Tournaments;

namespace WabbitBot.Core;

public static class Program
{
    // Static instances for global access
    private static readonly IGlobalEventBus GlobalEventBus;
    private static readonly ICoreEventBus CoreEventBus;
    private static readonly IErrorService ErrorService;
    private static IBotConfigurationService? ConfigurationService;

    // Static constructor for initialization of dependencies
    static Program()
    {
        // Initialize in dependency order
        GlobalEventBus = new GlobalEventBus();

        // Make GlobalEventBus available to other projects
        GlobalEventBusProvider.Initialize(GlobalEventBus);

        CoreEventBus = new CoreEventBus(GlobalEventBus);
        ErrorService = new WabbitBot.Common.ErrorService.ErrorService(); // TODO: Inject this properly
    }

    public static async Task Main()
    {
        try
        {
            // Initialize event buses first
            await CoreEventBus.InitializeAsync();

            // Initialize error handling
            // await GlobalErrorHandler.Initialize(); // This line is removed as per the new_code
            // await CoreErrorHandler.InitializeAsync(); // This line is removed as per the new_code

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

            // Publish startup event (allow startup events to carry configuration references)
            await GlobalEventBus.PublishAsync(new StartupInitiatedEvent(botOptions, ConfigurationService));

            // Signal that the core system is ready
            await CoreEventBus.PublishAsync(new CoreStartupCompletedEvent());

            // This will trigger DiscBot initialization through event handlers
            await GlobalEventBus.PublishAsync(new SystemReadyEvent());

            // Signal that the application is fully ready
            await GlobalEventBus.PublishAsync(new ApplicationReadyEvent());

            // Keep the application running
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            // Handle startup errors through global error handler
            await (ErrorService as IGlobalErrorHandler)!.HandleError(ex); // TODO: Refactor error handling initialization
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

            // Initialize event handlers
            await InitializeEventHandlers(CoreEventBus, ErrorService);

            // Publish core services initialized event
            var initializedServices = new[] { "Database", "DatabaseServices", "CoreServices" };
            await CoreEventBus.PublishAsync(new CoreServicesInitializedEvent(initializedServices));

            // Initialize features
            var features = new[] { "Tournaments", "Matches", "Leaderboards" };
            foreach (var feature in features)
            {
                await CoreEventBus.PublishAsync(new CoreFeatureReadyEvent(feature));
            }
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
            // Database services are now initialized directly by CoreService
            // This maintains the no-runtime-dependency-injection design principle
        }
        catch (Exception ex)
        {
            // await CoreErrorHandler.HandleErrorAsync(ex, "Failed to initialize core services"); // This line is removed as per the new_code
            throw;
        }
    }

    /// <summary>
    /// Initializes all event handlers to register their subscriptions
    /// </summary>
    private static async Task InitializeEventHandlers(ICoreEventBus coreEventBus, IErrorService errorService)
    {
        try
        {
            // Instantiate and initialize all active handlers.
            // The static .Instance pattern is deprecated.
            var seasonHandler = new SeasonHandler(coreEventBus, errorService);
            await seasonHandler.InitializeAsync();

            // TODO: Instantiate other handlers as they are refactored.
            // The following handlers are legacy and have been removed or deprecated:
            // - GameHandler, PlayerHandler, TeamHandler, UserHandler, ConfigurationHandler, MapHandler
            // - LeaderboardHandler, MatchHandler, ScrimmageHandler, ProvenPotentialHandler
        }
        catch (Exception ex)
        {
            await errorService.CaptureAsync(ex, "Failed to initialize event handlers", nameof(InitializeEventHandlers));
            throw;
        }
    }
}
