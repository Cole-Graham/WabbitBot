using WabbitBot.Common.Events;
using WabbitBot.Common.Events.EventInterfaces;

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

public record DiscordErrorEvent(Exception Exception) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}