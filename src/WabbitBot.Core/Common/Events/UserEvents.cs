using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Event published when a user is created
/// </summary>
public partial record UserCreatedEvent(
    string UserId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a user is updated
/// </summary>
public partial record UserUpdatedEvent(
    string UserId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a user is archived
/// </summary>
public partial record UserArchivedEvent(
    string UserId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a user is deleted
/// </summary>
public partial record UserDeletedEvent(
    string UserId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
