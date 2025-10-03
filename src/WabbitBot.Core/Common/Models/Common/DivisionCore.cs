using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Interfaces;

namespace WabbitBot.Core.Common.Models.Common
{
    /// <summary>
    /// Core business logic for Division entity and related statistics tracking.
    /// Handles division performance tracking, skill curves, and map density analytics.
    /// </summary>
    public class DivisionCore : IDivisionCore
    {
        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        #region Statistics Management

        /// <inheritdoc />
        public async Task UpdateDivisionStatsAsync(Guid divisionId, Guid teamId, bool won, double gameDurationMinutes, Guid mapId, Guid? seasonId = null)
        {
            // TODO: Implement division stats update logic
            // 1. Load or create DivisionStats for this division/teamSize/season
            // 2. Update games played, won/lost counts
            // 3. Recalculate winrate
            // 4. Update total/average game duration
            // 5. Update DensityPerformance for the map's density
            // 6. Update GameLengthPerformance bucket:
            //    - Get bucket key using GameLengthBuckets.GetBucketKey(gameDurationMinutes)
            //    - Create bucket if not exists using GameLengthBuckets.CreateEmptyBucket()
            //    - Increment GamesPlayed, GamesWon/GamesLost
            //    - Recalculate bucket Winrate
            //    - Call GameLengthBuckets.RecalculatePercentages() to update distribution
            // 7. Also update DivisionMapStats for this specific map (same fields including GameLengthPerformance)
            // 8. Save changes
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task UpdateLearningCurveAsync(Guid teamId, Guid divisionId, bool won, double gameDurationMinutes, Guid? seasonId = null)
        {
            // TODO: Implement learning curve update logic
            // 1. Load or create DivisionLearningCurve for this team/division/season
            // 2. Increment TotalGamesPlayed and TotalGamesWon
            // 3. Query all game results for this team/division/season from game history
            // 4. Fit multiple curve models (Linear, Logarithmic, PowerLaw, ExponentialApproach)
            // 5. Select best model based on R² (goodness of fit)
            // 6. Store model type, parameters, and R² in DivisionLearningCurve
            // 7. Set IsReliable = true if TotalGamesPlayed >= LearningCurveHelpers.MinGamesForReliableFit
            // 8. Save changes
            await Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<DivisionLearningCurve?> GetLearningCurveAsync(Guid teamId, Guid divisionId, Guid? seasonId = null)
        {
            // TODO: Implement learning curve retrieval
            // Query DivisionLearningCurve by teamId, divisionId, and optional seasonId
            await Task.CompletedTask;
            return null;
        }

        /// <inheritdoc />
        public async Task<double?> PredictWinrateAsync(Guid teamId, Guid divisionId, int gameCount, Guid? seasonId = null)
        {
            // TODO: Implement winrate prediction
            // 1. Get learning curve for this team/division/season
            // 2. Use LearningCurveHelpers.EvaluateCurve to predict winrate at gameCount
            await Task.CompletedTask;
            var curve = await GetLearningCurveAsync(teamId, divisionId, seasonId);
            return curve is not null ? LearningCurveHelpers.EvaluateCurve(curve, gameCount) : null;
        }

        /// <inheritdoc />
        public async Task<DivisionStats?> GetDivisionStatsAsync(Guid divisionId, TeamSize? teamSize = null, Guid? seasonId = null, RatingTier ratingTier = RatingTier.All)
        {
            // TODO: Implement division stats retrieval
            // 1. Query DivisionStats by divisionId, optional teamSize, and optional seasonId
            // 2. If ratingTier != All, get active breakpoints and filter by team ratings in tier range
            // 3. Aggregate stats across filtered teams
            await Task.CompletedTask;
            return null;
        }

        /// <inheritdoc />
        public async Task<DivisionMapStats?> GetDivisionMapStatsAsync(Guid divisionId, Guid mapId, TeamSize? teamSize = null, Guid? seasonId = null, RatingTier ratingTier = RatingTier.All)
        {
            // TODO: Implement division map stats retrieval
            // 1. Query DivisionMapStats by divisionId, mapId, optional teamSize, and optional seasonId
            // 2. If ratingTier != All, filter by teams in the specified tier's rating range
            await Task.CompletedTask;
            return null;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DivisionLearningCurve>> GetLearningCurvesAsync(Guid divisionId, Guid? seasonId = null, RatingTier ratingTier = RatingTier.All)
        {
            // TODO: Implement learning curves retrieval
            // 1. Query DivisionLearningCurve records for this division/season
            // 2. If ratingTier != All, join with Team ratings and filter by tier's rating range
            // 3. Return matching learning curve records (only reliable curves if IsReliable = true)
            await Task.CompletedTask;
            return Enumerable.Empty<DivisionLearningCurve>();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<DivisionPerformanceSummary>> GetTopDivisionsAsync(TeamSize teamSize, Guid? seasonId = null, RatingTier ratingTier = RatingTier.All, int limit = 10)
        {
            // TODO: Implement top divisions query
            // 1. Query all DivisionLearningCurve records for the team size
            // 2. If ratingTier != All, get active breakpoints and filter by team ratings in tier's range
            // 3. Group by division and aggregate performance metrics
            // 4. Calculate average winrate, games played, etc. for each division
            // 5. Order by winrate descending and take top N
            // 6. Return summary objects with division info and performance data
            await Task.CompletedTask;
            return Enumerable.Empty<DivisionPerformanceSummary>();
        }

        #endregion

        #region Analytics & Insights

        /// <summary>
        /// Calculates the average learning rate for a division across all players.
        /// Higher values indicate steeper learning curves (high skill ceiling).
        /// </summary>
        public static double CalculateAverageLearningRate(DivisionLearningCurve curve, int gameCount = 10)
        {
            // TODO: Implement average learning rate calculation
            // Use LearningCurveHelpers.GetLearningRate at specified game count
            // Return the derivative (rate of improvement) at that point
            var rate = LearningCurveHelpers.GetLearningRate(curve, gameCount);
            return rate ?? 0.0;
        }

        /// <summary>
        /// Determines if a division is beginner-friendly based on learning curve data.
        /// Beginner-friendly divisions have high initial winrates and low learning rates.
        /// </summary>
        public static bool IsBeginnerFriendly(DivisionLearningCurve curve, double thresholdWinrate = 0.45, double maxLearningRate = 0.01)
        {
            // TODO: Implement beginner-friendly analysis
            // 1. Check if predicted winrate at game 1 is above threshold
            // 2. Check if learning rate is low (indicating easy to master)
            var initialWinrate = LearningCurveHelpers.EvaluateCurve(curve, 1);
            var learningRate = LearningCurveHelpers.GetLearningRate(curve, 5);

            return initialWinrate >= thresholdWinrate &&
                   learningRate is not null &&
                   learningRate <= maxLearningRate;
        }

        /// <summary>
        /// Gets the optimal map density for a division based on performance data.
        /// </summary>
        public static MapDensity? GetOptimalDensity(DivisionStats stats)
        {
            // TODO: Implement optimal density calculation
            // Find density with highest winrate
            // Require minimum games played threshold
            return null;
        }

        /// <summary>
        /// Calculates performance variance across map densities.
        /// High variance indicates division is density-dependent.
        /// </summary>
        public static double CalculateDensityVariance(DivisionStats stats)
        {
            // TODO: Implement density variance calculation
            // Calculate standard deviation of winrates across densities
            return 0.0;
        }

        /// <summary>
        /// Gets data for "Win Rate vs Game Length" graph.
        /// Returns all buckets with their corresponding winrates.
        /// </summary>
        /// <param name="stats">Division statistics containing game length performance data</param>
        /// <returns>Dictionary with bucket start minutes as keys and winrates as values</returns>
        public static Dictionary<int, double> GetWinrateByGameLengthData(DivisionStats stats)
        {
            return stats.GameLengthPerformance
                .Where(kvp => kvp.Value.GamesPlayed > 0)
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Winrate);
        }

        /// <summary>
        /// Gets data for "Game Length Distribution" graph.
        /// Returns all buckets with their percentage of total games.
        /// </summary>
        /// <param name="stats">Division statistics containing game length performance data</param>
        /// <returns>Dictionary with bucket start minutes as keys and percentages as values</returns>
        public static Dictionary<int, double> GetGameLengthDistributionData(DivisionStats stats)
        {
            return stats.GameLengthPerformance
                .Where(kvp => kvp.Value.GamesPlayed > 0)
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.PercentageOfTotal);
        }

        /// <summary>
        /// Gets the most common game length bucket for a division.
        /// </summary>
        public static int? GetMostCommonGameLength(DivisionStats stats)
        {
            var bucket = stats.GameLengthPerformance
                .Where(kvp => kvp.Value.GamesPlayed > 0)
                .OrderByDescending(kvp => kvp.Value.GamesPlayed)
                .FirstOrDefault();

            return bucket.Key != 0 || bucket.Value is not null ? bucket.Key : (int?)null;
        }

        /// <summary>
        /// Gets the game length bucket with the highest winrate.
        /// Requires minimum games threshold to avoid statistical noise.
        /// </summary>
        public static int? GetOptimalGameLength(DivisionStats stats, int minGamesThreshold = 10)
        {
            var bucket = stats.GameLengthPerformance
                .Where(kvp => kvp.Value.GamesPlayed >= minGamesThreshold)
                .OrderByDescending(kvp => kvp.Value.Winrate)
                .FirstOrDefault();

            return bucket.Key != 0 || bucket.Value is not null ? bucket.Key : (int?)null;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Static validation methods for division-related operations.
        /// </summary>
        public static class Validation
        {
            /// <summary>
            /// Validates if a division name is valid.
            /// </summary>
            public static bool IsValidDivisionName(string name)
            {
                return !string.IsNullOrWhiteSpace(name) && name.Length <= 100;
            }

            /// <summary>
            /// Validates if a faction value is valid.
            /// </summary>
            public static bool IsValidFaction(string faction)
            {
                return faction is "BLUFOR" or "REDFOR";
            }

            /// <summary>
            /// Validates if a division icon filename is valid.
            /// </summary>
            public static bool IsValidIconFilename(string? filename)
            {
                if (string.IsNullOrWhiteSpace(filename))
                    return true; // Icon is optional

                var extension = System.IO.Path.GetExtension(filename).ToLowerInvariant();
                return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp";
            }

            /// <summary>
            /// Validates if a division configuration is complete.
            /// </summary>
            public static bool IsCompleteDivisionConfiguration(Division division)
            {
                return IsValidDivisionName(division.Name) &&
                       Enum.IsDefined(typeof(Faction), division.Faction) &&
                       IsValidIconFilename(division.IconFilename);
            }

            /// <summary>
            /// Validates if there are enough games to calculate a reliable learning curve.
            /// </summary>
            public static bool HasSufficientDataForLearningCurve(int totalGamesPlayed)
            {
                return totalGamesPlayed >= LearningCurveHelpers.MinGamesForReliableFit;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Determines if a game should be tracked for learning curve purposes (1v1 only).
        /// </summary>
        public static bool ShouldTrackLearningCurve(TeamSize teamSize)
        {
            return teamSize == TeamSize.OneVOne;
        }

        /// <summary>
        /// Calculates winrate from win/loss counts.
        /// </summary>
        public static double CalculateWinrate(int gamesWon, int gamesPlayed)
        {
            if (gamesPlayed == 0)
                return 0.0;

            return (double)gamesWon / gamesPlayed;
        }

        /// <summary>
        /// Calculates average game duration.
        /// </summary>
        public static double CalculateAverageDuration(double totalDurationMinutes, int gamesPlayed)
        {
            if (gamesPlayed == 0)
                return 0.0;

            return totalDurationMinutes / gamesPlayed;
        }

        /// <summary>
        /// Determines which percentile tier a rating belongs to based on current breakpoints.
        /// </summary>
        public static RatingTier GetRatingTierForRating(double rating, RatingPercentileBreakpoints breakpoints)
        {
            // Check tiers from most exclusive (Top10Plus) to least exclusive (Top90Plus)
            if (rating >= breakpoints.Breakpoints.GetValueOrDefault(RatingTier.Top10Plus, double.MaxValue))
                return RatingTier.Top10Plus;
            if (rating >= breakpoints.Breakpoints.GetValueOrDefault(RatingTier.Top20Plus, double.MaxValue))
                return RatingTier.Top20Plus;
            if (rating >= breakpoints.Breakpoints.GetValueOrDefault(RatingTier.Top30Plus, double.MaxValue))
                return RatingTier.Top30Plus;
            if (rating >= breakpoints.Breakpoints.GetValueOrDefault(RatingTier.Top40Plus, double.MaxValue))
                return RatingTier.Top40Plus;
            if (rating >= breakpoints.Breakpoints.GetValueOrDefault(RatingTier.Top50Plus, double.MaxValue))
                return RatingTier.Top50Plus;
            if (rating >= breakpoints.Breakpoints.GetValueOrDefault(RatingTier.Top60Plus, double.MaxValue))
                return RatingTier.Top60Plus;
            if (rating >= breakpoints.Breakpoints.GetValueOrDefault(RatingTier.Top70Plus, double.MaxValue))
                return RatingTier.Top70Plus;
            if (rating >= breakpoints.Breakpoints.GetValueOrDefault(RatingTier.Top80Plus, double.MaxValue))
                return RatingTier.Top80Plus;
            if (rating >= breakpoints.Breakpoints.GetValueOrDefault(RatingTier.Top90Plus, double.MaxValue))
                return RatingTier.Top90Plus;

            return RatingTier.All; // Below all percentile thresholds
        }

        /// <summary>
        /// Gets the minimum rating threshold for a specific tier from breakpoints.
        /// </summary>
        public static double GetMinRatingForTier(RatingTier tier, RatingPercentileBreakpoints breakpoints)
        {
            if (tier == RatingTier.All)
                return 0.0;

            return breakpoints.Breakpoints.GetValueOrDefault(tier, 0.0);
        }

        /// <summary>
        /// Gets the display name for a rating tier with current rating threshold.
        /// </summary>
        public static string GetTierDisplayName(RatingTier tier, RatingPercentileBreakpoints? breakpoints = null)
        {
            if (tier == RatingTier.All)
                return "All Players";

            var percentile = GetPercentileForTier(tier);

            if (breakpoints is not null && breakpoints.Breakpoints.TryGetValue(tier, out var minRating))
                return $"Top {percentile}% ({minRating:F0}+)";

            return $"Top {percentile}%";
        }

        /// <summary>
        /// Gets the percentile value for a tier (10, 20, 30, etc.).
        /// </summary>
        public static int GetPercentileForTier(RatingTier tier)
        {
            return tier switch
            {
                RatingTier.Top90Plus => 90,
                RatingTier.Top80Plus => 80,
                RatingTier.Top70Plus => 70,
                RatingTier.Top60Plus => 60,
                RatingTier.Top50Plus => 50,
                RatingTier.Top40Plus => 40,
                RatingTier.Top30Plus => 30,
                RatingTier.Top20Plus => 20,
                RatingTier.Top10Plus => 10,
                _ => 100, // All
            };
        }

        #endregion

        #region Percentile Breakpoint Management

        /// <inheritdoc />
        public async Task<RatingPercentileBreakpoints> CalculatePercentileBreakpointsAsync(TeamSize teamSize, Guid? seasonId = null, int expiryHours = 24)
        {
            // TODO: Implement percentile breakpoint calculation
            // 1. Query all active teams for this team size and season
            // 2. Get current ratings from ScrimmageTeamStats
            // 3. Filter out teams with too few games (e.g., < 10 games)
            // 4. Sort ratings and calculate percentile thresholds:
            //    - Top 10% = 90th percentile rating
            //    - Top 20% = 80th percentile rating
            //    - etc.
            // 5. Create new RatingPercentileBreakpoints entity
            // 6. Set IsActive = false on old breakpoints
            // 7. Save and return new breakpoints
            await Task.CompletedTask;

            return new RatingPercentileBreakpoints
            {
                TeamSize = teamSize,
                SeasonId = seasonId,
                CalculatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(expiryHours),
                TotalPlayersInSample = 0,
                Breakpoints = new Dictionary<RatingTier, double>(),
                IsActive = true,
            };
        }

        /// <inheritdoc />
        public async Task<RatingPercentileBreakpoints?> GetActivePercentileBreakpointsAsync(TeamSize teamSize, Guid? seasonId = null)
        {
            // TODO: Implement active breakpoints retrieval
            // 1. Query RatingPercentileBreakpoints for teamSize/seasonId where IsActive = true
            // 2. If found and not expired, return it
            // 3. If expired or not found, call CalculatePercentileBreakpointsAsync
            // 4. Cache the result for fast subsequent access
            await Task.CompletedTask;
            return null;
        }

        #endregion
    }

    #region DivisionPerformanceSummary
    /// <summary>
    /// Summary of division performance, used for ranking and comparison.
    /// </summary>
    public class DivisionPerformanceSummary
    {
        /// <summary>
        /// The division this summary applies to.
        /// </summary>
        public Guid DivisionId { get; set; }
        public Division? Division { get; set; }

        /// <summary>
        /// Division name for display.
        /// </summary>
        public string DivisionName { get; set; } = string.Empty;

        /// <summary>
        /// Division faction.
        /// </summary>
        public Faction Faction { get; set; }

        /// <summary>
        /// Total games played with this division.
        /// </summary>
        public int TotalGamesPlayed { get; set; }

        /// <summary>
        /// Overall winrate across all tracked games.
        /// </summary>
        public double Winrate { get; set; }

        /// <summary>
        /// Number of unique teams that have used this division.
        /// </summary>
        public int UniqueTeamsCount { get; set; }

        /// <summary>
        /// Average game duration with this division.
        /// </summary>
        public double AverageGameDurationMinutes { get; set; }

        /// <summary>
        /// Pick rate (percentage of games where this division was chosen).
        /// </summary>
        public double PickRate { get; set; }

        /// <summary>
        /// Rank among all divisions (1 = highest winrate).
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Performance on different map densities.
        /// </summary>
        public Dictionary<MapDensity, double> DensityWinrates { get; set; } = new();
    }
    #endregion
}

