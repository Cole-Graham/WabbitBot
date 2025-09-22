using System;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.DiscBot.DiscBot.Base;

/// <summary>
/// Base class for all Discord-specific events that stay on DiscordEventBus
/// and should not be forwarded to the GlobalEventBus
/// </summary>
public abstract record DiscordBaseEvent : IEvent
{
    public EventBusType EventBusType => EventBusType.DiscBot;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string EventType => GetType().Name;
}
