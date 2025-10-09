using System;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Core.Tournaments
{
    /// <summary>
    /// Event published when a tournament's status changes (e.g., from RegistrationOpen to InProgress).
    /// This is a business logic event, not a simple CRUD operation.
    /// </summary>
    public record TournamentStatusChangedEvent(Guid TournamentId, string NewStatus) : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
