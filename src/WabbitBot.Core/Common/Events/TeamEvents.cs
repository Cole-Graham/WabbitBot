using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-internal events for team management - not forwarded to GlobalEventBus
/// </summary>

// CRUD events removed: TeamCreatedEvent, TeamUpdatedEvent, TeamArchivedEvent, TeamDeletedEvent, TeamUnarchivedEvent
// These were database operations and violate the critical principle that events are not for CRUD.

/// <summary>
/// Business logic event for team member management - unique to Team entity
/// </summary>
public partial record TeamMemberAddedEvent(
    Guid TeamId,
    Guid PlayerId,
    TeamRole Role
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business logic event for team member management - unique to Team entity
/// </summary>
public partial record TeamMemberRemovedEvent(
    Guid TeamId,
    Guid PlayerId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business logic event for team captain changes - unique to Team entity
/// </summary>
public partial record TeamCaptainChangedEvent(
    Guid TeamId,
    Guid PreviousCaptainId,
    Guid NewCaptainId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business logic event for team manager status changes - unique to Team entity
/// </summary>
public partial record TeamManagerStatusChangedEvent(
    Guid TeamId,
    Guid PlayerId,
    bool IsTeamManager
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
