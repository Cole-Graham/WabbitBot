using WabbitBot.Common.Configuration;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-internal events for configuration management - not forwarded to GlobalEventBus
/// </summary>
public partial record ConfigurationChangedEvent(
    BotOptions Configuration,
    string ChangeType = "",
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default) : IEvent;

public partial record ServerIdSetEvent(
    ulong ServerId,
    string? PreviousServerId = null,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default) : IEvent;

public partial record ChannelConfiguredEvent(
    string ChannelType,
    ulong ChannelId,
    ulong? PreviousChannelId = null,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default) : IEvent;

public partial record RoleConfiguredEvent(
    string RoleType,
    ulong RoleId,
    ulong? PreviousRoleId = null,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default) : IEvent;
