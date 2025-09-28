using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.Events
{
    // CRUD events removed: PlayerCreatedEvent, PlayerUpdatedEvent, PlayerDeletedEvent
    // These were database operations and violate the critical principle that events are not for CRUD.

    /// <summary>
    /// Business logic event for player archive checking - unique to Player entity
    /// This event is used as a mutable data container where handlers set properties
    /// </summary>
    public partial class PlayerArchiveCheckEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
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

    // Additional CRUD events removed: PlayerArchivedEvent, PlayerUnarchivedEvent
    // These were database operations and violate the critical principle that events are not for CRUD.
}