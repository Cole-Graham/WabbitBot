using DSharpPlus;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.DiscBot.DiscBot.Events;

/// <summary>
/// Discord-internal event published when the Discord client is ready and commands can be registered
/// This event stays on DiscordEventBus and is not forwarded to GlobalEventBus
/// </summary>
public record DiscordClientReadyEvent : IEvent
{
    public EventBusType EventBusType => EventBusType.DiscBot;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public DiscordClient Client { get; init; } = null!;
}
