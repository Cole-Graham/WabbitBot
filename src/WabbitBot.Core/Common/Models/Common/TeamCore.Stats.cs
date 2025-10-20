using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class TeamCore
    {
        public class ScrimmageStats
        {
            // Configuration constants matching Python simulator
            private const double MAX_VARIETY_BONUS = 0.2;
            private const double MIN_VARIETY_BONUS = -0.1;
            private const double MIN_SCALING_FACTOR = 0.5;
            private const double MAX_GAP_PERCENT = 0.2; // 20% of rating range

            public static double GetWinRate(ScrimmageTeamStats stats)
            {
                var totalMatches = stats.Wins + stats.Losses;
                return totalMatches == 0 ? 0 : (double)stats.Wins / totalMatches;
            }

            public static int GetTotalMatches(ScrimmageTeamStats stats)
            {
                return stats.Wins + stats.Losses;
            }

            public static double GetVarietyScore(TeamVarietyStats varietyStats)
            {
                if (varietyStats == null)
                    return 0.0;

                var entropyScore = varietyStats.VarietyEntropy;
                var bonusScore = varietyStats.VarietyBonus;

                return (entropyScore * 0.7) + (bonusScore * 0.3);
            }

            public static double GetEffectiveRating(ScrimmageTeamStats stats, TeamVarietyStats varietyStats)
            {
                var baseRating = stats.CurrentRating;
                var varietyScore = GetVarietyScore(varietyStats);
                var varietyBonus = varietyScore * 0.1 * baseRating;
                return baseRating + varietyBonus;
            }

            /// <summary>
            /// Calculate variety bonus with opponent availability normalization.
            /// Implements the sophisticated algorithm from the Python rating simulator.
            /// </summary>
            public static async Task<Result> CalculateAndUpdateVarietyStatsAsync(
                Guid teamId,
                TeamSize teamSize,
                double teamRating,
                int teamGamesPlayed
            )
            {
                try
                {
                    // Fetch the team with its variety stats and encounters
                    var teamResult = await CoreService.Teams.GetByIdAsync(teamId, DatabaseComponent.Repository);
                    if (!teamResult.Success || teamResult.Data is null)
                    {
                        return Result.Failure("Failed to fetch team");
                    }

                    var team = teamResult.Data;

                    // Get or create variety stats for this team size
                    var varietyStats = team.VarietyStats.FirstOrDefault(v => v.TeamSize == teamSize);
                    if (varietyStats is null)
                    {
                        varietyStats = new TeamVarietyStats { TeamId = teamId, TeamSize = teamSize };
                        team.VarietyStats.Add(varietyStats);
                    }

                    // Get active season for filtering encounters to the current season
                    var seasonsResult = await CoreService.Seasons.GetAllAsync(DatabaseComponent.Repository);
                    var activeSeason = seasonsResult.Data?.FirstOrDefault(s => s.IsActive);
                    DateTime seasonStartDate = activeSeason?.StartDate ?? DateTime.UtcNow.AddYears(-1);

                    // Get opponent encounters for this team size within the active season
                    var encountersResult = await CoreService.TeamOpponentEncounters.GetAllAsync(
                        DatabaseComponent.Repository
                    );
                    if (!encountersResult.Success || encountersResult.Data is null)
                    {
                        return Result.Failure("Failed to fetch opponent encounters");
                    }

                    var recentEncounters = encountersResult
                        .Data.Where(e =>
                            e.TeamId == teamId && e.TeamSize == teamSize && e.EncounteredAt >= seasonStartDate
                        )
                        .OrderByDescending(e => e.EncounteredAt)
                        .ToList();

                    if (!recentEncounters.Any())
                    {
                        // No encounters yet, set default values
                        varietyStats.VarietyEntropy = 0;
                        varietyStats.VarietyBonus = MAX_VARIETY_BONUS;
                        varietyStats.LastCalculated = DateTime.UtcNow;
                        varietyStats.LastUpdated = DateTime.UtcNow;
                        await CoreService.TeamVarietyStats.UpdateAsync(varietyStats, DatabaseComponent.Repository);
                        return Result.CreateSuccess();
                    }

                    // Calculate basic entropy (Shannon entropy)
                    var opponentCounts = recentEncounters
                        .GroupBy(e => e.OpponentId)
                        .ToDictionary(g => g.Key, g => g.Count());
                    var totalEncounters = recentEncounters.Count;
                    var uniqueOpponents = opponentCounts.Count;

                    double playerVarietyEntropy = 0.0;
                    foreach (var count in opponentCounts.Values)
                    {
                        if (count > 0)
                        {
                            double p = (double)count / totalEncounters;
                            playerVarietyEntropy -= p * Math.Log2(p);
                        }
                    }

                    varietyStats.VarietyEntropy = playerVarietyEntropy;

                    // Get all teams to calculate average entropy and median games
                    var allTeamsResult = await CoreService.Teams.GetAllAsync(DatabaseComponent.Repository);
                    if (!allTeamsResult.Success || allTeamsResult.Data is null)
                    {
                        return Result.Failure("Failed to fetch all teams");
                    }

                    var allTeams = allTeamsResult.Data.ToList();

                    // Calculate average variety entropy across all teams (season-filtered)
                    double avgVarietyEntropy = await CalculateAverageVarietyEntropyAsync(
                        allTeams,
                        teamSize,
                        seasonStartDate
                    );

                    // Calculate median games played
                    var gamesPlayedList = allTeams
                        .Where(t => t.ScrimmageTeamStats.ContainsKey(teamSize))
                        .Select(t => t.ScrimmageTeamStats[teamSize].Wins + t.ScrimmageTeamStats[teamSize].Losses)
                        .OrderBy(g => g)
                        .ToList();

                    double medianGamesPlayed =
                        gamesPlayedList.Count > 0 ? gamesPlayedList[gamesPlayedList.Count / 2] : 1.0;

                    // Calculate entropy difference from average
                    double entropyDifference = playerVarietyEntropy - avgVarietyEntropy;
                    double relativeDiff =
                        avgVarietyEntropy != 0 ? entropyDifference / Math.Abs(avgVarietyEntropy) : entropyDifference;

                    // Calculate games played scaling factor (quadratic growth)
                    double gamesPlayedRatio = Math.Min(teamGamesPlayed / medianGamesPlayed, 1.0);
                    double scalingFactor =
                        MIN_SCALING_FACTOR + ((1.0 - MIN_SCALING_FACTOR) * gamesPlayedRatio * gamesPlayedRatio);

                    double scaledDiff = relativeDiff * scalingFactor;
                    double baseBonus = scaledDiff * MAX_VARIETY_BONUS;

                    // Calculate opponent availability normalization
                    var teamRatings = allTeams
                        .Where(t => t.ScrimmageTeamStats.ContainsKey(teamSize))
                        .Select(t => new { TeamId = t.Id, Rating = t.ScrimmageTeamStats[teamSize].CurrentRating })
                        .ToList();

                    if (teamRatings.Any())
                    {
                        double ratingRange = teamRatings.Max(t => t.Rating) - teamRatings.Min(t => t.Rating);
                        double neighborRange = ratingRange * MAX_GAP_PERCENT;

                        // Count potential opponents within neighbor range
                        int playerNeighbors = teamRatings.Count(t =>
                            t.TeamId != teamId && Math.Abs(t.Rating - teamRating) <= neighborRange
                        );

                        // Find maximum neighbors any team has
                        int maxNeighbors = 1;
                        foreach (var t in teamRatings)
                        {
                            int neighbors = teamRatings.Count(other =>
                                other.TeamId != t.TeamId && Math.Abs(other.Rating - t.Rating) <= neighborRange
                            );
                            maxNeighbors = Math.Max(maxNeighbors, neighbors);
                        }

                        // Calculate availability factor
                        double availabilityFactor = 1.0;
                        if (maxNeighbors > 0)
                        {
                            double neighborRatio = (double)playerNeighbors / maxNeighbors;
                            availabilityFactor = 1.0 + (1.0 - neighborRatio) * 0.5; // 1.0 to 1.5 range
                        }

                        baseBonus *= availabilityFactor;

                        // Store calculation context
                        varietyStats.RatingRangeAtCalc = ratingRange;
                        varietyStats.NeighborRangeAtCalc = neighborRange;
                        varietyStats.PlayerNeighborsAtCalc = playerNeighbors;
                        varietyStats.MaxNeighborsObservedAtCalc = maxNeighbors;
                        varietyStats.AvailabilityFactorUsed = availabilityFactor;
                    }

                    // Clamp final bonus
                    varietyStats.VarietyBonus = Math.Clamp(baseBonus, MIN_VARIETY_BONUS, MAX_VARIETY_BONUS);

                    // Store calculation metadata
                    varietyStats.AverageVarietyEntropyAtCalc = avgVarietyEntropy;
                    varietyStats.MedianGamesAtCalc = medianGamesPlayed;
                    varietyStats.TotalOpponents = totalEncounters;
                    varietyStats.UniqueOpponents = uniqueOpponents;
                    varietyStats.LastCalculated = DateTime.UtcNow;
                    varietyStats.LastUpdated = DateTime.UtcNow;

                    // Update in database
                    var updateResult = await CoreService.TeamVarietyStats.UpdateAsync(
                        varietyStats,
                        DatabaseComponent.Repository
                    );

                    return updateResult.Success
                        ? Result.CreateSuccess()
                        : Result.Failure($"Failed to update variety stats: {updateResult.ErrorMessage}");
                }
                catch (Exception ex)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        ex,
                        "Failed to calculate variety stats",
                        nameof(CalculateAndUpdateVarietyStatsAsync)
                    );
                    return Result.Failure($"Error calculating variety stats: {ex.Message}");
                }
            }

            /// <summary>
            /// Calculate average variety entropy across all teams within the active season.
            /// </summary>
            private static async Task<double> CalculateAverageVarietyEntropyAsync(
                List<Team> allTeams,
                TeamSize teamSize,
                DateTime seasonStartDate
            )
            {
                var encountersResult = await CoreService.TeamOpponentEncounters.GetAllAsync(
                    DatabaseComponent.Repository
                );
                if (!encountersResult.Success || encountersResult.Data is null)
                {
                    return 2.0; // Default fallback
                }

                // Filter encounters to the active season
                var allEncounters = encountersResult
                    .Data.Where(e => e.TeamSize == teamSize && e.EncounteredAt >= seasonStartDate)
                    .ToList();

                var teamEntropies = new List<double>();

                foreach (var team in allTeams.Where(t => t.ScrimmageTeamStats.ContainsKey(teamSize)))
                {
                    var teamEncounters = allEncounters.Where(e => e.TeamId == team.Id).ToList();
                    if (!teamEncounters.Any())
                        continue;

                    var opponentCounts = teamEncounters
                        .GroupBy(e => e.OpponentId)
                        .ToDictionary(g => g.Key, g => g.Count());
                    var totalEncounters = teamEncounters.Count;

                    double entropy = 0.0;
                    foreach (var count in opponentCounts.Values)
                    {
                        if (count > 0)
                        {
                            double p = (double)count / totalEncounters;
                            entropy -= p * Math.Log2(p);
                        }
                    }

                    teamEntropies.Add(entropy);
                }

                return teamEntropies.Any() ? teamEntropies.Average() : 2.0;
            }
        }
    }
}
