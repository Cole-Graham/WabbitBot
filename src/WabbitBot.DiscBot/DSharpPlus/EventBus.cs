using WabbitBot.Common.Events;

namespace WabbitBot.DiscBot.DSharpPlus;

public class EventBus
{
    private readonly IGlobalEventBus _globalEventBus;

    public EventBus(IGlobalEventBus globalEventBus)
    {
        _globalEventBus = globalEventBus;
    }

    // We'll add event handlers here once we confirm the correct DSharpPlus 5.0 API usage
    public void RegisterEventHandlers(global::DSharpPlus.DiscordClient client)
    {
        // TODO: Add event handlers after confirming DSharpPlus 5.0 API
    }
}