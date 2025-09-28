using System;
using System.Collections.Generic;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Scrimmages
{
    public record RatingAdjustmentEvent(
        Guid Team1Id,
        Guid Team2Id,
        double Adjustment,
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default) : IEvent;

    public record ApplyProvenPotentialAdjustmentEvent(
        Guid ChallengerId,
        Guid OpponentId,
        double Adjustment,
        TeamSize TeamSize,
        string Reason,
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default) : IEvent;

    public record UpdateTeamRatingEvent(
        Guid TeamId,
        double NewRating,
        TeamSize TeamSize,
        string Reason,
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default) : IEvent;

    public record ApplyTeamRatingChangeEvent(
        Guid TeamId,
        double RatingChange,
        TeamSize TeamSize,
        string Reason,
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default) : IEvent;

    public record AllTeamRatingsRequest(
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record AllTeamRatingsResponse(
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        // Ratings and TeamIds should be fetched from database by handlers
    }

    public record TeamOpponentStatsRequest(
        Guid TeamId,
        DateTime Since,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record TeamOpponentStatsResponse(
        Guid TeamId,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        // Team rating, size, and opponent stats should be fetched from database by handlers using TeamId
    }

    public record AllTeamOpponentDistributionsRequest(
        DateTime Since,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record AllTeamOpponentDistributionsResponse(
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        // Distributions should be fetched from database by handlers
    }

    public record CalculateConfidenceRequest(
        Guid TeamId,
        TeamSize TeamSize,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record CalculateConfidenceResponse(
        Guid TeamId,
        double Confidence,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record CalculateRatingChangeRequest(
        Guid Team1Id,
        Guid Team2Id,
        double Team1Rating,
        double Team2Rating,
        int Team1Score,
        int Team2Score,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    public record CalculateRatingChangeResponse(
        Guid MatchId,
        double Team1Change,
        double Team2Change,
        EventBusType EventBusType = EventBusType.Core) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        // MatchId is used to correlate with the original request
    }
}