using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-internal events for team management - not forwarded to GlobalEventBus
/// </summary>

/// <summary>
/// Event published when a team is created
/// </summary>
public partial record TeamCreatedEvent(
    string TeamId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a team is updated
/// </summary>
public partial record TeamUpdatedEvent(
    string TeamId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a team is archived
/// </summary>
public partial record TeamArchivedEvent(
    string TeamId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a team is deleted
/// </summary>
public partial record TeamDeletedEvent(
    string TeamId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a team is unarchived
/// </summary>
public partial record TeamUnarchivedEvent(
    string TeamId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business logic event for team member management - unique to Team entity
/// </summary>
public partial record TeamMemberAddedEvent(
    string TeamId,
    string PlayerId,
    TeamRole Role
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business logic event for team member management - unique to Team entity
/// </summary>
public partial record TeamMemberRemovedEvent(
    string TeamId,
    string PlayerId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business logic event for team captain changes - unique to Team entity
/// </summary>
public partial record TeamCaptainChangedEvent(
    string TeamId,
    string PreviousCaptainId,
    string NewCaptainId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business logic event for team manager status changes - unique to Team entity
/// </summary>
public partial record TeamManagerStatusChangedEvent(
    string TeamId,
    string PlayerId,
    bool IsTeamManager
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
