using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Scrimmages;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    /// <summary>
    /// Service for handling scrimmage rating-related business logic.
    /// </summary>
    public class RatingCalculatorService
    {
        private readonly ICoreEventBus _eventBus;
        private readonly ICoreErrorHandler _errorHandler;
        private const double MAX_GAP_PERCENT = 0.4; // 40% of rating range
        private const double BASE_RATING_CHANGE = 32.0;
        private const double MAX_VARIETY_BONUS = 0.2;  // +20% max bonus for high variety
        private const double MIN_VARIETY_BONUS = -0.1; // -10% penalty for low variety
        private const int VARIETY_WINDOW_DAYS = 30;

        public RatingCalculatorService(
            ICoreEventBus eventBus,
            ICoreErrorHandler errorHandler)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <summary>
        /// Calculates opponent distributions for all teams.
        /// </summary>
        public async Task<AllTeamOpponentDistributionsResponse> CalculateAllTeamOpponentDistributions(
            AllTeamOpponentDistributionsRequest request)
        {
            try
            {
                // Get all teams' ratings
                var ratingsRequest = new AllTeamRatingsRequest();
                var ratingsResponse = await _eventBus.RequestAsync<AllTeamRatingsRequest, AllTeamRatingsResponse>(ratingsRequest);

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

                    var statsResponse = await _eventBus.RequestAsync<TeamOpponentStatsRequest, TeamOpponentStatsResponse>(statsRequest);
                    if (statsResponse?.OpponentMatches == null) continue;

                    // Calculate normalized weights for each opponent
                    var weighted = CalculateOpponentWeights(statsResponse.TeamRating, statsResponse.OpponentMatches, ratingRange);

                    distributions.Add((teamId, weighted));
                }

                return new AllTeamOpponentDistributionsResponse
                {
                    Distributions = distributions
                };
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                throw;
            }
        }

        /// <summary>
        /// Calculates normalized weights for a team's opponents based on rating gaps and match counts.
        /// </summary>
        private Dictionary<string, double> CalculateOpponentWeights(
            int teamRating,
            Dictionary<string, (int Count, int Rating)> opponentMatches,
            (int Max, int Min) ratingRange)
        {
            var weighted = new Dictionary<string, double>();
            double total = 0;

            foreach (var (opponentId, (count, rating)) in opponentMatches)
            {
                // Calculate gap as percentage of total rating range
                double gap = Math.Abs(rating - teamRating) / (double)(ratingRange.Max - ratingRange.Min);

                // Weight decreases linearly as gap approaches MAX_GAP_PERCENT
                double weight = Math.Max(1.0 - (gap / MAX_GAP_PERCENT), 0.0) * count;

                weighted[opponentId] = weight;
                total += weight;
            }

            // Normalize weights to sum to 1
            if (total > 0)
            {
                foreach (var key in weighted.Keys.ToList())
                {
                    weighted[key] /= total;
                }
            }

            return weighted;
        }

        /// <summary>
        /// Calculates the variety bonus for a team based on their opponent distribution.
        /// </summary>
        public async Task<double> CalculateVarietyBonusAsync(
            string teamId,
            int teamRating,
            GameSize gameSize)
        {
            try
            {
                // Get the current global rating range
                var ratingRange = await GetCurrentRatingRangeAsync();

                // Get team's opponent distribution
                var distribution = await GetTeamOpponentDistributionAsync(teamId, ratingRange, gameSize);

                // Calculate team's variety entropy
                double teamVarietyEntropy = CalculateWeightedEntropy(distribution);

                // Calculate global average variety entropy
                double averageVarietyEntropy = await GetAverageEntropyAsync(ratingRange);

                // Get average matches played in the team's rating range
                double averageMatchesPlayed = await GetAverageGamesPlayedAsync(teamRating, ratingRange);

                // Get team's matches played
                int teamMatchesPlayed = await GetTeamMatchesPlayedAsync(teamId, gameSize);

                // Calculate and return the variety bonus
                return CalculateVarietyBonus(teamVarietyEntropy, averageVarietyEntropy, teamMatchesPlayed, averageMatchesPlayed);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                throw;
            }
        }

        private static double CalculateWeightedEntropy(Dictionary<string, double> opponentWeights)
        {
            double entropy = 0.0;
            foreach (var weight in opponentWeights.Values)
            {
                if (weight > 0)
                    entropy -= weight * Math.Log(weight, 2); // Shannon entropy
            }
            return entropy;
        }

        private static double CalculateVarietyBonus(
            double teamVarietyEntropy,
            double averageVarietyEntropy,
            int teamMatchesPlayed,
            double averageMatchesPlayed)
        {
            // Calculate how far the team's variety entropy is from the average
            double entropyDifference = teamVarietyEntropy - averageVarietyEntropy;

            // Bonus scales proportionally with difference from average
            double relativeDiff = entropyDifference / (averageVarietyEntropy == 0 ? 1 : averageVarietyEntropy);

            // Scale the bonus based on games played relative to average
            double gamesPlayedRatio = Math.Min(teamMatchesPlayed / averageMatchesPlayed, 1.0);

            // Apply a minimum scaling factor to ensure some bonus even with few games
            double minScalingFactor = 0.5; // 50% of the max bonus
            double scalingFactor = minScalingFactor + (1.0 - minScalingFactor) * gamesPlayedRatio;

            // Apply the scaling factor to the relative difference
            double scaledDiff = relativeDiff * scalingFactor;

            return Math.Clamp(scaledDiff * MAX_VARIETY_BONUS, MIN_VARIETY_BONUS, MAX_VARIETY_BONUS);
        }

        private async Task<(int Max, int Min)> GetCurrentRatingRangeAsync()
        {
            var request = new AllTeamRatingsRequest();
            var response = await _eventBus.RequestAsync<AllTeamRatingsRequest, AllTeamRatingsResponse>(request);

            if (response?.Ratings == null || !response.Ratings.Any())
                return (2100, 1500); // Fallback

            return (response.Ratings.Max(), response.Ratings.Min());
        }

        private async Task<Dictionary<string, double>> GetTeamOpponentDistributionAsync(
            string teamId,
            (int Max, int Min) ratingRange,
            GameSize gameSize)
        {
            const int VARIETY_WINDOW_DAYS = 30;
            var startDate = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS);
            var request = new TeamOpponentStatsRequest
            {
                TeamId = teamId,
                Since = startDate,
                GameSize = gameSize
            };

            var response = await _eventBus.RequestAsync<TeamOpponentStatsRequest, TeamOpponentStatsResponse>(request);
            if (response == null)
                throw new InvalidOperationException("Failed to retrieve team opponent statistics");

            return CalculateOpponentWeights(response.TeamRating, response.OpponentMatches, ratingRange);
        }

        private async Task<double> GetAverageEntropyAsync((int Max, int Min) ratingRange)
        {
            const int VARIETY_WINDOW_DAYS = 30;
            var request = new AllTeamOpponentDistributionsRequest
            {
                Since = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS)
            };

            var response = await _eventBus.RequestAsync<AllTeamOpponentDistributionsRequest, AllTeamOpponentDistributionsResponse>(request);
            if (response?.Distributions == null || response.Distributions.Count == 0)
                return 0;

            // Variety entropy mean across all teams
            double totalVarietyEntropy = 0;
            int count = 0;

            foreach (var (_, weights) in response.Distributions)
            {
                double varietyEntropy = CalculateWeightedEntropy(weights);
                totalVarietyEntropy += varietyEntropy;
                count++;
            }

            return count > 0 ? totalVarietyEntropy / count : 0;
        }

        private async Task<double> GetAverageGamesPlayedAsync(int teamRating, (int Max, int Min) ratingRange)
        {
            const int VARIETY_WINDOW_DAYS = 30;
            var request = new TeamGamesPlayedRequest
            {
                Since = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS)
            };

            var response = await _eventBus.RequestAsync<TeamGamesPlayedRequest, TeamGamesPlayedResponse>(request);
            if (response?.SeasonTeamStats == null || response.SeasonTeamStats.Count == 0)
                return 0;

            // Calculate dynamic range based on MAX_GAP_PERCENT
            double range = ratingRange.Max - ratingRange.Min;
            double maxGap = range * MAX_GAP_PERCENT;

            // Calculate average games played for teams within the dynamic range
            var relevantTeams = response.SeasonTeamStats
                .Where(t => Math.Abs(t.Value.CurrentRating - teamRating) <= maxGap)
                .ToList();

            if (relevantTeams.Count == 0)
                return 0;

            return relevantTeams.Average(t => t.Value.RecentMatchesCount);
        }

        private async Task<int> GetTeamMatchesPlayedAsync(string teamId, GameSize gameSize)
        {
            const int VARIETY_WINDOW_DAYS = 30;
            var startDate = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS);
            var request = new ScrimmageHistoryRequest
            {
                TeamId = teamId,
                Since = startDate
            };

            var response = await _eventBus.RequestAsync<ScrimmageHistoryRequest, ScrimmageHistoryResponse>(request);
            return response?.Matches?.Count() ?? 0;
        }

        /// <summary>
        /// Calculates the rating multiplier for a match based on team confidence and opponent variety.
        /// </summary>
        public async Task<double> CalculateRatingMultiplierAsync(
            string team1Id,
            string team2Id,
            int team1Rating,
            int team2Rating,
            GameSize gameSize)
        {
            try
            {
                // Get team confidence levels
                var team1Confidence = await CalculateConfidenceAsync(team1Id, gameSize);
                var team2Confidence = await CalculateConfidenceAsync(team2Id, gameSize);

                // Get variety bonuses
                var team1VarietyBonus = await CalculateVarietyBonusAsync(team1Id, team1Rating, gameSize);
                var team2VarietyBonus = await CalculateVarietyBonusAsync(team2Id, team2Rating, gameSize);

                // Calculate combined multiplier
                double multiplier = 1.0;
                multiplier *= 1.0 + team1VarietyBonus;
                multiplier *= 1.0 + team2VarietyBonus;
                multiplier *= 1.0 - team1Confidence;
                multiplier *= 1.0 - team2Confidence;

                return multiplier;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                throw;
            }
        }

        /// <summary>
        /// Calculates the rating change for a match using the ELO formula.
        /// </summary>
        public double CalculateRatingChange(
            string team1Id,
            string team2Id,
            int team1Rating,
            int team2Rating,
            double multiplier)
        {
            // Calculate expected outcome using ELO formula
            double expectedOutcome = 1.0 / (1.0 + Math.Pow(10.0, (team2Rating - team1Rating) / 400.0));

            // Calculate actual outcome (1 for win, 0 for loss)
            double actualOutcome = team1Rating > team2Rating ? 1.0 : 0.0;

            // Calculate rating change
            double ratingChange = BASE_RATING_CHANGE * (actualOutcome - expectedOutcome) * multiplier;

            return ratingChange;
        }

        /// <summary>
        /// Calculates the confidence level for a team based on games played.
        /// </summary>
        private async Task<double> CalculateConfidenceAsync(string teamId, GameSize gameSize)
        {
            try
            {
                // Get team's match history
                var request = new ScrimmageHistoryRequest
                {
                    TeamId = teamId,
                    Since = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS)
                };

                var response = await _eventBus.RequestAsync<ScrimmageHistoryRequest, ScrimmageHistoryResponse>(request);
                if (response?.Matches == null)
                    return 0.0;

                // Calculate confidence based on number of games played
                int totalGames = response.Matches.Count();
                int recentGames = response.Matches.Count(m => m.CreatedAt >= DateTime.UtcNow.AddDays(-7));

                // Confidence increases with more games played
                double confidence = Math.Min(totalGames / 20.0, 1.0); // Max confidence at 20 games
                confidence *= 0.7; // Base confidence cap at 70%

                // Recent games boost confidence
                double recentBoost = Math.Min(recentGames / 5.0, 1.0) * 0.3; // Up to 30% boost from recent games
                confidence += recentBoost;

                return Math.Min(confidence, 1.0);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                throw;
            }
        }
    }
}
