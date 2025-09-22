using System;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Tournaments
{
    public record TournamentCreatedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string TournamentId { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;

        // TournamentId is inherited - no full object payload
    }

    public record TournamentUpdatedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string TournamentId { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;

        public string[] ChangedProperties { get; init; } = Array.Empty<string>();
    }

    public record TournamentStatusChangedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string TournamentId { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;

        public string OldStatus { get; init; } = string.Empty;
        public string NewStatus { get; init; } = string.Empty;
        public string? Reason { get; init; }
    }

    public record TournamentDeletedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string TournamentId { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;

        public string? Reason { get; init; }
    }
}
