using System;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Events
{
    /// <summary>
    /// Event published when the database has been successfully initialized and migrations have been applied.
    /// </summary>
    public record DatabaseInitializedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
