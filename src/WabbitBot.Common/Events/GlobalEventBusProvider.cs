namespace WabbitBot.Common.Events;

public static class GlobalEventBusProvider
{
    private static IGlobalEventBus? _instance;

    public static void Initialize(IGlobalEventBus eventBus)
    {
        _instance = eventBus;
    }

    public static IGlobalEventBus GetGlobalEventBus()
    {
        return _instance
            ?? throw new InvalidOperationException("GlobalEventBus has not been initialized. Call Initialize() first.");
    }
}
