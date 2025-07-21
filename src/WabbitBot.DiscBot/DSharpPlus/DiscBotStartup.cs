using WabbitBot.Common.Events;
using WabbitBot.DiscBot.DiscBot.Events;
using WabbitBot.Common.Configuration;

namespace WabbitBot.DiscBot.DSharpPlus;

public static class DiscBotStartup
{
    private static IBotConfigurationReader? _configReader;

    // This will be called when the assembly loads
    static DiscBotStartup()
    {
        // This is a simple initialization to get handlers registered
        // The actual handler instances will subscribe to events
        RegisterEventHandlers();
    }

    private static void RegisterEventHandlers()
    {
        // Get the global event bus from Common provider
        var globalEventBus = GlobalEventBusProvider.GetGlobalEventBus();

        // Subscribe to startup events
        globalEventBus.Subscribe<StartupInitiatedEvent>(OnStartupInitiated);
    }

    private static Task OnStartupInitiated(StartupInitiatedEvent evt)
    {
        _configReader = evt.ConfigurationReader;
        _ = new DiscordEventHandler(GlobalEventBusProvider.GetGlobalEventBus(), _configReader);
        return Task.CompletedTask;
    }
}