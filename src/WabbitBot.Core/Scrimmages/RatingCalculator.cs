using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
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
            #region Configuration Constants
            /// <summary>
            /// Configuration constants for the rating system.
            /// </summary>
            public static class Config
            {
                // Base rating configuration
                public const double STARTING_RATING = 1000.0;
                public const double MINIMUM_RATING = 600.0;
                public const double BASE_K_FACTOR = 40.0;
                public const double ELO_DIVISOR = 400.0;

                // Confidence system
                public const int CONFIDENCE_GAMES = 20;
                public const double MAX_CONFIDENCE = 1.0;
                public const double CONFIDENCE_GROWTH_RATE = 3.0;

                // Variety bonus
                public const double MAX_VARIETY_BONUS = 0.2;
                public const double MIN_VARIETY_BONUS = -0.1;
                public const double MIN_SCALING_FACTOR = 0.5;

                // Gap scaling
                public const double MAX_GAP_PERCENT = 0.2;

                // Multipliers
                public const double MAX_MULTIPLIER = 2.0;

                // Catch-up bonus (helps players converge to target rating from below)
                public const double CATCH_UP_TARGET_RATING = 1500.0;
                public const double CATCH_UP_THRESHOLD = 200.0;
                public const double CATCH_UP_MAX_BONUS = 1.0;

                // Proven potential
                public const int PROVEN_POTENTIAL_TRACKING_MATCHES = 16;
                public const double PROVEN_POTENTIAL_GAP_THRESHOLD = 0.1;
            }
            #endregion

            #region Elo Calculation
            /// <summary>
            /// Core Elo rating calculations.
            /// </summary>
            public static class EloCalculation
            {
                /// <summary>
                /// Calculates the expected win probability for a player based on rating difference.
                /// </summary>
                public static double CalculateExpectedScore(double playerRating, double opponentRating)
                {
                    return 1.0 / (1.0 + Math.Pow(10, (opponentRating - playerRating) / Config.ELO_DIVISOR));
                }

                /// <summary>
                /// Calculates the base rating change before any multipliers.
                /// </summary>
                public static double CalculateBaseChange(double expectedScore, bool isWinner)
                {
                    double actualScore = isWinner ? 1.0 : 0.0;
                    return Config.BASE_K_FACTOR * (actualScore - expectedScore);
                }
            }
            #endregion

            #region Confidence System
            /// <summary>
            /// Confidence system that scales rating volatility based on games played.
            /// </summary>
            public static class ConfidenceSystem
            {
                /// <summary>
                /// Calculate confidence based on games played.
                /// Uses exponential growth that levels out at max_confidence_games.
                /// </summary>
                public static double CalculateConfidence(int gamesPlayed)
                {
                    if (gamesPlayed >= Config.CONFIDENCE_GAMES)
                    {
                        return Config.MAX_CONFIDENCE;
                    }

                    double progress = (double)gamesPlayed / Config.CONFIDENCE_GAMES;
                    return Config.MAX_CONFIDENCE * (1.0 - Math.Exp(-Config.CONFIDENCE_GROWTH_RATE * progress));
                }

                /// <summary>
                /// Calculate the confidence multiplier for rating changes (2.0 to 1.0).
                /// </summary>
                public static double CalculateConfidenceMultiplier(double confidence)
                {
                    return 2.0 - confidence;
                }
            }
            #endregion

            #region Gap Scaling
            /// <summary>
            /// Gap scaling to prevent "shadow-boxing" with low-rated opponents.
            /// </summary>
            public static class GapScaling
            {
                /// <summary>
                /// Calculate gap scaling factor using cosine function.
                /// </summary>
                public static double CalculateGapScaling(
                    double higherRating,
                    double lowerRating,
                    double ratingRange,
                    double lowerConfidence
                )
                {
                    // Only apply gap scaling if lower-rated player has 1.0 confidence
                    if (lowerConfidence < Config.MAX_CONFIDENCE)
                    {
                        return 1.0;
                    }

                    double maxGap = ratingRange * Config.MAX_GAP_PERCENT;
                    if (maxGap <= 0)
                    {
                        return 1.0;
                    }

                    double gap = higherRating - lowerRating;
                    if (gap <= 0 || gap > maxGap)
                    {
                        return gap > maxGap ? 0.0 : 1.0;
                    }

                    double normalizedGap = gap / maxGap;
                    return (1.0 + Math.Cos(Math.PI * normalizedGap * 0.7)) / 2.0;
                }
            }
            #endregion

            #region Catch-Up Bonus
            /// <summary>
            /// Catch-up bonus to help players below target rating converge toward it.
            /// Uses exponential decay to give stronger bonuses further from target.
            /// </summary>
            public static class CatchUpBonus
            {
                /// <summary>
                /// Calculate catch-up bonus for a player below target rating.
                /// Returns 0 for players at or above target.
                /// </summary>
                public static double CalculateCatchUpBonus(double playerRating)
                {
                    if (playerRating >= Config.CATCH_UP_TARGET_RATING)
                    {
                        return 0.0;
                    }

                    double distance = Config.CATCH_UP_TARGET_RATING - playerRating;
                    if (distance <= Config.CATCH_UP_THRESHOLD)
                    {
                        return 0.0;
                    }

                    // Use exponential decay for the bonus (matching Python implementation)
                    double scale = Config.CATCH_UP_THRESHOLD / 2.0;
                    double progress = 1.0 - Math.Exp(-distance / scale);
                    return progress * Config.CATCH_UP_MAX_BONUS;
                }
            }
            #endregion

            #region Variety Bonus
            /// <summary>
            /// Variety bonus calculations based on opponent diversity.
            /// </summary>
            public static class VarietyBonus
            {
                /// <summary>
                /// Calculate variety bonus from pre-calculated TeamVarietyStats.
                /// </summary>
                public static double GetVarietyBonus(TeamVarietyStats? varietyStats, double confidence)
                {
                    // Variety bonus only applies to teams with 1.0 confidence
                    if (confidence < Config.MAX_CONFIDENCE)
                    {
                        return 0.0;
                    }

                    if (varietyStats is null)
                    {
                        return Config.MAX_VARIETY_BONUS;
                    }

                    // Use the pre-calculated variety bonus
                    return Math.Clamp(varietyStats.VarietyBonus, Config.MIN_VARIETY_BONUS, Config.MAX_VARIETY_BONUS);
                }
            }
            #endregion

            #region Rating Change Calculation
            /// <summary>
            /// Complete rating change calculation result.
            /// </summary>
            public class RatingChangeResult
            {
                public double WinnerChange { get; set; }
                public double LoserChange { get; set; }
                public double WinnerMultiplier { get; set; }
                public double LoserMultiplier { get; set; }
                public double WinnerVarietyBonus { get; set; }
                public double LoserVarietyBonus { get; set; }
                public double WinnerCatchUpBonus { get; set; }
                public double LoserCatchUpBonus { get; set; }
                public double GapScalingApplied { get; set; }
                public Guid HigherRatedTeamId { get; set; }
            }

            /// <summary>
            /// Calculate complete rating changes for a match.
            /// </summary>
            public static RatingChangeResult CalculateRatingChange(
                Guid winnerTeamId,
                double winnerRating,
                double winnerConfidence,
                TeamVarietyStats? winnerVarietyStats,
                Guid loserTeamId,
                double loserRating,
                double loserConfidence,
                TeamVarietyStats? loserVarietyStats,
                double ratingRange
            )
            {
                // Calculate expected score
                double expectedScore = EloCalculation.CalculateExpectedScore(winnerRating, loserRating);

                // Calculate base changes
                double baseChange = Config.BASE_K_FACTOR * (1.0 - expectedScore);

                // Calculate confidence multipliers
                double winnerConfidenceMultiplier = ConfidenceSystem.CalculateConfidenceMultiplier(winnerConfidence);
                double loserConfidenceMultiplier = ConfidenceSystem.CalculateConfidenceMultiplier(loserConfidence);

                // Get variety bonuses
                double winnerVarietyBonus = VarietyBonus.GetVarietyBonus(winnerVarietyStats, winnerConfidence);
                double loserVarietyBonus = VarietyBonus.GetVarietyBonus(loserVarietyStats, loserConfidence);

                // Calculate gap scaling
                bool winnerIsHigher = winnerRating > loserRating;
                double gapScaling;
                Guid higherRatedTeamId = winnerIsHigher ? winnerTeamId : loserTeamId;

                if (winnerIsHigher)
                {
                    gapScaling = GapScaling.CalculateGapScaling(
                        winnerRating,
                        loserRating,
                        ratingRange,
                        loserConfidence
                    );
                }
                else
                {
                    gapScaling = GapScaling.CalculateGapScaling(
                        loserRating,
                        winnerRating,
                        ratingRange,
                        winnerConfidence
                    );
                }

                // Calculate final multipliers (before catch-up bonus)
                // Winners: positive variety = more gain (higher multiplier)
                // Losers: positive variety = less loss (lower multiplier)
                double winnerMultiplier = Math.Min(
                    Config.MAX_MULTIPLIER,
                    winnerConfidenceMultiplier + winnerVarietyBonus
                );
                double loserMultiplier = Math.Min(Config.MAX_MULTIPLIER, loserConfidenceMultiplier - loserVarietyBonus);

                // Calculate catch-up bonuses (additive, applied after other multipliers)
                double winnerCatchUpBonus = CatchUpBonus.CalculateCatchUpBonus(winnerRating);
                double loserCatchUpBonus = 0.0; // Only apply to winners by default

                // Calculate final rating changes with catch-up bonus
                double winnerChange;
                double loserChange;

                if (winnerIsHigher)
                {
                    // Winner is higher rated, apply gap scaling to their change
                    winnerChange = baseChange * (winnerMultiplier + winnerCatchUpBonus) * gapScaling;
                    loserChange = -baseChange * (loserMultiplier + loserCatchUpBonus);
                }
                else
                {
                    // Loser is higher rated, apply gap scaling to their change
                    winnerChange = baseChange * (winnerMultiplier + winnerCatchUpBonus);
                    loserChange = -baseChange * (loserMultiplier + loserCatchUpBonus) * gapScaling;
                }

                return new RatingChangeResult
                {
                    WinnerChange = winnerChange,
                    LoserChange = loserChange,
                    WinnerMultiplier = winnerMultiplier,
                    LoserMultiplier = loserMultiplier,
                    WinnerVarietyBonus = winnerVarietyBonus,
                    LoserVarietyBonus = loserVarietyBonus,
                    WinnerCatchUpBonus = winnerCatchUpBonus,
                    LoserCatchUpBonus = loserCatchUpBonus,
                    GapScalingApplied = gapScaling,
                    HigherRatedTeamId = higherRatedTeamId,
                };
            }
            #endregion

            #region Main Calculation Method
            /// <summary>
            /// Calculate and apply rating changes for a completed match.
            /// </summary>
            public static async Task<Result<RatingChangeResult>> CalculateAndApplyRatingChangesAsync(
                Guid matchId,
                Guid scrimmageId,
                Guid winnerTeamId,
                Guid loserTeamId,
                TeamSize teamSize
            )
            {
                try
                {
                    // Fetch teams and their stats
                    var winnerTeamResult = await CoreService.Teams.GetByIdAsync(
                        winnerTeamId,
                        DatabaseComponent.Repository
                    );
                    var loserTeamResult = await CoreService.Teams.GetByIdAsync(
                        loserTeamId,
                        DatabaseComponent.Repository
                    );

                    if (!winnerTeamResult.Success || winnerTeamResult.Data is null)
                    {
                        return Result<RatingChangeResult>.Failure("Failed to fetch winner team");
                    }
                    if (!loserTeamResult.Success || loserTeamResult.Data is null)
                    {
                        return Result<RatingChangeResult>.Failure("Failed to fetch loser team");
                    }

                    var winnerTeam = winnerTeamResult.Data;
                    var loserTeam = loserTeamResult.Data;

                    // Get or create scrimmage stats
                    if (!winnerTeam.ScrimmageTeamStats.TryGetValue(teamSize, out var winnerStats))
                    {
                        winnerStats = new ScrimmageTeamStats
                        {
                            TeamId = winnerTeamId,
                            TeamSize = teamSize,
                            CurrentRating = Config.STARTING_RATING,
                            InitialRating = Config.STARTING_RATING,
                            HighestRating = Config.STARTING_RATING,
                        };
                        winnerTeam.ScrimmageTeamStats[teamSize] = winnerStats;
                    }

                    if (!loserTeam.ScrimmageTeamStats.TryGetValue(teamSize, out var loserStats))
                    {
                        loserStats = new ScrimmageTeamStats
                        {
                            TeamId = loserTeamId,
                            TeamSize = teamSize,
                            CurrentRating = Config.STARTING_RATING,
                            InitialRating = Config.STARTING_RATING,
                            HighestRating = Config.STARTING_RATING,
                        };
                        loserTeam.ScrimmageTeamStats[teamSize] = loserStats;
                    }

                    // Calculate confidence
                    int winnerGamesPlayed = winnerStats.Wins + winnerStats.Losses;
                    int loserGamesPlayed = loserStats.Wins + loserStats.Losses;
                    double winnerConfidence = ConfidenceSystem.CalculateConfidence(winnerGamesPlayed);
                    double loserConfidence = ConfidenceSystem.CalculateConfidence(loserGamesPlayed);

                    // Get variety stats
                    TeamVarietyStats? winnerVarietyStats = winnerTeam.VarietyStats.FirstOrDefault(v =>
                        v.TeamSize == teamSize
                    );
                    TeamVarietyStats? loserVarietyStats = loserTeam.VarietyStats.FirstOrDefault(v =>
                        v.TeamSize == teamSize
                    );

                    // Calculate rating range (for gap scaling)
                    var allTeamsResult = await CoreService.Teams.GetAllAsync(DatabaseComponent.Repository);
                    double ratingRange = 0.0;
                    if (allTeamsResult.Success && allTeamsResult.Data is not null)
                    {
                        var ratings = allTeamsResult
                            .Data.Where(t => t.ScrimmageTeamStats.ContainsKey(teamSize))
                            .Select(t => t.ScrimmageTeamStats[teamSize].CurrentRating)
                            .ToList();

                        if (ratings.Count > 0)
                        {
                            ratingRange = ratings.Max() - ratings.Min();
                        }
                    }

                    // Calculate rating changes
                    var result = CalculateRatingChange(
                        winnerTeamId,
                        winnerStats.CurrentRating,
                        winnerConfidence,
                        winnerVarietyStats,
                        loserTeamId,
                        loserStats.CurrentRating,
                        loserConfidence,
                        loserVarietyStats,
                        ratingRange
                    );

                    // Update stats and ratings
                    winnerStats.CurrentRating = Math.Max(
                        Config.MINIMUM_RATING,
                        winnerStats.CurrentRating + result.WinnerChange
                    );
                    loserStats.CurrentRating = Math.Max(
                        Config.MINIMUM_RATING,
                        loserStats.CurrentRating + result.LoserChange
                    );
                    winnerStats.HighestRating = Math.Max(winnerStats.HighestRating, winnerStats.CurrentRating);
                    loserStats.HighestRating = Math.Max(loserStats.HighestRating, loserStats.CurrentRating);
                    winnerStats.RecentRatingChange = result.WinnerChange;
                    loserStats.RecentRatingChange = result.LoserChange;
                    winnerStats.Confidence = winnerConfidence;
                    loserStats.Confidence = loserConfidence;

                    // Update win/loss records via TeamCore
                    var teamCore = new TeamCore();
                    var winnerUpdateResult = await teamCore.UpdateScrimmageStats(winnerTeamId, teamSize, true);
                    var loserUpdateResult = await teamCore.UpdateScrimmageStats(loserTeamId, teamSize, false);

                    if (!winnerUpdateResult.Success || !loserUpdateResult.Success)
                    {
                        return Result<RatingChangeResult>.Failure("Failed to update team stats");
                    }

                    // Update ratings via TeamCore
                    var winnerRatingUpdate = await teamCore.UpdateScrimmageRating(
                        winnerTeamId,
                        teamSize,
                        winnerStats.CurrentRating
                    );
                    var loserRatingUpdate = await teamCore.UpdateScrimmageRating(
                        loserTeamId,
                        teamSize,
                        loserStats.CurrentRating
                    );

                    if (!winnerRatingUpdate.Success || !loserRatingUpdate.Success)
                    {
                        return Result<RatingChangeResult>.Failure("Failed to update team ratings");
                    }

                    return Result<RatingChangeResult>.CreateSuccess(result);
                }
                catch (Exception ex)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        ex,
                        "Failed to calculate and apply rating changes",
                        nameof(CalculateAndApplyRatingChangesAsync)
                    );
                    return Result<RatingChangeResult>.Failure($"Error calculating rating changes: {ex.Message}");
                }
            }
            #endregion
        }
    }
}
