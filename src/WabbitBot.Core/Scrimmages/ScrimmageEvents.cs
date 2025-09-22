using System;
using System.Collections.Generic;
using WabbitBot.Core.Scrimmages.Data;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Scrimmages
{

    public class ScrimmageHistoryRequest
    {
        public string TeamId { get; set; } = string.Empty;
        public DateTime Since { get; set; }
    }

    public class ScrimmageHistoryResponse
    {
        public IEnumerable<Scrimmage> Matches { get; set; } = Array.Empty<Scrimmage>();
    }

    public class AllTeamRatingsRequest
    {
        // No additional properties needed
    }

    public class AllTeamRatingsResponse
    {
        public IEnumerable<double> Ratings { get; set; } = Array.Empty<double>();
        public IEnumerable<string> TeamIds { get; set; } = Array.Empty<string>();
    }

    public class TeamOpponentStatsRequest
    {
        public string TeamId { get; set; } = string.Empty;
        public DateTime Since { get; set; }
        public EvenTeamFormat EvenTeamFormat { get; set; }
    }

    public class TeamOpponentStatsResponse
    {
        public double TeamRating { get; set; }
        public EvenTeamFormat? EvenTeamFormat { get; set; }
        public Dictionary<string, (int Count, double Rating)> OpponentMatches { get; set; } = new();
    }

    public class GetActiveSeasonRequest
    {
        // No additional properties needed
    }

    public class GetActiveSeasonResponse
    {
        public Season? Season { get; set; }
    }

    public class TeamGamesPlayedRequest
    {
        public DateTime Since { get; set; }
    }

    public class TeamGamesPlayedResponse
    {
        public Dictionary<string, Stats> TeamStats { get; set; } = new();
    }

    public class GetTeamRatingRequest
    {
        public string TeamId { get; set; } = string.Empty;
    }

    public class GetTeamRatingResponse
    {
        public double Rating { get; set; }
    }

    public class CheckProvenPotentialRequest
    {
        public string TeamId { get; set; } = string.Empty;
        public double CurrentRating { get; set; }
    }

    public class CheckProvenPotentialResponse
    {
        public bool HasAdjustments { get; set; }
        public List<RatingAdjustment> Adjustments { get; set; } = new();
    }

    public class RatingAdjustment
    {
        public string ChallengerId { get; set; } = string.Empty;
        public string OpponentId { get; set; } = string.Empty;
        public double Adjustment { get; set; }
    }

    public class CreateProvenPotentialRecordRequest
    {
        public Guid MatchId { get; set; }
        public string ChallengerId { get; set; } = string.Empty;
        public string OpponentId { get; set; } = string.Empty;
        public double ChallengerRating { get; set; }
        public double OpponentRating { get; set; }
        public double ChallengerConfidence { get; set; }
        public double OpponentConfidence { get; set; }
        public double ChallengerOriginalRatingChange { get; set; }
        public double OpponentOriginalRatingChange { get; set; }
        public EvenTeamFormat EvenTeamFormat { get; set; }
    }

    public class CreateProvenPotentialRecordResponse
    {
        public bool Created { get; set; }
        public string? Reason { get; set; }
    }

    [GenerateCoreToDiscBot]
    public record ScrimmageAcceptedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string ScrimmageId { get; init; } = string.Empty;

        public ScrimmageAcceptedEvent() => EventBusType = EventBusType.Global;
    }

    [GenerateCoreToDiscBot]
    public record ScrimmageDeclinedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string ScrimmageId { get; init; } = string.Empty;

        // DeclinedBy is included for Discord UX but handlers should be prepared to fetch from database if needed
        public string? DeclinedBy { get; init; }
        public ScrimmageDeclinedEvent() => EventBusType = EventBusType.Global;
    }

    [GenerateCoreToDiscBot]
    public record ScrimmageCompletedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string ScrimmageId { get; init; } = string.Empty;

        public string MatchId { get; init; } = string.Empty;
        // MatchId is kept as essential identifier for linking to completed match
        // All other data should be fetched from repositories by handlers
        public ScrimmageCompletedEvent() => EventBusType = EventBusType.Global;
    }

    // Rating system events - internal processing only, simple ID pattern
    public record ScrimmageRatingUpdateEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string ScrimmageId { get; init; } = string.Empty;

        // All rating data (teams, scores, game size, confidence) should be fetched from database by handlers
        // This follows the simple ID pattern to avoid heavy data payloads in events
        public ScrimmageRatingUpdateEvent() => EventBusType = EventBusType.Core;
    }
}
