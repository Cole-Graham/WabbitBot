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
        /// <summary>
        /// Proven Potential system for retroactive rating adjustments.
        /// Tracks matches between new (<1.0 confidence) and established (1.0 confidence) teams.
        /// </summary>
        public static class ProvenPotential
        {
            /// <summary>
            /// Check if a match is eligible for proven potential tracking.
            /// </summary>
            public static bool IsEligibleForTracking(
                double challengerConfidence,
                double opponentConfidence,
                int challengerGamesPlayed,
                int opponentGamesPlayed
            )
            {
                // One team must be new (< 1.0 confidence) and one established (>= 1.0 confidence)
                bool challengerIsNew = challengerConfidence < RatingCalculator.Config.MAX_CONFIDENCE;
                bool opponentIsNew = opponentConfidence < RatingCalculator.Config.MAX_CONFIDENCE;

                // Only track if exactly one player is new
                return (challengerIsNew && !opponentIsNew) || (!challengerIsNew && opponentIsNew);
            }

            /// <summary>
            /// Create a proven potential record for an eligible match.
            /// </summary>
            public static async Task<Result<ProvenPotentialRecord>> CreateProvenPotentialRecordAsync(
                Guid matchId,
                Guid challengerTeamId,
                Guid opponentTeamId,
                double challengerRating,
                double opponentRating,
                double challengerConfidence,
                double opponentConfidence,
                double? challengerRatingChange,
                double? opponentRatingChange,
                TeamSize teamSize
            )
            {
                try
                {
                    // Determine which team is new and which is established
                    bool challengerIsNew = challengerConfidence < RatingCalculator.Config.MAX_CONFIDENCE;
                    Guid newTeamId = challengerIsNew ? challengerTeamId : opponentTeamId;
                    Guid establishedTeamId = challengerIsNew ? opponentTeamId : challengerTeamId;

                    // Get new player's current match count for tracking window
                    var newTeamResult = await CoreService.Teams.GetByIdAsync(newTeamId, DatabaseComponent.Repository);
                    int newPlayerMatchCount = 0;
                    if (newTeamResult.Success && newTeamResult.Data is not null)
                    {
                        var stats = newTeamResult.Data.ScrimmageTeamStats.GetValueOrDefault(teamSize);
                        if (stats is not null)
                        {
                            newPlayerMatchCount = stats.Wins + stats.Losses + stats.Draws;
                        }
                    }

                    if (challengerRatingChange is null)
                    {
                        return Result<ProvenPotentialRecord>.Failure("Challenger rating change is required");
                    }
                    if (opponentRatingChange is null)
                    {
                        return Result<ProvenPotentialRecord>.Failure("Opponent rating change is required");
                    }

                    var record = new ProvenPotentialRecord
                    {
                        Id = Guid.NewGuid(),
                        CreatedAt = DateTime.UtcNow,
                        OriginalMatchId = matchId,
                        ChallengerId = challengerTeamId,
                        OpponentId = opponentTeamId,
                        NewPlayerId = newTeamId,
                        EstablishedPlayerId = establishedTeamId,
                        TriggerMatchId = matchId,
                        ChallengerRating = challengerRating,
                        OpponentRating = opponentRating,
                        ChallengerConfidence = challengerConfidence,
                        OpponentConfidence = opponentConfidence,
                        ChallengerOriginalRatingChange = challengerRatingChange.Value,
                        OpponentOriginalRatingChange = opponentRatingChange.Value,
                        RatingAdjustment = 0.0,
                        NewPlayerMatchCountAtCreation = newPlayerMatchCount,
                        TeamSize = teamSize,
                        AppliedThresholds = new HashSet<double>(),
                        LastCheckedAt = DateTime.UtcNow,
                    };

                    var createResult = await CoreService.ProvenPotentialRecords.CreateAsync(
                        record,
                        DatabaseComponent.Repository
                    );

                    if (!createResult.Success)
                    {
                        return Result<ProvenPotentialRecord>.Failure(
                            $"Failed to create proven potential record: {createResult.ErrorMessage}"
                        );
                    }

                    return Result<ProvenPotentialRecord>.CreateSuccess(record);
                }
                catch (Exception ex)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        ex,
                        "Failed to create proven potential record",
                        nameof(CreateProvenPotentialRecordAsync)
                    );
                    return Result<ProvenPotentialRecord>.Failure($"Error creating PP record: {ex.Message}");
                }
            }

            /// <summary>
            /// Check and apply proven potential adjustments for a team that just completed a match.
            /// </summary>
            public static async Task<Result> CheckAndApplyProvenPotentialAsync(
                Guid teamId,
                TeamSize teamSize,
                double currentRating,
                double currentConfidence,
                Guid currentMatchId
            )
            {
                try
                {
                    // Only check if team hasn't reached full confidence yet
                    if (currentConfidence < RatingCalculator.Config.MAX_CONFIDENCE)
                    {
                        return Result.CreateSuccess();
                    }

                    // Get all incomplete PP records for this team
                    var allRecordsResult = await CoreService.ProvenPotentialRecords.GetAllAsync(
                        DatabaseComponent.Repository
                    );
                    if (!allRecordsResult.Success || allRecordsResult.Data is null)
                    {
                        return Result.CreateSuccess(); // No records to process
                    }

                    var incompleteRecords = allRecordsResult
                        .Data.Where(r => !r.IsComplete && r.NewPlayerId == teamId && r.TeamSize == teamSize)
                        .ToList();

                    if (!incompleteRecords.Any())
                    {
                        return Result.CreateSuccess();
                    }

                    // Batch process all incomplete records
                    var batchId = Guid.NewGuid();
                    var adjustmentsApplied = new List<(Guid EstablishedTeamId, double Adjustment)>();

                    foreach (var record in incompleteRecords)
                    {
                        // Calculate thresholds that should be applied
                        double initialNewRating = record.ChallengerRating;
                        if (record.NewPlayerId == record.OpponentId)
                        {
                            initialNewRating = record.OpponentRating;
                        }

                        double ratingGap = Math.Abs(currentRating - initialNewRating);
                        int thresholdsCrossed = (int)(
                            ratingGap / (RatingCalculator.Config.PROVEN_POTENTIAL_GAP_THRESHOLD * 1000)
                        );

                        // Calculate adjustment for each threshold crossed
                        double totalAdjustmentThisCheck = 0.0;
                        for (int i = 0; i < thresholdsCrossed; i++)
                        {
                            double threshold = (i + 1) * RatingCalculator.Config.PROVEN_POTENTIAL_GAP_THRESHOLD;
                            if (!record.AppliedThresholds.Contains(threshold))
                            {
                                // Calculate partial rating restoration
                                double closureFraction = threshold;
                                double establishedOriginalChange =
                                    record.EstablishedPlayerId == record.ChallengerId
                                        ? record.ChallengerOriginalRatingChange
                                        : record.OpponentOriginalRatingChange;

                                double adjustment = Math.Abs(establishedOriginalChange) * closureFraction * 0.5;

                                // Track adjustment (positive because we're giving rating back)
                                adjustmentsApplied.Add((record.EstablishedPlayerId, adjustment));
                                totalAdjustmentThisCheck += adjustment;

                                record.AppliedThresholds.Add(threshold);
                            }
                        }

                        // Update tracking fields
                        record.CrossedThresholds = record.AppliedThresholds.Count;
                        record.RatingAdjustment += totalAdjustmentThisCheck;
                        record.ClosureFraction = ratingGap / Math.Max(1.0, Math.Abs(initialNewRating - currentRating));
                        record.LastCheckedAt = DateTime.UtcNow;

                        // Check if tracking window has closed (new player played 16 matches since PP record creation)
                        var newTeamStatsResult = await CoreService.Teams.GetByIdAsync(
                            record.NewPlayerId,
                            DatabaseComponent.Repository
                        );
                        if (newTeamStatsResult.Success && newTeamStatsResult.Data is not null)
                        {
                            var stats = newTeamStatsResult.Data.ScrimmageTeamStats.GetValueOrDefault(teamSize);
                            if (stats is not null)
                            {
                                int currentMatchCount = stats.Wins + stats.Losses + stats.Draws;
                                int matchesSinceCreation = currentMatchCount - record.NewPlayerMatchCountAtCreation;

                                // Set TrackingEndMatchId when the tracking window closes
                                if (
                                    matchesSinceCreation >= RatingCalculator.Config.PROVEN_POTENTIAL_TRACKING_MATCHES
                                    && record.TrackingEndMatchId == Guid.Empty
                                )
                                {
                                    record.TrackingEndMatchId = currentMatchId;
                                }

                                // Complete and apply PP if tracking window closed AND new player reached 1.0 confidence
                                if (
                                    record.TrackingEndMatchId != Guid.Empty
                                    && currentConfidence >= RatingCalculator.Config.MAX_CONFIDENCE
                                )
                                {
                                    record.IsComplete = true;
                                    record.AppliedAtMatchId = currentMatchId;
                                    record.BatchId = batchId;
                                }
                            }
                        }

                        // Update the record
                        await CoreService.ProvenPotentialRecords.UpdateAsync(record, DatabaseComponent.Repository);
                    }

                    // Apply batched adjustments to established teams
                    var groupedAdjustments = adjustmentsApplied
                        .GroupBy(a => a.EstablishedTeamId)
                        .Select(g => new { TeamId = g.Key, TotalAdjustment = g.Sum(x => x.Adjustment) });

                    foreach (var adjustment in groupedAdjustments)
                    {
                        var teamResult = await CoreService.Teams.GetByIdAsync(
                            adjustment.TeamId,
                            DatabaseComponent.Repository
                        );
                        if (teamResult.Success && teamResult.Data is not null)
                        {
                            var stats = teamResult.Data.ScrimmageTeamStats.GetValueOrDefault(teamSize);
                            if (stats is not null)
                            {
                                double newRating = stats.CurrentRating + adjustment.TotalAdjustment;
                                var teamCore = new TeamCore();
                                await teamCore.UpdateScrimmageRating(adjustment.TeamId, teamSize, newRating);
                            }
                        }
                    }

                    return Result.CreateSuccess();
                }
                catch (Exception ex)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        ex,
                        "Failed to check and apply proven potential",
                        nameof(CheckAndApplyProvenPotentialAsync)
                    );
                    return Result.Failure($"Error applying proven potential: {ex.Message}");
                }
            }
        }
    }
}
