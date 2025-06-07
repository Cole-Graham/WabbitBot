using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Matches
{
    public abstract record MatchEvent : ICoreEvent
    {
        public string MatchId { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record MatchCreatedEvent : MatchEvent
    {
        public string Team1Id { get; init; } = string.Empty;
        public string Team2Id { get; init; } = string.Empty;
        public List<string> Team1PlayerIds { get; init; } = new();
        public List<string> Team2PlayerIds { get; init; } = new();
        public GameSize GameSize { get; init; }
        public int BestOf { get; init; }
    }

    public record MatchStartedEvent : MatchEvent
    {
        public DateTime StartedAt { get; init; }
        public string GameId { get; init; } = string.Empty;
    }

    public record MatchCompletedEvent : MatchEvent
    {
        public string WinnerId { get; init; } = string.Empty;
        public DateTime CompletedAt { get; init; }
        public int Team1Rating { get; init; }
        public int Team2Rating { get; init; }
        public int RatingChange { get; init; }
        public string GameId { get; init; } = string.Empty;
    }

    public record MatchCancelledEvent : MatchEvent
    {
        public string Reason { get; init; } = string.Empty;
        public string CancelledBy { get; init; } = string.Empty;
        public string? GameId { get; init; }
    }

    public record MatchForfeitedEvent : MatchEvent
    {
        public string ForfeitedTeamId { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
        public string GameId { get; init; } = string.Empty;
    }

    public record MatchPlayerJoinedEvent : MatchEvent
    {
        public string PlayerId { get; init; } = string.Empty;
        public int TeamNumber { get; init; }
    }

    public record MatchPlayerLeftEvent : MatchEvent
    {
        public string PlayerId { get; init; } = string.Empty;
        public int TeamNumber { get; init; }
    }

    public record GameStartedEvent : MatchEvent
    {
        public string GameId { get; init; } = string.Empty;
        public int GameNumber { get; init; }
        public string MapId { get; init; } = string.Empty;
    }

    public record GameCompletedEvent : MatchEvent
    {
        public string GameId { get; init; } = string.Empty;
        public string WinnerId { get; init; } = string.Empty;
        public DateTime CompletedAt { get; init; }
    }

    public record GameCancelledEvent : MatchEvent
    {
        public string GameId { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
    }

    public record GameForfeitedEvent : MatchEvent
    {
        public string GameId { get; init; } = string.Empty;
        public string ForfeitedTeamId { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;
    }
}
