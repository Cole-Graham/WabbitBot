using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.Events
{
    /// <summary>
    /// Event published when a player is created
    /// </summary>
    public partial record PlayerCreatedEvent(
        string PlayerId
    ) : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event published when a player is updated
    /// </summary>
    public partial record PlayerUpdatedEvent(
        string PlayerId
    ) : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event published when a player is deleted
    /// </summary>
    public partial record PlayerDeletedEvent(
        string PlayerId
    ) : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Business logic event for player archive checking - unique to Player entity
    /// This event is used as a mutable data container where handlers set properties
    /// </summary>
    public partial class PlayerArchiveCheckEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid PlayerId { get; }
        public bool HasActiveUsers { get; set; }
        public bool HasActiveMatches { get; set; }

        public PlayerArchiveCheckEvent(Guid playerId)
        {
            PlayerId = playerId;
            HasActiveUsers = false;
            HasActiveMatches = false;
        }
    }

    /// <summary>
    /// Event published when a player is archived
    /// </summary>
    public partial record PlayerArchivedEvent(
        string PlayerId
    ) : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event published when a player is unarchived
    /// </summary>
    public partial record PlayerUnarchivedEvent(
        string PlayerId,
        DateTime UnarchivedAt = default
    ) : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public DateTime UnarchivedAt { get; init; } = UnarchivedAt == default ? DateTime.UtcNow : UnarchivedAt;
    }
}