using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Models;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Common.Database;
using WabbitBot.Common.Data;
using WabbitBot.Common.Events;
using WabbitBot.Common.Utilities;

namespace WabbitBot.Core;

public static class Program
{
    // Static instances for global access
    private static readonly IGlobalEventBus GlobalEventBus;
    private static readonly ICoreEventBus CoreEventBus;
    private static readonly IGlobalErrorHandler GlobalErrorHandler;
    private static readonly ICoreErrorHandler CoreErrorHandler;
    private static IBotConfigurationService? ConfigurationService;

    // Static constructor for initialization of dependencies
    static Program()
    {
        // Initialize in dependency order
        GlobalEventBus = new GlobalEventBus();

        // Make GlobalEventBus available to other projects
        GlobalEventBusProvider.Initialize(GlobalEventBus);

        CoreEventBus = new CoreEventBus(GlobalEventBus);
        GlobalErrorHandler = new GlobalErrorHandler(GlobalEventBus);
        CoreErrorHandler = new CoreErrorHandler(CoreEventBus, GlobalEventBus);
    }

    public static async Task Main()
    {
        try
        {
            // Initialize event buses first
            await CoreEventBus.InitializeAsync();

            // Initialize error handling
            await GlobalErrorHandler.Initialize();
            await CoreErrorHandler.InitializeAsync();

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
            await InitializeCoreAsync(botOptions);

            // Publish startup event with configuration
            // This will be picked up by DiscBot through event handlers
            await GlobalEventBus.PublishAsync(new StartupInitiatedEvent(botOptions, ConfigurationService));

            // Signal that the core system is ready
            await CoreEventBus.PublishAsync(new CoreStartupCompletedEvent());

            // This will trigger DiscBot initialization through event handlers
            await GlobalEventBus.PublishAsync(new SystemReadyEvent());

            // Signal that the application is fully ready
            var startTime = DateTime.UtcNow;
            var startupDuration = DateTime.UtcNow - startTime;
            await GlobalEventBus.PublishAsync(new ApplicationReadyEvent(startupDuration, ConfigurationService)
            {
                ConfigurationService = ConfigurationService
            });

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

    private static async Task InitializeCoreAsync(BotOptions config)
    {
        try
        {
            // Initialize EF Core database
            var dbSettings = new DatabaseSettings
            {
                Provider = config.Database.Provider,
                ConnectionString = config.Database.ConnectionString,
                MaxPoolSize = config.Database.MaxPoolSize
            };

            // Initialize the DbContext provider
            WabbitBotDbContextProvider.Initialize(dbSettings.GetEffectiveConnectionString());

            // Create DbContext and run migrations
            using (var dbContext = WabbitBotDbContextProvider.CreateDbContext())
            {
                await dbContext.Database.MigrateAsync();
                await GlobalEventBus.PublishAsync(new DatabaseInitializedEvent());
            }

            // Initialize core services with their dependencies
            await InitializeCoreServices();

            // Initialize event handlers
            await InitializeEventHandlers();

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
            await CoreErrorHandler.HandleErrorAsync(ex, "Program startup error");
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
            await CoreErrorHandler.HandleErrorAsync(ex, "Failed to initialize core services");
            throw;
        }
    }

    /// <summary>
    /// Initializes all event handlers to register their subscriptions
    /// </summary>
    private static async Task InitializeEventHandlers()
    {
        try
        {
            // Initialize Common handlers
            await WabbitBot.Core.Common.Handlers.GameHandler.Instance.InitializeAsync();
            await WabbitBot.Core.Common.Handlers.PlayerHandler.Instance.InitializeAsync();
            await WabbitBot.Core.Common.Handlers.TeamHandler.Instance.InitializeAsync();
            await WabbitBot.Core.Common.Handlers.UserHandler.Instance.InitializeAsync();
            await WabbitBot.Core.Common.Handlers.ConfigurationHandler.Instance.InitializeAsync();
            await WabbitBot.Core.Common.Handlers.MapHandler.Instance.InitializeAsync();

            // Initialize Vertical Slice handlers (only those with Instance properties)
            await WabbitBot.Core.Leaderboards.LeaderboardHandler.Instance.InitializeAsync();
            await WabbitBot.Core.Leaderboards.SeasonHandler.Instance.InitializeAsync();
            await WabbitBot.Core.Matches.MatchHandler.Instance.InitializeAsync();
            await WabbitBot.Core.Scrimmages.ScrimmageHandler.Instance.InitializeAsync();
            await WabbitBot.Core.Scrimmages.ScrimmageRating.ProvenPotentialHandler.Instance.InitializeAsync();

            // Note: TournamentHandler and RatingCalculatorHandler
            // have complex constructors and need to be initialized differently
        }
        catch (Exception ex)
        {
            await CoreErrorHandler.HandleErrorAsync(ex, "Failed to initialize event handlers");
            throw;
        }
    }
}
