using System;
using System.Collections.Generic;
using WabbitBot.Core.Scrimmages.Data;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Scrimmages
{
    public abstract record ScrimmageEvent : ICoreEvent
    {
        public string ScrimmageId { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

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
        public Dictionary<string, SeasonTeamStats> SeasonTeamStats { get; set; } = new();
    }

    public class GetTeamRatingRequest
    {
        public string TeamId { get; set; } = string.Empty;
    }

    public class GetTeamRatingResponse
    {
        public int Rating { get; set; }
    }

    public class CheckProvenPotentialRequest
    {
        public string TeamId { get; set; } = string.Empty;
        public int CurrentRating { get; set; }
    }

    public class CheckProvenPotentialResponse
    {
        public bool HasAdjustments { get; set; }
        public List<RatingAdjustment> Adjustments { get; set; } = new();
    }

    public class RatingAdjustment
    {
        public string Team1Id { get; set; } = string.Empty;
        public string Team2Id { get; set; } = string.Empty;
        public int Adjustment { get; set; }
    }

    public class CreateProvenPotentialRecordRequest
    {
        public Guid MatchId { get; set; }
        public string Team1Id { get; set; } = string.Empty;
        public string Team2Id { get; set; } = string.Empty;
        public int Team1Rating { get; set; }
        public int Team2Rating { get; set; }
        public double Team1Confidence { get; set; }
        public double Team2Confidence { get; set; }
        public int RatingChange { get; set; }
    }

    public class CreateProvenPotentialRecordResponse
    {
        public bool Created { get; set; }
        public string? Reason { get; set; }
    }

    public record ScrimmageCompletedEvent : ScrimmageEvent
    {
        public Guid MatchId { get; init; }
        public string Team1Id { get; init; } = string.Empty;
        public string Team2Id { get; init; } = string.Empty;
        public int Team1Score { get; init; }
        public int Team2Score { get; init; }
        public GameSize GameSize { get; init; }
        public double Team1Confidence { get; init; } = 0.0;
        public double Team2Confidence { get; init; } = 0.0;
    }
}
