using System;
using System.Collections.Generic;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    public class RatingAdjustmentEvent : ICoreEvent
    {
        public string Team1Id { get; init; } = string.Empty;
        public string Team2Id { get; init; } = string.Empty;
        public int Adjustment { get; init; }
    }

    public class ApplyProvenPotentialAdjustmentEvent : ICoreEvent
    {
        public string Team1Id { get; init; } = string.Empty;
        public string Team2Id { get; init; } = string.Empty;
        public int Adjustment { get; init; }
        public GameSize GameSize { get; init; }
        public string Reason { get; init; } = string.Empty;
    }

    public class UpdateTeamRatingEvent : ICoreEvent
    {
        public string TeamId { get; init; } = string.Empty;
        public int NewRating { get; init; }
        public GameSize GameSize { get; init; }
        public string Reason { get; init; } = string.Empty;
    }

    public class AllTeamRatingsRequest
    {
        // No additional properties needed
    }

    public class AllTeamRatingsResponse
    {
        public IEnumerable<int> Ratings { get; set; } = Array.Empty<int>();
        public IEnumerable<string> TeamIds { get; set; } = Array.Empty<string>();
    }

    public class TeamOpponentStatsRequest
    {
        public string TeamId { get; set; } = string.Empty;
        public DateTime Since { get; set; }
        public GameSize GameSize { get; set; }
    }

    public class TeamOpponentStatsResponse
    {
        public int TeamRating { get; set; }
        public GameSize? GameSize { get; set; }
        public Dictionary<string, (int Count, int Rating)> OpponentMatches { get; set; } = new();
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
        public int Rating { get; set; }
    }
}