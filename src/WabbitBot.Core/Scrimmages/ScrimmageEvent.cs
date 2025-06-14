using System;
using System.Collections.Generic;
using WabbitBot.Core.Scrimmages.Data;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Common.Models;

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
}
