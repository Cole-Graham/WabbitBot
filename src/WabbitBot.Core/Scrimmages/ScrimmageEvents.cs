using System;
using System.Collections.Generic;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Scrimmages
{

    public record ScrimmageHistoryRequest(
        Guid TeamId,
        DateTime Since,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record ScrimmageHistoryResponse(
        Guid TeamId,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        // Matches should be fetched from database by handlers using TeamId
    }

    public record GetActiveSeasonRequest(
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record GetActiveSeasonResponse(
        Guid? SeasonId,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        // Season data should be fetched from database by handlers using SeasonId
    }

    public record TeamGamesPlayedRequest(
        DateTime Since,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record TeamGamesPlayedResponse(
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        // Team stats should be fetched from database by handlers
    }

    public record GetTeamRatingRequest(
        Guid TeamId,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record GetTeamRatingResponse(
        double Rating,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record CheckProvenPotentialRequest(
        Guid TeamId,
        double CurrentRating,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record CheckProvenPotentialResponse(
        Guid TeamId,
        bool HasAdjustments,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        // Adjustments should be fetched from database by handlers using TeamId
    }

    public class RatingAdjustment
    {
        public Guid ChallengerId { get; set; }
        public Guid OpponentId { get; set; }
        public double Adjustment { get; set; }
    }

    public record CreateProvenPotentialRecordRequest(
        Guid MatchId,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        // All match and rating data should be fetched from database by handlers using MatchId
    }

    public record CreateProvenPotentialRecordResponse(
        Guid MatchId,
        bool Created,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record ScrimmageAcceptedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid ScrimmageId { get; init; }
        public DateTime AcceptedAt { get; init; }

        public ScrimmageAcceptedEvent() => EventBusType = EventBusType.Global;
    }

    public record ScrimmageDeclinedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid ScrimmageId { get; init; }

        // DeclinedBy is included for Discord UX but handlers should be prepared to fetch from database if needed
        public string? DeclinedBy { get; init; }
        public ScrimmageDeclinedEvent() => EventBusType = EventBusType.Global;
    }

    public record ScrimmageCompletedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid ScrimmageId { get; init; }

        public Guid MatchId { get; init; }
        // MatchId is kept as essential identifier for linking to completed match
        // All other data should be fetched from repositories by handlers
        public ScrimmageCompletedEvent() => EventBusType = EventBusType.Global;
    }

    // Rating system events - internal processing only, simple ID pattern
    public record ScrimmageRatingUpdateEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Guid ScrimmageId { get; init; }

        // All rating data (teams, scores, game size, confidence) should be fetched from database by handlers
        // This follows the simple ID pattern to avoid heavy data payloads in events
        public ScrimmageRatingUpdateEvent() => EventBusType = EventBusType.Core;
    }
}
