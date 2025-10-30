using WabbitBot.Common.Attributes;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-internal events for configuration management - not forwarded to GlobalEventBus
/// </summary>
[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Commands.ConfigurationCommands",
    subTargetClasses: ["WabbitBot.Core.Common.Handlers.ConfigurationHandler"]
)]
public partial record ConfigurationChanged(
    string ChangeDescription,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default
) : IEvent;

[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Commands.ConfigurationCommands",
    subTargetClasses: ["WabbitBot.Core.Common.Handlers.ConfigurationHandler"]
)]
public partial record ServerIdSet(
    ulong ServerId,
    string? PreviousServerId = null,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default
) : IEvent;

[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Commands.ConfigurationCommands",
    subTargetClasses: ["WabbitBot.Core.Common.Handlers.ConfigurationHandler"]
)]
public partial record ChannelConfigured(
    string ChannelType,
    ulong ChannelId,
    ulong? PreviousChannelId = null,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default
) : IEvent;

[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Commands.ConfigurationCommands",
    subTargetClasses: ["WabbitBot.Core.Common.Handlers.ConfigurationHandler"]
)]
public partial record RoleConfigured(
    string RoleType,
    ulong RoleId,
    ulong? PreviousRoleId = null,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default
) : IEvent;

[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Commands.ConfigurationCommands",
    subTargetClasses: ["WabbitBot.Core.Common.Handlers.ConfigurationHandler"]
)]
public partial record ThreadInactivityThresholdConfigured(
    int ThresholdMinutes,
    int? PreviousThresholdMinutes = null,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default
) : IEvent;
