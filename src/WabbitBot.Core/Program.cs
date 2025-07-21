using System;
using System.Text.Json;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Models;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.BotCore;


namespace WabbitBot.Core;

public static class Program
{
    // Static instances for global access
    private static readonly IGlobalEventBus GlobalEventBus;
    private static readonly ICoreEventBus CoreEventBus;
    private static readonly IGlobalErrorHandler GlobalErrorHandler;
    private static readonly ICoreErrorHandler CoreErrorHandler;
    private static IBotConfigurationReader? ConfigurationReader;

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
            await CoreErrorHandler.Initialize();

            // Load configuration
            var config = await LoadConfigurationAsync();
            ConfigurationReader = new BotConfigurationReader(config);

            // Initialize core business logic
            await InitializeCoreAsync();

            // Publish startup event with configuration
            // This will be picked up by DiscBot through event handlers
            await GlobalEventBus.PublishAsync(new StartupInitiatedEvent(config, ConfigurationReader));

            // Signal that the core system is ready
            await CoreEventBus.PublishAsync(new CoreStartupCompletedEvent());

            // This will trigger DiscBot initialization through event handlers
            await GlobalEventBus.PublishAsync(new SystemReadyEvent());

            // Signal that the application is fully ready
            var startTime = DateTime.UtcNow;
            var startupDuration = DateTime.UtcNow - startTime;
            await GlobalEventBus.PublishAsync(new ApplicationReadyEvent(startupDuration, ConfigurationReader)
            {
                ConfigurationReader = ConfigurationReader
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

    private static async Task<BotConfiguration> LoadConfigurationAsync()
    {
        // Look for config file in project root (relative to executable location)
        var configPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config.json");
        var fullPath = Path.GetFullPath(configPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Configuration file not found at: {fullPath}");
        }

        var jsonString = await File.ReadAllTextAsync(fullPath);
        var config = JsonSerializer.Deserialize<BotConfiguration>(jsonString)
            ?? throw new InvalidOperationException("Failed to deserialize configuration");

        return config;
    }

    private static async Task InitializeCoreAsync()
    {
        try
        {
            // Initialize core services
            var initializedServices = new[] { "Database", "Cache", "EventBus" };
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
            await CoreErrorHandler.HandleError(ex);
            throw;
        }
    }
}
