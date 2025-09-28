using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.Events
{
    public record MatchCreatedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid MatchId { get; init; } = Guid.Empty;

        public Guid Team1Id { get; init; } = Guid.Empty;
        public Guid Team2Id { get; init; } = Guid.Empty;
        public List<Guid> Team1PlayerIds { get; init; } = new();
        public List<Guid> Team2PlayerIds { get; init; } = new();
        public TeamSize TeamSize { get; init; }
        public int BestOf { get; init; }
    }

    public record MatchStartedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid MatchId { get; init; } = Guid.Empty;

        public DateTime StartedAt { get; init; }
        public Guid GameId { get; init; } = Guid.Empty;
    }

    public record MatchCompletedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid MatchId { get; init; } = Guid.Empty;

        public Guid WinnerId { get; init; } = Guid.Empty;
        public DateTime CompletedAt { get; init; }
        public Guid GameId { get; init; } = Guid.Empty;
    }

    public record MatchCancelledEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid MatchId { get; init; } = Guid.Empty;

        public string Reason { get; init; } = string.Empty;
        public Guid CancelledBy { get; init; } = Guid.Empty;
        public Guid? GameId { get; init; }
    }

    public record MatchForfeitedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid MatchId { get; init; } = Guid.Empty;

        public Guid ForfeitedTeamId { get; init; } = Guid.Empty;
        public string Reason { get; init; } = string.Empty;
        public Guid GameId { get; init; } = Guid.Empty;
    }

    public record MatchPlayerJoinedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid MatchId { get; init; } = Guid.Empty;

        public Guid PlayerId { get; init; } = Guid.Empty;
        public int TeamNumber { get; init; }
    }

    public record MatchPlayerLeftEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid MatchId { get; init; } = Guid.Empty;

        public Guid PlayerId { get; init; } = Guid.Empty;
        public int TeamNumber { get; init; }
    }

}
