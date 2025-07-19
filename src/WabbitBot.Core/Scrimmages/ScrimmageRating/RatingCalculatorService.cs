using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    /// <summary>
    /// Service for handling scrimmage rating-related business logic.
    /// </summary>
    public class RatingCalculatorService
    {
        private readonly ICoreEventBus _eventBus;
        private readonly ICoreErrorHandler _errorHandler;
        private readonly SeasonRatingService _seasonRatingService;
        private const double MAX_GAP_PERCENT = 0.2; // 20% of rating range
        private const double BASE_RATING_CHANGE = 16.0; // K-factor changed from 32 to 16
        private const double MAX_VARIETY_BONUS = 0.2;  // +20% max bonus for high variety
        private const double MIN_VARIETY_BONUS = -0.1; // -10% penalty for low variety
        private const int VARIETY_WINDOW_DAYS = 30;

        // Multiplier configuration
        private const double MAX_MULTIPLIER = 2.0;
        private const int VARIETY_BONUS_GAMES_THRESHOLD = 20; // Only apply variety bonuses after 20 games

        // Configuration option to enable/disable time-based filtering for variety calculations
        // Can be changed at runtime through configuration
        private bool UseTimeBasedVarietyFiltering { get; set; } = false;

        // Tail scaling constants for variety bonus
        private const double TAIL_SCALING_THRESHOLD = 0.8; // 80% distance from middle
        private const double SIGMOID_GROWTH_RATE = 1.1;
        private const double SIGMOID_MIDPOINT = 8.0;
        private const double MIN_SCALING_FACTOR = 0.1;
        private const double MAX_SCALING_FACTOR = 1.0;

        public RatingCalculatorService(
            ICoreEventBus eventBus,
            ICoreErrorHandler errorHandler,
            SeasonRatingService seasonRatingService)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _seasonRatingService = seasonRatingService ?? throw new ArgumentNullException(nameof(seasonRatingService));
        }

        /// <summary>
        /// Calculates the percentile of a player's rating in the current rating distribution.
        /// </summary>
        /// <param name="playerRating">The player's current rating.</param>
        /// <param name="allRatings">A sorted list of all player ratings.</param>
        /// <returns>The percentile (0.0 to 100.0) of the player's rating.</returns>
        private static double CalculatePlayerPercentile(int playerRating, List<int> allRatings)
        {
            if (allRatings == null || allRatings.Count == 0)
                return 50.0; // Default to middle if no ratings

            // Find the position of this rating in the sorted list
            // Count how many ratings are less than this rating
            int countBelow = 0;
            int countEqual = 0;

            foreach (int rating in allRatings)
            {
                if (rating < playerRating)
                    countBelow++;
                else if (rating == playerRating)
                    countEqual++;
            }

            // Calculate percentile using the formula: (count_below + 0.5 * count_equal) / total * 100
            // This handles ties by giving them the average position
            double percentile = (countBelow + 0.5 * countEqual) / (double)allRatings.Count * 100.0;

            return percentile;
        }

        /// <summary>
        /// Calculates the scaled bonus curve using sigmoid function for tail scaling.
        /// </summary>
        /// <param name="percentile">Percentile from 90 to 100 (for top 10%) or 1 to 10 (for bottom 10%).</param>
        /// <returns>Bonus multiplier from 0.1 to 1.0 (smooth sigmoid).</returns>
        private static double CalculateScaledBonusCurve(int percentile)
        {
            // For bottom 10%, mirror the calculation by mapping to top 10% range
            if (percentile <= 10)
            {
                // Map 10th percentile to 90th, 1st to 99th, etc.
                percentile = 100 - percentile;
            }

            // Validate input is in the correct range for top 10%
            if (percentile < 90 || percentile > 100)
                throw new ArgumentException("Percentile must be between 1-10 or 90-100 for tail scaling");

            // Map percentile to x in range [1, 10]
            double x = percentile - 89;

            double k = SIGMOID_GROWTH_RATE;  // Growth rate
            double x0 = SIGMOID_MIDPOINT;    // Midpoint

            double raw = 1.0 / (1.0 + Math.Exp(-k * (x - x0)));
            double minRaw = 1.0 / (1.0 + Math.Exp(-k * (1.0 - x0)));
            double maxRaw = 1.0 / (1.0 + Math.Exp(-k * (10.0 - x0)));

            double scaled = (raw - minRaw) / (maxRaw - minRaw);  // Normalize to [0, 1]
            return MIN_SCALING_FACTOR + scaled * (MAX_SCALING_FACTOR - MIN_SCALING_FACTOR);  // Scale to [0.1, 1.0]
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

            // Calculate variety gap (40% of total rating range, split in half for each side)
            double varietyGap = (ratingRange.Max - ratingRange.Min) * 0.4 / 2.0;

            foreach (var (opponentId, (count, rating)) in opponentMatches)
            {
                // Calculate gap between team and opponent
                double gap = Math.Abs(rating - teamRating);

                // Only count opponents within variety gap
                if (gap <= varietyGap)
                {
                    double weight;

                    // For higher-rated opponents, use full weight
                    if (rating >= teamRating)
                    {
                        weight = 1.0;
                    }
                    else
                    {
                        // For lower-rated opponents, use cosine scaling
                        // normalized_gap is gap normalized to 1.0 (0.0 to 1.0)
                        double normalizedGap = gap / varietyGap;
                        weight = (1.0 + Math.Cos(Math.PI * normalizedGap * 0.7)) / 2.0;
                    }

                    // Apply the weight to the match count
                    weighted[opponentId] = count * weight;
                    total += count * weight;
                }
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

                // Get all team ratings for percentile calculation
                var allTeamRatings = await GetAllTeamRatingsAsync();

                // Calculate player's percentile in the current rating distribution
                double playerPercentile = CalculatePlayerPercentile(teamRating, allTeamRatings);

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

                // Calculate and return the variety bonus with tail scaling
                return CalculateVarietyBonus(teamVarietyEntropy, averageVarietyEntropy, teamMatchesPlayed, averageMatchesPlayed, playerPercentile);
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
            double averageMatchesPlayed,
            double? playerPercentile = null)
        {
            // Calculate how far the team's variety entropy is from the average
            double entropyDifference = teamVarietyEntropy - averageVarietyEntropy;

            // Bonus scales proportionally with difference from average
            double relativeDiff = entropyDifference / (averageVarietyEntropy == 0 ? 1 : averageVarietyEntropy);

            // Scale the bonus based on games played relative to average
            double gamesPlayedRatio = Math.Min(teamMatchesPlayed / averageMatchesPlayed, 1.0);

            // Apply quadratic scaling based on games played relative to average
            // Formula: min_scaling_factor + (1.0 - min_scaling_factor) * games_played_ratio²
            double minScalingFactor = 0.5; // 50% of the max bonus
            double scalingFactor = minScalingFactor + (1.0 - minScalingFactor) * gamesPlayedRatio * gamesPlayedRatio;

            // Apply the scaling factor to the relative difference
            double scaledDiff = relativeDiff * scalingFactor;

            // Calculate base bonus
            double baseBonus = scaledDiff * MAX_VARIETY_BONUS;

            // Apply distribution scaling for players at the extremes
            // This helps players at the top and bottom 10% who have fewer opponents at their skill level
            if (playerPercentile.HasValue)
            {
                // Calculate distance from the middle (50th percentile)
                // 0 = at 50th percentile, 1 = at 0th or 100th percentile
                double distanceFromMiddle = Math.Abs(playerPercentile.Value - 50.0) / 50.0;

                // Only apply scaling to top and bottom 10% (distance > 0.8)
                if (distanceFromMiddle > TAIL_SCALING_THRESHOLD)
                {
                    // Use sigmoid-based scaling for smooth curve
                    double bonusMultiplier;
                    if (playerPercentile.Value >= 90)
                    {
                        // Top 10%: use sigmoid curve from 90-100 percentile
                        bonusMultiplier = CalculateScaledBonusCurve((int)playerPercentile.Value);
                    }
                    else
                    {
                        // Bottom 10%: mirror the top 10% scaling
                        // Map 10th percentile to 90th, 1st to 99th, etc.
                        int mirroredPercentile = 100 - (int)playerPercentile.Value;
                        bonusMultiplier = CalculateScaledBonusCurve(mirroredPercentile);
                    }

                    // Apply the bonus multiplier
                    // The scaling should always INCREASE variety bonuses for players at extremes
                    // to compensate for naturally lower variety due to fewer opponents
                    // Convert sigmoid output (0.1 to 1.0) to increase factor (1.1 to 2.0)
                    double increaseFactor = 1.0 + bonusMultiplier;

                    // Apply symmetric scaling: both positive and negative get the same relative improvement
                    // e.g., if +5% becomes +10% (100% increase), then -5% should become 0% (100% improvement)
                    if (baseBonus >= 0)
                    {
                        // Positive bonus: increase by the increase factor
                        // e.g., 0.05 (5% bonus) with 100% scaling becomes 0.1 (10% bonus)
                        baseBonus *= increaseFactor;
                    }
                    else
                    {
                        // Negative penalty: move toward 0 by the same relative amount
                        // e.g., -0.05 (5% penalty) with 100% scaling becomes 0.0 (0% penalty)
                        // This gives the same relative improvement as positive bonuses
                        baseBonus = baseBonus * (2.0 - increaseFactor);
                    }
                }
            }

            return Math.Clamp(baseBonus, MIN_VARIETY_BONUS, MAX_VARIETY_BONUS);
        }

        private async Task<(int Max, int Min)> GetCurrentRatingRangeAsync()
        {
            // Get all team ratings from Season system
            var allRatings = new List<int>();

            foreach (GameSize gameSize in Enum.GetValues(typeof(GameSize)))
            {
                if (gameSize != GameSize.OneVOne) // Teams don't participate in 1v1
                {
                    var ratings = await _seasonRatingService.GetAllTeamRatingsAsync(gameSize);
                    allRatings.AddRange(ratings.Values);
                }
            }

            if (!allRatings.Any())
                return (2100, 1000); // Fallback

            return (allRatings.Max(), allRatings.Min());
        }

        /// <summary>
        /// Gets all team ratings for percentile calculation.
        /// </summary>
        /// <returns>List of all team ratings.</returns>
        private async Task<List<int>> GetAllTeamRatingsAsync()
        {
            var allRatings = new List<int>();

            foreach (GameSize gameSize in Enum.GetValues(typeof(GameSize)))
            {
                if (gameSize != GameSize.OneVOne) // Teams don't participate in 1v1
                {
                    var ratings = await _seasonRatingService.GetAllTeamRatingsAsync(gameSize);
                    allRatings.AddRange(ratings.Values);
                }
            }

            return allRatings;
        }



        private async Task<Dictionary<string, double>> GetTeamOpponentDistributionAsync(
            string teamId,
            (int Max, int Min) ratingRange,
            GameSize gameSize)
        {
            DateTime startDate;
            if (UseTimeBasedVarietyFiltering)
            {
                startDate = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS);
            }
            else
            {
                var seasonRequest = new GetActiveSeasonRequest();
                var seasonResponse = await _eventBus.RequestAsync<GetActiveSeasonRequest, GetActiveSeasonResponse>(seasonRequest);
                startDate = seasonResponse?.Season?.StartDate ?? DateTime.UtcNow.AddDays(-365); // 1 year ago as fallback
            }

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
            DateTime startDate;
            if (UseTimeBasedVarietyFiltering)
            {
                startDate = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS);
            }
            else
            {
                var seasonRequest = new GetActiveSeasonRequest();
                var seasonResponse = await _eventBus.RequestAsync<GetActiveSeasonRequest, GetActiveSeasonResponse>(seasonRequest);
                startDate = seasonResponse?.Season?.StartDate ?? DateTime.UtcNow.AddDays(-365); // 1 year ago as fallback
            }

            var request = new AllTeamOpponentDistributionsRequest
            {
                Since = startDate
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
            DateTime startDate;
            if (UseTimeBasedVarietyFiltering)
            {
                startDate = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS);
            }
            else
            {
                var seasonRequest = new GetActiveSeasonRequest();
                var seasonResponse = await _eventBus.RequestAsync<GetActiveSeasonRequest, GetActiveSeasonResponse>(seasonRequest);
                startDate = seasonResponse?.Season?.StartDate ?? DateTime.UtcNow.AddDays(-365); // 1 year ago as fallback
            }

            var request = new TeamGamesPlayedRequest
            {
                Since = startDate
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
            DateTime startDate;
            if (UseTimeBasedVarietyFiltering)
            {
                startDate = DateTime.UtcNow.AddDays(-VARIETY_WINDOW_DAYS);
            }
            else
            {
                var seasonRequest = new GetActiveSeasonRequest();
                var seasonResponse = await _eventBus.RequestAsync<GetActiveSeasonRequest, GetActiveSeasonResponse>(seasonRequest);
                startDate = seasonResponse?.Season?.StartDate ?? DateTime.UtcNow.AddDays(-365); // 1 year ago as fallback
            }

            var request = new ScrimmageHistoryRequest
            {
                TeamId = teamId,
                Since = startDate
            };

            var response = await _eventBus.RequestAsync<ScrimmageHistoryRequest, ScrimmageHistoryResponse>(request);
            return response?.Matches?.Count() ?? 0;
        }

        /// <summary>
        /// Calculates individual rating multipliers for each team based on their confidence and variety bonus.
        /// </summary>
        public async Task<(double Team1Multiplier, double Team2Multiplier)> CalculateRatingMultipliersAsync(
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

                // Get games played for each team to determine if variety bonuses should be applied
                var team1GamesPlayed = await GetTeamMatchesPlayedAsync(team1Id, gameSize);
                var team2GamesPlayed = await GetTeamMatchesPlayedAsync(team2Id, gameSize);

                // Calculate individual multipliers for each team
                // Only apply variety bonuses after threshold games (matching Python implementation)
                var team1VarietyEffect = team1GamesPlayed >= VARIETY_BONUS_GAMES_THRESHOLD ? team1VarietyBonus : 0.0;
                var team2VarietyEffect = team2GamesPlayed >= VARIETY_BONUS_GAMES_THRESHOLD ? team2VarietyBonus : 0.0;

                // Calculate confidence multipliers (2.0 - confidence for lower confidence = higher multiplier)
                var team1ConfidenceMultiplier = 2.0 - team1Confidence;
                var team2ConfidenceMultiplier = 2.0 - team2Confidence;

                // Calculate final multipliers: confidence_multiplier + variety_effect
                var team1Multiplier = team1ConfidenceMultiplier + team1VarietyEffect;
                var team2Multiplier = team2ConfidenceMultiplier + team2VarietyEffect;

                // Clamp multipliers to maximum value only (matching Python implementation)
                team1Multiplier = Math.Min(team1Multiplier, MAX_MULTIPLIER);
                team2Multiplier = Math.Min(team2Multiplier, MAX_MULTIPLIER);

                return (team1Multiplier, team2Multiplier);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                throw;
            }
        }



        /// <summary>
        /// Calculates the rating change for a match using the ELO formula with individual multipliers.
        /// </summary>
        public async Task<(double Team1Change, double Team2Change)> CalculateRatingChangeAsync(
            string team1Id,
            string team2Id,
            int team1Rating,
            int team2Rating,
            GameSize gameSize)
        {
            try
            {
                // Calculate expected outcome using ELO formula
                double expectedOutcome = 1.0 / (1.0 + Math.Pow(10.0, (team2Rating - team1Rating) / 400.0));

                // Calculate base rating change (K * (actual - expected))
                // For team1, actual outcome is 1 if they win, 0 if they lose
                double baseChange = BASE_RATING_CHANGE * (1.0 - expectedOutcome);

                // Get individual multipliers for each team
                var (team1Multiplier, team2Multiplier) = await CalculateRatingMultipliersAsync(team1Id, team2Id, team1Rating, team2Rating, gameSize);

                // Calculate rating gap scaling to prevent shadow-boxing
                // Get current rating range to calculate max gap (20% of total range)
                var ratingRange = await GetCurrentRatingRangeAsync();
                double maxGap = (ratingRange.Max - ratingRange.Min) * 0.2; // 20% of total range
                double ratingGap = Math.Abs(team1Rating - team2Rating);

                // Initialize gap scaling (no effect by default)
                double gapScaling = 1.0;

                if (ratingGap <= maxGap)
                {
                    // normalized_gap is rating_gap normalized to 1.0 (0.0 to 1.0)
                    double normalizedGap = ratingGap / maxGap;
                    // Use cosine scaling: (1 + cos(π * normalized_gap * 0.7)) / 2
                    double cosineScaling = (1.0 + Math.Cos(Math.PI * normalizedGap * 0.7)) / 2.0;

                    // Get confidence levels to determine if gap scaling should be applied
                    var team1Confidence = await CalculateConfidenceAsync(team1Id, gameSize);
                    var team2Confidence = await CalculateConfidenceAsync(team2Id, gameSize);

                    // Only apply gap scaling to higher-rated player if lower-rated player has 1.0 confidence
                    if (team1Rating > team2Rating && team2Confidence >= 1.0)
                    {
                        // Team1 is higher rated, apply scaling to their change
                        gapScaling = cosineScaling;
                    }
                    else if (team2Rating > team1Rating && team1Confidence >= 1.0)
                    {
                        // Team2 is higher rated, apply scaling to their change
                        gapScaling = cosineScaling;
                    }
                }

                // Calculate final rating changes with gap scaling
                double team1Change = baseChange * team1Multiplier;
                double team2Change = -baseChange * team2Multiplier;

                // Apply gap scaling to the appropriate team
                if (team1Rating > team2Rating)
                {
                    // Team1 is higher rated, apply gap scaling to their change
                    team1Change *= gapScaling;
                }
                else if (team2Rating > team1Rating)
                {
                    // Team2 is higher rated, apply gap scaling to their change
                    team2Change *= gapScaling;
                }

                return (team1Change, team2Change);
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                throw;
            }
        }



        /// <summary>
        /// Calculates the confidence level for a team based on games played.
        /// </summary>
        public async Task<double> CalculateConfidenceAsync(string teamId, GameSize gameSize)
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
                // Uses exponential decay formula: confidence = max_confidence * (1 - e^(-k * games_played / max_confidence_games))
                // where k = 3.0 gives a good balance of quick early growth and smooth leveling
                int totalGames = response.Matches.Count();

                if (totalGames >= VARIETY_BONUS_GAMES_THRESHOLD)
                    return 1.0; // Max confidence at 20 games and stays there

                // Calculate how far along we are in the confidence growth (0 to 1)
                double progress = totalGames / (double)VARIETY_BONUS_GAMES_THRESHOLD;

                // Use exponential decay formula: 1 - e^(-k * x)
                // k = 3.0 gives a good balance of quick early growth and smooth leveling
                const double k = 3.0;
                double confidence = 1.0 * (1.0 - Math.Exp(-k * progress));

                return confidence;
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
                throw;
            }
        }
    }
}
