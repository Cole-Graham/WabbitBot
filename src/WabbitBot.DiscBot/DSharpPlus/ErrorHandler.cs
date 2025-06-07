using WabbitBot.Common.Events;

namespace WabbitBot.DiscBot.DSharpPlus;

public class ErrorHandler
{
    private readonly IGlobalEventBus _globalEventBus;

    public ErrorHandler(IGlobalEventBus globalEventBus)
    {
        _globalEventBus = globalEventBus;
    }

    public Task HandleError(Exception ex)
    {
        return _globalEventBus.PublishAsync(new DiscordErrorEvent(ex));
    }
}

public record DiscordErrorEvent(Exception Exception);