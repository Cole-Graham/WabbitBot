using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Matches
{
    public record MatchCreatedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string MatchId { get; init; } = string.Empty;

        public string Team1Id { get; init; } = string.Empty;
        public string Team2Id { get; init; } = string.Empty;
        public List<string> Team1PlayerIds { get; init; } = new();
        public List<string> Team2PlayerIds { get; init; } = new();
        public EvenTeamFormat EvenTeamFormat { get; init; }
        public int BestOf { get; init; }
    }

    public record MatchStartedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string MatchId { get; init; } = string.Empty;

        public DateTime StartedAt { get; init; }
        public string GameId { get; init; } = string.Empty;
    }

    public record MatchCompletedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string MatchId { get; init; } = string.Empty;

        public string WinnerId { get; init; } = string.Empty;
        public DateTime CompletedAt { get; init; }
        public string GameId { get; init; } = string.Empty;
    }

    public record MatchCancelledEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string MatchId { get; init; } = string.Empty;

        public string Reason { get; init; } = string.Empty;
        public string CancelledBy { get; init; } = string.Empty;
        public string? GameId { get; init; }
    }

    public record MatchForfeitedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string MatchId { get; init; } = string.Empty;

        public string ForfeitedTeamId { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public string GameId { get; init; } = string.Empty;
    }

    public record MatchPlayerJoinedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string MatchId { get; init; } = string.Empty;

        public string PlayerId { get; init; } = string.Empty;
        public int TeamNumber { get; init; }
    }

    public record MatchPlayerLeftEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string MatchId { get; init; } = string.Empty;

        public string PlayerId { get; init; } = string.Empty;
        public int TeamNumber { get; init; }
    }

}
