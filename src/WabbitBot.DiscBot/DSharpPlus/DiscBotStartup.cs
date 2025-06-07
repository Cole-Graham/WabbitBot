using WabbitBot.Common.Events;
using WabbitBot.DiscBot.DiscBot.Events;

namespace WabbitBot.DiscBot.DSharpPlus;

public static class DiscBotStartup
{
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

        // Create and register handlers - their constructors will subscribe to events
        _ = new DiscordEventHandler(globalEventBus, config);
    }
}

// This class provides access to the GlobalEventBus singleton
// It would typically be provided by a more sophisticated mechanism in a real app
public static class GlobalEventBusAccessor
{
    private static IGlobalEventBus? _globalEventBus;

    public static void Initialize(IGlobalEventBus globalEventBus)
    {
        _globalEventBus = globalEventBus;
    }

    public static IGlobalEventBus GetGlobalEventBus()
    {
        return _globalEventBus ?? throw new InvalidOperationException("GlobalEventBus not initialized");
    }
}