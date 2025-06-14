using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Core.Scrimmages.Data;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Scrimmages
{
    /// <summary>
    /// Handler for scrimmage-related events and requests.
    /// </summary>
    public class ScrimmageHandler : CoreBaseHandler
    {
        public ScrimmageHandler() : base(CoreEventBus.Instance)
        {
        }

        public override Task InitializeAsync()
        {
            // Subscribe to scrimmage events
            EventBus.Subscribe<AllTeamOpponentDistributionsRequest>(async request =>
                await HandleAllTeamOpponentDistributionsRequest(request));

            return Task.CompletedTask;
        }

        public async Task<AllTeamOpponentDistributionsResponse> HandleAllTeamOpponentDistributionsRequest(
            AllTeamOpponentDistributionsRequest request)
        {
            // Get all teams' ratings
            var ratingsRequest = new AllTeamRatingsRequest();
            var ratingsResponse = await EventBus.RequestAsync<AllTeamRatingsRequest, AllTeamRatingsResponse>(ratingsRequest);

            if (ratingsResponse?.Ratings == null || !ratingsResponse.Ratings.Any())
            {
                return new AllTeamOpponentDistributionsResponse
                {
                    Distributions = new List<(string, Dictionary<string, double>)>()
                };
            }

            var distributions = new List<(string, Dictionary<string, double>)>();
            var ratingRange = (ratingsResponse.Ratings.Max(), ratingsResponse.Ratings.Min());

            // For each team, get their opponent distribution
            foreach (var teamId in ratingsResponse.TeamIds)
            {
                var statsRequest = new TeamOpponentStatsRequest
                {
                    TeamId = teamId,
                    Since = request.Since
                };

                var statsResponse = await EventBus.RequestAsync<TeamOpponentStatsRequest, TeamOpponentStatsResponse>(statsRequest);
                if (statsResponse?.OpponentMatches == null) continue;

                // Calculate normalized weights for each opponent
                var weighted = new Dictionary<string, double>();
                double total = 0;

                foreach (var (opponentId, (count, rating)) in statsResponse.OpponentMatches)
                {
                    double gap = Math.Abs(rating - statsResponse.TeamRating) / (double)(ratingRange.Item1 - ratingRange.Item2);
                    double weight = Math.Max(1.0 - (gap / 0.4), 0.0) * count; // 0.4 is MAX_GAP_PERCENT
                    weighted[opponentId] = weight;
                    total += weight;
                }

                // Normalize the weights
                if (total > 0)
                {
                    foreach (var key in weighted.Keys.ToList())
                    {
                        weighted[key] /= total;
                    }
                }

                distributions.Add((teamId, weighted));
            }

            return new AllTeamOpponentDistributionsResponse
            {
                Distributions = distributions
            };
        }
    }
}
