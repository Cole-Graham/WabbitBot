using WabbitBot.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-internal events for configuration management - not forwarded to GlobalEventBus
/// </summary>
public partial class ConfigurationChangedEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public BotOptions Configuration { get; set; } = null!;
    public string ChangeType { get; set; } = string.Empty; // "Save", "Import", "Export"
}

public partial class ServerIdSetEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public ulong ServerId { get; set; }
    public string? PreviousServerId { get; set; }
}

public partial class ChannelConfiguredEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string ChannelType { get; set; } = string.Empty; // "bot", "replay", "deck", etc.
    public ulong ChannelId { get; set; }
    public ulong? PreviousChannelId { get; set; }
}

public partial class RoleConfiguredEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string RoleType { get; set; } = string.Empty; // "whitelisted", "admin", "moderator"
    public ulong RoleId { get; set; }
    public ulong? PreviousRoleId { get; set; }
}
