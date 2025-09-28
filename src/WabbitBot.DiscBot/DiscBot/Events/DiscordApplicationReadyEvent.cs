using WabbitBot.Common.Configuration;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.DiscBot.DiscBot.Events;

/// <summary>
/// Discord-internal event published when the Discord application is fully ready
/// This event stays on DiscordEventBus and is not forwarded to GlobalEventBus
/// </summary>
public record DiscordApplicationReadyEvent : IEvent
{
    public EventBusType EventBusType => EventBusType.DiscBot;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public TimeSpan StartupDuration { get; init; }
    public IBotConfigurationService ConfigurationService { get; init; } = null!;

    public DiscordApplicationReadyEvent(TimeSpan startupDuration, IBotConfigurationService configService)
    {
        StartupDuration = startupDuration;
        ConfigurationService = configService;
    }
}
