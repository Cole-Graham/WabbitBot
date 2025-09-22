using System;
using System.Collections.Generic;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    public class RatingAdjustmentEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string Team1Id { get; init; } = string.Empty;
        public string Team2Id { get; init; } = string.Empty;
        public double Adjustment { get; init; }
    }

    public class ApplyProvenPotentialAdjustmentEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string ChallengerId { get; init; } = string.Empty;
        public string OpponentId { get; init; } = string.Empty;
        public double Adjustment { get; init; }
        public EvenTeamFormat EvenTeamFormat { get; init; }
        public string Reason { get; init; } = string.Empty;
    }

    public class UpdateTeamRatingEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string TeamId { get; init; } = string.Empty;
        public double NewRating { get; init; }
        public EvenTeamFormat EvenTeamFormat { get; init; }
        public string Reason { get; init; } = string.Empty;
    }

    public class ApplyTeamRatingChangeEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string TeamId { get; init; } = string.Empty;
        public double RatingChange { get; init; }
        public EvenTeamFormat EvenTeamFormat { get; init; }
        public string Reason { get; init; } = string.Empty;
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

    public class AllTeamOpponentDistributionsRequest
    {
        public DateTime Since { get; set; }
    }

    public class AllTeamOpponentDistributionsResponse
    {
        public List<(string TeamId, Dictionary<string, double> NormalizedWeights)> Distributions { get; set; } = new();
    }

    public class GetTeamRatingRequest
    {
        public string TeamId { get; set; } = string.Empty;
    }

    public class GetTeamRatingResponse
    {
        public double Rating { get; set; }
    }

    public class CalculateConfidenceRequest
    {
        public string TeamId { get; set; } = string.Empty;
        public EvenTeamFormat EvenTeamFormat { get; set; }
    }

    public class CalculateConfidenceResponse
    {
        public double Confidence { get; set; }
    }

    public class CalculateRatingChangeRequest
    {
        public string Team1Id { get; set; } = string.Empty;
        public string Team2Id { get; set; } = string.Empty;
        public double Team1Rating { get; set; }
        public double Team2Rating { get; set; }
        public EvenTeamFormat EvenTeamFormat { get; set; }
        public int Team1Score { get; set; }
        public int Team2Score { get; set; }
    }

    public class CalculateRatingChangeResponse
    {
        public double Team1Change { get; set; }
        public double Team2Change { get; set; }
    }
}