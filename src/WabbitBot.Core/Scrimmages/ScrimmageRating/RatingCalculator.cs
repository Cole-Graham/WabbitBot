using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Scrimmages
{
    public partial class ScrimmageCore
    {
        public static class RatingCalculator
        {
            // Configuration option to enable/disable time-based match filtering for variety calculations
            // Can be changed at runtime through configuration
            private static bool UseTimeBasedVarietyFiltering { get; set; } = false;

            // Tail scaling constants for variety bonus
            // TODO: Instead of an arbitrary threshold & multiplier, normalize variety bonus according to # of players in their rating range
            private const double TAIL_SCALING_THRESHOLD = 0.8; // 80% distance from middle
            private const double SIGMOID_GROWTH_RATE = 1.1;
            private const double SIGMOID_MIDPOINT = 8.0;
            private const double MIN_SCALING_FACTOR = 0.1;
            private const double MAX_SCALING_FACTOR = 1.0;

            // Database access is now through CoreService static accessors

            /// <summary>
            /// Gets all team ratings for a specific game size.
            /// Note: Since we can't access season data directly due to architectural constraints,
            /// this method queries all teams and returns their ratings for the specified game size.
            /// </summary>
            private static async Task<Dictionary<string, double>> GetAllTeamRatingsAsync(TeamSize TeamSize)
            {
                try
                {
                    // Get all teams from the repository
                    var allTeams = await CoreService.Teams.GetAllAsync(DatabaseComponent.Repository);
                    var ratings = new Dictionary<string, double>();

                    foreach (var team in allTeams.Data ?? Enumerable.Empty<Team>())
                    {
                        if (team.ScrimmageTeamStats.ContainsKey(TeamSize))
                        {
                            ratings[team.Id.ToString()] = team.ScrimmageTeamStats[TeamSize].CurrentRating;
                        }
                    }

                    return ratings;
                }
                catch (Exception ex)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        ex,
                        "Failed to get all team ratings",
                        nameof(GetAllTeamRatingsAsync)
                    );
                    return new Dictionary<string, double>();
                }
            }

            private static ScrimmageOptions GetScrimmageDbConfig()
            {
                return ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
            }

            /// <summary>
            /// Calculates the percentile of a player's rating in the current rating distribution.
            /// </summary>
            /// <param name="playerRating">The player's current rating.</param>
            /// <param name="allRatings">A sorted list of all player ratings.</param>
            /// <returns>The percentile (0.0 to 100.0) of the player's rating.</returns>
            private static double CalculatePlayerPercentile(double playerRating, List<double> allRatings)
            {
                if (allRatings == null || !allRatings.Any())
                    return 50.0; // Default to middle if no ratings

                // Find the position of this rating in the sorted list
                // Count how many ratings are less than this rating
                double countBelow = 0;
                double countEqual = 0;

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
                if (percentile < 90.0 || percentile > 100.0)
                    throw new ArgumentException("Percentile must be between 90-100 for tail scaling");

                // Map percentile to x in range [1, 10]
                double x = percentile - 89;

                double k = SIGMOID_GROWTH_RATE; // Growth rate
                double x0 = SIGMOID_MIDPOINT; // Midpoint

                double raw = 1.0 / (1.0 + Math.Exp(-k * (x - x0)));
                double minRaw = 1.0 / (1.0 + Math.Exp(-k * (1.0 - x0)));
                double maxRaw = 1.0 / (1.0 + Math.Exp(-k * (10.0 - x0)));

                double scaled = (raw - minRaw) / (maxRaw - minRaw); // Normalize to [0, 1]
                return MIN_SCALING_FACTOR + scaled * (MAX_SCALING_FACTOR - MIN_SCALING_FACTOR); // Scale to [0.1, 1.0]
            }

            #region CalculateAllDistributionScoresAsync
            public static async Task<List<(Guid, Dictionary<Guid, double>)>> CalculateAllDistributionScoresAsync(
                TeamSize teamSize
            )
            {
                try
                {
                    // Get all relevant data
                    var allTeams = await CoreService.Teams.GetAllAsync(DatabaseComponent.Repository);
                    var teamVarietyStatsData = new DatabaseService<TeamVarietyStats>();
                    var opponentEncounterData = new DatabaseService<TeamOpponentEncounter>();

                    // Calculate variety scores using pre-computed TeamVarietyStats
                    var teamVarietyScores = new Dictionary<Guid, double>();

                    foreach (var team in allTeams.Data ?? Enumerable.Empty<Team>())
                    {
                        // Try to get pre-computed variety stats for this team and team size
                        if (team.VarietyStats.FirstOrDefault(vs => vs.TeamSize == teamSize) is { } varietyStats)
                        {
                            // Use the pre-computed variety score
                            teamVarietyScores[team.Id] = TeamCore.ScrimmageStats.GetVarietyScore(varietyStats);
                        }
                        else
                        {
                            // Fallback: calculate on-demand using recent opponent encounters
                            var recentEncounters =
                                team.ScrimmageTeamStats[teamSize]
                                    .OpponentEncounters?.Where(oe => oe.TeamSize == teamSize)
                                    .OrderByDescending(oe => oe.EncounteredAt)
                                    .Take(50) // Last 50 encounters for performance
                                    .ToList()
                                ?? new List<TeamOpponentEncounter>();

                            if (recentEncounters.Any())
                            {
                                // Calculate variety score from recent encounters
                                var uniqueOpponents = recentEncounters.Select(e => e.OpponentId).Distinct().Count();
                                var totalEncounters = recentEncounters.Count;

                                // Calculate entropy
                                var entropy = 0.0;
                                var opponentGroups = recentEncounters.GroupBy(e => e.OpponentId);

                                foreach (var group in opponentGroups)
                                {
                                    var probability = (double)group.Count() / totalEncounters;
                                    entropy -= probability * Math.Log(probability);
                                }

                                // Normalize entropy
                                var maxEntropy = Math.Log(uniqueOpponents);
                                var normalizedEntropy = maxEntropy == 0 ? 0 : entropy / maxEntropy;

                                // Calculate bonus
                                var uniqueBonus = Math.Min(uniqueOpponents * 0.1, 1.0);
                                var repeatPenalty =
                                    totalEncounters > uniqueOpponents
                                        ? (totalEncounters - uniqueOpponents) * 0.05
                                        : 0.0;
                                var bonus = Math.Max(uniqueBonus - repeatPenalty, 0.0);

                                teamVarietyScores[team.Id] = (normalizedEntropy * 0.7) + (bonus * 0.3);
                            }
                            else
                            {
                                teamVarietyScores[team.Id] = 0.0; // No encounters = no variety
                            }
                        }
                    }

                    // Calculate global average variety score
                    double averageVarietyScore = teamVarietyScores.Values.Average();

                    // Calculate average opponent encounters (matches played)
                    var teamEncounterCounts = new Dictionary<Guid, int>();
                    foreach (var team in allTeams.Data ?? Enumerable.Empty<Team>())
                    {
                        // Count total opponent encounters for this team and team size
                        var encounterCount =
                            team.ScrimmageTeamStats[teamSize]
                                .OpponentEncounters?.Where(oe => oe.TeamSize == teamSize)
                                .Count()
                            ?? 0;
                        teamEncounterCounts[team.Id] = encounterCount;
                    }

                    double averageEncounters = teamEncounterCounts.Values.Average();

                    // Calculate final variety bonuses
                    var distributions = new List<(Guid, Dictionary<Guid, double>)>();

                    foreach (var team in allTeams.Data ?? Enumerable.Empty<Team>())
                    {
                        double teamVarietyScore = teamVarietyScores.GetValueOrDefault(team.Id, 0.0);
                        int teamEncounters = teamEncounterCounts.GetValueOrDefault(team.Id, 0);

                        // Calculate variety bonus using the new scoring system
                        double varietyBonus = CalculateVarietyBonus(
                            teamVarietyScore,
                            averageVarietyScore,
                            teamEncounters,
                            averageEncounters
                        );

                        distributions.Add((team.Id, new Dictionary<Guid, double> { { team.Id, varietyBonus } }));
                    }

                    return distributions;
                }
                catch (Exception ex)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        ex,
                        "Failed to calculate all team variety scores",
                        nameof(CalculateAllDistributionScoresAsync)
                    );
                    throw;
                }
            }
            #endregion

            /// <summary>
            /// Calculates Shannon entropy for a frequency distribution.
            /// Higher entropy indicates more diverse opponents.
            /// </summary>
            private static double CalculateShannonEntropy(IEnumerable<int> frequencies)
            {
                var total = frequencies.Sum();
                if (total == 0)
                    return 0.0;

                double entropy = 0.0;
                foreach (var frequency in frequencies)
                {
                    if (frequency > 0)
                    {
                        double probability = (double)frequency / total;
                        entropy -= probability * Math.Log2(probability);
                    }
                }
                return entropy;
            }

            /// <summary>
            /// Calculates variety bonus based on opponent diversity and match activity.
            /// Formula: relativeDiff * (1 - gamesPlayedFactor)
            /// </summary>
            private static double CalculateVarietyBonus(
                double teamVarietyEntropy,
                double averageVarietyEntropy,
                int teamMatchesPlayed,
                double averageMatchesPlayed
            )
            {
                // Calculate how far the team's variety entropy is from the average
                double entropyDifference = teamVarietyEntropy - averageVarietyEntropy;

                // Bonus scales proportionally with difference from average
                double relativeDiff = entropyDifference / (averageVarietyEntropy == 0 ? 1 : averageVarietyEntropy);

                // Scale the bonus based on games played relative to average
                double gamesPlayedFactor = Math.Min(teamMatchesPlayed / averageMatchesPlayed, 1.0);

                // Final bonus is proportional to entropy difference and inversely proportional to games played
                return relativeDiff * (1 - gamesPlayedFactor);
            }

            /// <summary>
            /// Calculates normalized weights for a team's opponents based on rating gaps and match counts.
            /// </summary>
            private static Dictionary<Guid, double> CalculateOpponentWeights(
                double teamRating,
                Dictionary<Guid, (int Count, double Rating)> opponentMatches,
                (double Max, double Min) ratingRange
            )
            {
                var weighted = new Dictionary<Guid, double>();
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
                    foreach (var key in weighted.Keys)
                    {
                        weighted[key] /= total;
                    }
                }

                return weighted;
            }

            #region CalculateVarietyBonus
            /// <summary>
            /// Calculates the variety bonus for a team based on their opponent distribution.
            /// </summary>
            // public static async Task<double> CalculateVarietyBonusAsync(
            //     Guid teamId,
            //     double teamRating,
            //     TeamSize TeamSize)
            // {
            //     try
            //     {
            //         // Get the current global rating range
            //         var ratingRange = await GetCurrentRatingRangeAsync();

            //         // Get all team ratings for percentile calculation
            //         var allTeamRatings = await GetAllTeamRatingsAsync();

            //         // Calculate player's percentile in the current rating distribution
            //         double playerPercentile = CalculatePlayerPercentile(teamRating, allTeamRatings);

            //         // Get team's opponent distribution
            //         var distribution = await GetTeamOpponentDistributionAsync(teamId, ratingRange, TeamSize);

            //         // Calculate team's variety entropy
            //         double teamVarietyEntropy = CalculateWeightedEntropy(distribution);

            //         // Calculate global average variety entropy
            //         double averageVarietyEntropy = await GetAverageEntropyAsync(ratingRange);

            //         // Get average matches played in the team's rating range
            //         double averageMatchesPlayed = await GetAverageGamesPlayedAsync(teamRating, ratingRange);

            //         // Get team's matches played
            //         int teamMatchesPlayed = await GetTeamMatchesPlayedAsync(teamId, TeamSize);

            //         // Calculate and return the variety bonus with tail scaling
            //         return CalculateVarietyBonus(teamVarietyEntropy, averageVarietyEntropy, teamMatchesPlayed, averageMatchesPlayed, playerPercentile);
            //     }
            //     catch (Exception ex)
            //     {
            //         await ErrorHandler.CaptureAsync(ex, "Failed to calculate variety bonus", nameof(CalculateVarietyBonusAsync));
            //         throw;
            //     }
            // }
            #endregion

            private static double CalculateWeightedEntropy(Dictionary<Guid, double> opponentWeights)
            {
                double entropy = 0.0;
                foreach (var weight in opponentWeights.Values)
                {
                    if (weight > 0)
                        entropy -= weight * Math.Log(weight, 2); // Shannon entropy
                }
                return entropy;
            }

            #region CalculateVarietyBonus
            private static double CalculateVarietyBonus(
                double teamVarietyEntropy,
                double averageVarietyEntropy,
                int teamMatchesPlayed,
                double averageMatchesPlayed,
                double? playerPercentile = null
            )
            {
                // Calculate how far the team's variety entropy is from the average
                double entropyDifference = teamVarietyEntropy - averageVarietyEntropy;

                // Bonus scales proportionally with difference from average
                double relativeDiff =
                    entropyDifference / (Math.Abs(averageVarietyEntropy) == 0 ? 1 : Math.Abs(averageVarietyEntropy));

                // Scale the bonus based on games played relative to average
                double gamesPlayedRatio = Math.Min(teamMatchesPlayed / averageMatchesPlayed, 1.0);

                // Apply quadratic scaling based on games played relative to average
                // Formula: min_scaling_factor + (1.0 - min_scaling_factor) * games_played_ratio²
                double minScalingFactor = 0.5; // 50% of the max bonus
                double scalingFactor =
                    minScalingFactor + (1.0 - minScalingFactor) * gamesPlayedRatio * gamesPlayedRatio;

                // Apply the scaling factor to the relative difference
                double scaledDiff = relativeDiff * scalingFactor;

                // Calculate base bonus
                double baseBonus = scaledDiff * GetScrimmageDbConfig().MaxVarietyBonus;

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
                // Return the bonus between the minimum and maximum bonus limits
                return Math.Clamp(
                    baseBonus,
                    GetScrimmageDbConfig().MinVarietyBonus,
                    GetScrimmageDbConfig().MaxVarietyBonus
                );
            }
            #endregion

            private static async Task<(double Max, double Min)> GetCurrentRatingRangeAsync()
            {
                // Get all team ratings from Season system
                var allRatings = new List<double>();

                foreach (TeamSize TeamSize in Enum.GetValues(typeof(TeamSize)))
                {
                    if (TeamSize != TeamSize.OneVOne) // Teams don't participate in 1v1
                    {
                        var ratings = await GetAllTeamRatingsAsync(TeamSize);
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
            private static async Task<List<double>> GetAllTeamRatingsAsync()
            {
                var allRatings = new List<double>();

                foreach (TeamSize TeamSize in Enum.GetValues(typeof(TeamSize)))
                {
                    if (TeamSize != TeamSize.OneVOne) // Teams don't participate in 1v1
                    {
                        var ratings = await GetAllTeamRatingsAsync(TeamSize);
                        allRatings.AddRange(ratings.Values);
                    }
                }

                return allRatings;
            }

            // TODO: Distinguish methods which need this for calculating needing variety score.
            // private static async Task<Dictionary<Guid, double>> GetTeamOpponentDistributionAsync(
            //     Guid teamId,
            //     (double Max, double Min) ratingRange,
            //     TeamSize TeamSize)
            // {
            //     DateTime startDate;
            //     if (UseTimeBasedVarietyFiltering)
            //     {
            //         startDate = DateTime.UtcNow.AddDays(-GetScrimmageDbConfig().VarietyWindowDays);
            //     }
            //     else
            //     {
            //         var seasonRequest = new GetActiveSeasonRequest();
            //         var seasonResponse = await EventBus.RequestAsync<GetActiveSeasonRequest, GetActiveSeasonResponse>(seasonRequest);
            //         startDate = seasonResponse?.Season?.StartDate ?? DateTime.UtcNow.AddDays(-365); // 1 year ago as fallback
            //     }

            //     var Opponent = await CoreService.Teams.GetByIdAsync(teamId, DatabaseComponent.Repository);
            //     if (Opponent == null)
            //     {
            //         throw new InvalidOperationException("Team not found");
            //     }
            //     else
            //     {
            //         if (Opponent.Stats.ContainsKey(TeamSize))
            //         {
            //             return CalculateOpponentWeights(Opponent.Stats[TeamSize].CurrentRating, Opponent.Stats[TeamSize].OpponentMatches, ratingRange);
            //         }
            //     }

            //     var request = new TeamOpponentStatsRequest
            //     {
            //         TeamId = teamId,
            //         Since = startDate,
            //         TeamSize = TeamSize
            //     };

            //     var response = await EventBus.RequestAsync<TeamOpponentStatsRequest, TeamOpponentStatsResponse>(request);
            //     if (response == null)
            //         throw new InvalidOperationException("Failed to retrieve team opponent statistics");

            //     return CalculateOpponentWeights(response.TeamRating, response.OpponentMatches, ratingRange);
            // }

            private static async Task<double> GetAverageEntropyAsync((double Max, double Min) ratingRange)
            {
                DateTime startDate;
                if (UseTimeBasedVarietyFiltering)
                {
                    startDate = DateTime.UtcNow.AddDays(-GetScrimmageDbConfig().VarietyWindowDays);
                }
                else
                {
                    // Fetch active season directly from database (no event payloads)
                    var seasons = await CoreService.Seasons.GetAllAsync(DatabaseComponent.Repository);
                    var activeSeason = seasons.Data?.FirstOrDefault(s => s.IsActive);
                    startDate = activeSeason?.StartDate ?? DateTime.UtcNow.AddDays(-365);
                }

                var stats = await CoreService.TeamVarietyStats.GetAllAsync(DatabaseComponent.Repository);
                var varietyEntropies = stats.Data?.Select(t => t.VarietyEntropy).ToList();
                if (varietyEntropies == null || varietyEntropies.Count == 0)
                    return 0;

                // Variety entropy mean across all teams
                // double totalVarietyEntropy = 0;
                // int count = 0;

                // TODO: Find out why it was originally a loop
                // foreach (var distribution in response.Distributions)
                // {
                //     double varietyEntropy = CalculateWeightedEntropy(distribution.NormalizedWeights);
                //     totalVarietyEntropy += varietyEntropy;
                //     count++;
                // }
                double meanVarietyEntropy = 0;
                meanVarietyEntropy = varietyEntropies.Average();

                // return count > 0 ? totalVarietyEntropy / count : 0;
                return meanVarietyEntropy;
            }

            // TODO: Find out what RecentMatchesCount represented in the design document
            // private static async Task<double> GetAverageGamesPlayedAsync(double teamRating, (double Max, double Min) ratingRange)
            // {
            //     DateTime startDate;
            //     if (UseTimeBasedVarietyFiltering)
            //     {
            //         startDate = DateTime.UtcNow.AddDays(-GetScrimmageDbConfig().VarietyWindowDays);
            //     }
            //     else
            //     {
            //         var seasonRequest = new GetActiveSeasonRequest();
            //         var seasonResponse = await EventBus.RequestAsync<GetActiveSeasonRequest, GetActiveSeasonResponse>(seasonRequest);
            //         startDate = seasonResponse?.Season?.StartDate ?? DateTime.UtcNow.AddDays(-365); // 1 year ago as fallback
            //     }

            //     var request = new TeamGamesPlayedRequest
            //     {
            //         Since = startDate
            //     };

            //     var response = await EventBus.RequestAsync<TeamGamesPlayedRequest, TeamGamesPlayedResponse>(request);
            //     if (response?.TeamStats == null || response.TeamStats.Count == 0)
            //         return 0;

            //     // Calculate dynamic range based on MaxGapPercent
            //     double range = ratingRange.Max - ratingRange.Min;
            //     double maxGap = range * GetScrimmageDbConfig().MaxGapPercent;

            //     // Calculate average games played for teams within the dynamic range
            //     var relevantTeams = response.TeamStats
            //         .Where(t => Math.Abs(t.Value.CurrentRating - teamRating) <= maxGap)
            //         .ToList();

            //     if (relevantTeams.Count == 0)
            //         return 0;

            //     return relevantTeams.Average(t => t.Value.RecentMatchesCount);
            // }

            private static async Task<int> GetTeamMatchesPlayedAsync(Guid teamId, TeamSize TeamSize)
            {
                DateTime startDate;
                if (UseTimeBasedVarietyFiltering)
                {
                    startDate = DateTime.UtcNow.AddDays(-GetScrimmageDbConfig().VarietyWindowDays);
                }
                else
                {
                    // Fetch active season directly from database (no event payloads)
                    var seasons = await CoreService.Seasons.GetAllAsync(DatabaseComponent.Repository);
                    var activeSeason = seasons.Data?.FirstOrDefault(s => s.IsActive);
                    startDate = activeSeason?.StartDate ?? DateTime.UtcNow.AddDays(-365);
                }

                // Fetch matches from database using TeamId
                var matchesResult = await CoreService.Matches.GetAllAsync(DatabaseComponent.Repository);
                return matchesResult
                        .Data?.Where(m => (m.Team1Id == teamId || m.Team2Id == teamId) && m.CompletedAt >= startDate)
                        .Count() ?? 0;
            }

            /// <summary>
            /// Calculates individual rating multipliers for each team based on their confidence and variety bonus.
            /// </summary>
            // public static async Task<(double Team1Multiplier, double Team2Multiplier)> CalculateRatingMultipliersAsync(
            // Guid team1Id,
            // Guid team2Id,
            // double team1Rating,
            // double team2Rating,
            // TeamSize TeamSize,
            // bool team1Won = true)
            // {
            //     try
            //     {
            //         // Get team confidence levels
            //         var team1Confidence = await CalculateConfidenceAsync(team1Id, TeamSize);
            //         var team2Confidence = await CalculateConfidenceAsync(team2Id, TeamSize);

            //         // Get variety bonuses
            //         var team1VarietyBonus = await CalculateVarietyBonusAsync(team1Id, team1Rating, TeamSize);
            //         var team2VarietyBonus = await CalculateVarietyBonusAsync(team2Id, team2Rating, TeamSize);

            //         // Apply variety bonus threshold based on confidence (not games played)
            //         // Variety bonus is only applied when players have reached 1.0 confidence
            //         if (team1Confidence < 1.0)
            //             team1VarietyBonus = 0.0;
            //         if (team2Confidence < 1.0)
            //             team2VarietyBonus = 0.0;

            //         // Calculate confidence multipliers for each player (1.0 to 2.0 based on confidence)
            //         var team1ConfidenceMultiplier = 2.0 - team1Confidence;
            //         var team2ConfidenceMultiplier = 2.0 - team2Confidence;

            //         // Calculate final multipliers - use different logic for winners vs losers
            //         // Winners: positive variety = more gain (higher multiplier)
            //         // Losers: positive variety = less loss (lower multiplier)
            //         var team1Multiplier = team1Won
            //             ? team1ConfidenceMultiplier + team1VarietyBonus  // Winner: add variety bonus
            //             : team1ConfidenceMultiplier - team1VarietyBonus; // Loser: subtract variety bonus

            //         var team2Multiplier = !team1Won
            //             ? team2ConfidenceMultiplier + team2VarietyBonus  // Winner: add variety bonus
            //             : team2ConfidenceMultiplier - team2VarietyBonus; // Loser: subtract variety bonus

            //         // Clamp multipliers to maximum value only (matching Python implementation)
            //         team1Multiplier = Math.Min(team1Multiplier, GetScrimmageDbConfig().MaxMultiplier);
            //         team2Multiplier = Math.Min(team2Multiplier, GetScrimmageDbConfig().MaxMultiplier);

            //         return (team1Multiplier, team2Multiplier);
            //     }
            //     catch (Exception ex)
            //     {
            //         await ErrorHandler.CaptureAsync(ex, "Failed to calculate rating multipliers", nameof(CalculateRatingMultipliersAsync));
            //         throw;
            //     }
            // }

            /// <summary>
            /// Calculates the rating change for a match using the ELO formula with individual multipliers.
            /// </summary>
            // public static async Task<(double Team1Change, double Team2Change)> CalculateRatingChangeAsync(
            // Guid team1Id,
            // Guid team2Id,
            // double team1Rating,
            // double team2Rating,
            // TeamSize TeamSize,
            // int team1Score = 0,
            // int team2Score = 0)
            // {
            //     try
            //     {
            //         // Determine who won based on scores
            //         bool team1Won = team1Score > team2Score;
            //         bool team2Won = team2Score > team1Score;

            //         // If no scores provided, assume team1 won (default behavior)
            //         if (team1Score == 0 && team2Score == 0)
            //         {
            //             team1Won = true;
            //             team2Won = false;
            //         }

            //         // Calculate expected outcome using ELO formula
            //         double expectedOutcome = 1.0 / (1.0 + Math.Pow(10.0, (team2Rating - team1Rating) / GetScrimmageDbConfig().EloDivisor));

            //         // Calculate base rating change (K * (actual - expected))
            //         // For team1, actual outcome is 1 if they win, 0 if they lose
            //         double actualOutcome = team1Won ? 1.0 : 0.0;
            //         double baseChange = GetScrimmageDbConfig().BaseRatingChange * (actualOutcome - expectedOutcome);

            //         // Get individual multipliers for each team
            //         var (team1Multiplier, team2Multiplier) = await CalculateRatingMultipliersAsync(team1Id, team2Id, team1Rating, team2Rating, TeamSize, team1Won);

            //         // Calculate rating gap scaling to prevent shadow-boxing
            //         // Get current rating range to calculate max gap (40% / 2 = 20% of total range)
            //         var ratingRange = await GetCurrentRatingRangeAsync();
            //         double maxGap = (ratingRange.Max - ratingRange.Min) * 0.4 / 2.0;
            //         double ratingGap = Math.Abs(team1Rating - team2Rating);

            //         // Initialize gap scaling (no effect by default)
            //         double gapScaling = 1.0;

            //         if (ratingGap <= maxGap)
            //         {
            //             // normalized_gap is rating_gap normalized to 1.0 (0.0 to 1.0)
            //             double normalizedGap = ratingGap / maxGap;
            //             // Use cosine scaling: (1 + cos(π * normalized_gap * 0.7)) / 2
            //             double cosineScaling = (1.0 + Math.Cos(Math.PI * normalizedGap * 0.7)) / 2.0;

            //             // Get confidence levels to determine if gap scaling should be applied
            //             var team1Confidence = await CalculateConfidenceAsync(team1Id, TeamSize);
            //             var team2Confidence = await CalculateConfidenceAsync(team2Id, TeamSize);

            //             // Only apply gap scaling to higher-rated player if lower-rated player has 1.0 confidence
            //             if (team1Rating > team2Rating && team2Confidence >= 1.0)
            //             {
            //                 // Team1 is higher rated, apply scaling to their change
            //                 gapScaling = cosineScaling;
            //             }
            //             else if (team2Rating > team1Rating && team1Confidence >= 1.0)
            //             {
            //                 // Team2 is higher rated, apply scaling to their change
            //                 gapScaling = cosineScaling;
            //             }
            //         }

            //         // Apply catch-up bonus if enabled (additive after other multipliers)
            //         double team1CatchUpBonus = 0.0;
            //         double team2CatchUpBonus = 0.0;

            //         if (GetScrimmageDbConfig().CatchUpEnabled)
            //         {
            //             // Apply catch-up bonus to winner if below target
            //             if (team1Won && team1Rating < GetScrimmageDbConfig().CatchUpTargetRating)
            //             {
            //                 team1CatchUpBonus = CalculateCatchUpBonus(team1Rating, GetScrimmageDbConfig().CatchUpTargetRating, GetScrimmageDbConfig().CatchUpThreshold, GetScrimmageDbConfig().CatchUpMaxBonus);
            //             }
            //             else if (team2Won && team2Rating < GetScrimmageDbConfig().CatchUpTargetRating)
            //             {
            //                 team2CatchUpBonus = CalculateCatchUpBonus(team2Rating, GetScrimmageDbConfig().CatchUpTargetRating, GetScrimmageDbConfig().CatchUpThreshold, GetScrimmageDbConfig().CatchUpMaxBonus);
            //             }
            //         }

            //         // Calculate final rating changes with additive catchup bonus
            //         double team1Change = baseChange * (team1Multiplier + team1CatchUpBonus);
            //         double team2Change = -baseChange * (team2Multiplier + team2CatchUpBonus);

            //         // Apply gap scaling correctly: only apply to higher-rated team when they WIN
            //         // If team1 is higher rated and wins, apply gap scaling to team1
            //         // If team2 is higher rated and wins, apply gap scaling to team2
            //         if (team1Rating > team2Rating && team1Won)
            //         {
            //             // Team1 is higher rated and wins, apply gap scaling to their change
            //             team1Change *= gapScaling;
            //         }
            //         else if (team2Rating > team1Rating && team2Won)
            //         {
            //             // Team2 is higher rated and wins, apply gap scaling to their change
            //             team2Change *= gapScaling;
            //         }

            //         return (team1Change, team2Change);
            //     }
            //     catch (Exception ex)
            //     {
            //         await ErrorHandler.CaptureAsync(ex, "Failed to calculate rating change", nameof(CalculateRatingChangeAsync));
            //         throw;
            //     }
            // }

            /// <summary>
            /// Calculates catch-up bonus for a player below the target rating.
            /// Matches Python implementation: exponential decay based on distance from target.
            /// </summary>
            /// <param name="playerRating">Current player rating</param>
            /// <param name="targetRating">Target rating to converge toward</param>
            /// <param name="threshold">Rating difference threshold before bonus applies</param>
            /// <param name="maxBonus">Maximum bonus multiplier</param>
            /// <returns>Catch-up bonus multiplier (0.0 to maxBonus)</returns>
            private static double CalculateCatchUpBonus(
                double playerRating,
                double targetRating,
                double threshold,
                double maxBonus
            )
            {
                if (playerRating >= targetRating)
                    return 0.0;

                var distance = targetRating - playerRating;
                if (distance <= threshold)
                    return 0.0;

                // Use exponential decay for the bonus (matching Python)
                var scale = threshold / 2.0;
                var progress = 1.0 - Math.Exp(-distance / scale);
                return progress * maxBonus;
            }

            /// <summary>
            /// Calculates the confidence level for a team based on games played.
            /// </summary>
            public static async Task<double> CalculateConfidenceAsync(Guid teamId, TeamSize TeamSize)
            {
                try
                {
                    // Get team's match history
                    // Fetch recent matches directly from database
                    var since = DateTime.UtcNow.AddDays(-GetScrimmageDbConfig().VarietyWindowDays);
                    var matchesResult = await CoreService.Matches.GetAllAsync(DatabaseComponent.Repository);
                    var totalGames =
                        matchesResult
                            .Data?.Where(m => (m.Team1Id == teamId || m.Team2Id == teamId) && m.CompletedAt >= since)
                            .Count() ?? 0;

                    // Calculate confidence based on number of games played
                    // Uses exponential decay formula: confidence = max_confidence * (1 - e^(-k * games_played / max_confidence_games))
                    // where k = 3.0 gives a good balance of quick early growth and smooth leveling
                    if (totalGames >= GetScrimmageDbConfig().VarietyBonusGamesThreshold)
                        return 1.0; // Max confidence at threshold games and stays there

                    // Calculate how far along we are in the confidence growth (0 to 1)
                    double progress = totalGames / (double)GetScrimmageDbConfig().VarietyBonusGamesThreshold;

                    // Use exponential decay formula: 1 - e^(-k * x)
                    // k = 3.0 gives a good balance of quick early growth and smooth leveling
                    const double k = 3.0;
                    double confidence = 1.0 * (1.0 - Math.Exp(-k * progress));

                    return confidence;
                }
                catch (Exception ex)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        ex,
                        "Failed to calculate confidence",
                        nameof(CalculateConfidenceAsync)
                    );
                    throw;
                }
            }
        }
    }
}
