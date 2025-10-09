// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using WabbitBot.Common.Data.Interfaces;
// using WabbitBot.Common.Data.Service;
// using WabbitBot.Common.Models;
// using WabbitBot.Core.Common.Models.Common;
// using WabbitBot.Core.Common.Models.Scrimmage;
// using WabbitBot.Core.Common.Services;

// namespace WabbitBot.Core.Scrimmages
// {
//     public partial class ScrimmageCore
//     {
//         private const double PROVEN_POTENTIAL_GAP_THRESHOLD = 0.1;
//         private const int MAX_MATCHES_FOR_PROVEN_POTENTIAL = 16; // Match Python: max_matches_for_proven_potential
//         private const double MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL = 1.0; // Match Python: only consider matches where player had low confidence (< 1.0)

//         public class RatingAdjustment
//         {
//             public Guid ChallengerId { get; set; }
//             public Guid OpponentId { get; set; }
//             public int Adjustment { get; set; }
//         }

//         public class CheckProvenPotentialRequest
//         {
//             public Guid TeamId { get; set; }
//             public double CurrentRating { get; set; }
//         }

//         public class CheckProvenPotentialResponse
//         {
//             public bool HasAdjustments { get; set; }
//             public List<RatingAdjustment> Adjustments { get; set; } = new();
//         }

//         public async Task<CheckProvenPotentialResponse> HandleCheckProvenPotentialRequest(
//             Guid teamId, double currentRating)
//         {
//             var response = new CheckProvenPotentialResponse();

//             var allRecordsResult = await CoreService.ProvenPotentialRecords.GetAllAsync(DatabaseComponent.Repository);
//             var activeRecords = allRecordsResult.Data!
//                 .Where(r => (r.ChallengerId == teamId || r.OpponentId == teamId) && !r.IsComplete)
//                 .OrderByDescending(r => r.CreatedAt)
//                 .Take(MAX_MATCHES_FOR_PROVEN_POTENTIAL)
//                 .ToList();

//             foreach (var record in activeRecords)
//             {
//                 var isChallenger = record.ChallengerId == teamId;
//                 var challengerRatingAtTime = isChallenger ? record.ChallengerRating : record.OpponentRating;
//                 var opponentRatingAtTime = isChallenger ? record.OpponentRating : record.ChallengerRating;
//                 var challengerConfidenceAtTime = isChallenger ? record.ChallengerConfidence : record.OpponentConfidence;
//                 var opponentConfidenceAtTime = isChallenger ? record.OpponentConfidence : record.ChallengerConfidence;
//                 var challengerOriginalChange = isChallenger ? record.ChallengerOriginalRatingChange : record.OpponentOriginalRatingChange;
//                 var opponentOriginalChange = isChallenger ? record.OpponentOriginalRatingChange : record.ChallengerOriginalRatingChange;

//                 var originalGap = Math.Abs(opponentRatingAtTime - challengerRatingAtTime);
//                 if (originalGap == 0) continue;
//                 var currentGap = Math.Abs(opponentRatingAtTime - currentRating);
//                 var gapClosurePercent = (originalGap - currentGap) / originalGap;

//                 if (challengerConfidenceAtTime >= MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL) continue;
//                 if (opponentConfidenceAtTime < MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL) continue;

//                 var maxApplied = record.AppliedThresholds.Count > 0 ? record.AppliedThresholds.Max() : 0.0;
//                 var thresholdsToApply = new List<double>();
//                 var currentThreshold = (Math.Floor(
//                     maxApplied / PROVEN_POTENTIAL_GAP_THRESHOLD) + 1) * PROVEN_POTENTIAL_GAP_THRESHOLD;
//                 while (currentThreshold <= gapClosurePercent)
//                 {
//                     if (!record.AppliedThresholds.Contains(currentThreshold))
//                     {
//                         thresholdsToApply.Add(currentThreshold);
//                     }
//                     currentThreshold += PROVEN_POTENTIAL_GAP_THRESHOLD;
//                 }

//                 if (thresholdsToApply.Count == 0) continue;

//                 var totalCompensationPercentage = thresholdsToApply.Max() - maxApplied;
//                 var challengerAdjustment = challengerOriginalChange * totalCompensationPercentage;
//                 var opponentAdjustment = opponentOriginalChange * totalCompensationPercentage;

//                 foreach (var threshold in thresholdsToApply)
//                 {
//                     record.AppliedThresholds.Add(threshold);
//                 }

//                 response.Adjustments.Add(new RatingAdjustment
//                 {
//                     ChallengerId = record.ChallengerId,
//                     OpponentId = record.OpponentId,
//                     Adjustment = (int)challengerAdjustment,
//                 });

//                 response.Adjustments.Add(new RatingAdjustment
//                 {
//                     ChallengerId = record.OpponentId,
//                     OpponentId = record.ChallengerId,
//                     Adjustment = (int)opponentAdjustment,
//                 });

//                 record.RatingAdjustment = record.RatingAdjustment + challengerAdjustment;
//                 record.LastCheckedAt = DateTime.UtcNow;
//                 await CoreService.ProvenPotentialRecords.UpdateAsync(record, DatabaseComponent.Repository);
//             }

//             response.HasAdjustments = response.Adjustments.Count > 0;
//             return response;
//         }

//         /// <summary>
//         /// Handles a request to create a proven potential record.
//         /// </summary>
//         public async Task<Result> HandleCreateProvenPotentialRecordRequest(
//             CreateProvenPotentialRecordRequest request)
//         {
//             // Fetch match data from database using MatchId (events now carry only IDs)
//             var matchResult = await CoreService.Matches.GetByIdAsync(request.MatchId, DatabaseComponent.Repository);
//             var match = matchResult.Data;
//             if (match == null)
//             {
//                 return new CreateProvenPotentialRecordResponse(request.MatchId, false);
//             }

//             // Determine context
//             var team1Id = match.Team1Id;
//             var team2Id = match.Team2Id;
//             var teamSize = match.TeamSize;

//             // If this match comes from a scrimmage, prefer scrimmage-stored ratings and rating changes
//             double? scrimTeam1Rating = null;
//             double? scrimTeam2Rating = null;
//             double? scrimTeam1Change = null;
//             double? scrimTeam2Change = null;
//             if (match.ParentType == MatchParentType.Scrimmage && match.ParentId.HasValue)
//             {
//                 var scrimResult = await CoreService.Scrimmages.GetByIdAsync(match.ParentId.Value, DatabaseComponent.Repository);
//                 var scrim = scrimResult.Data;
//                 if (scrim != null)
//                 {
//                     scrimTeam1Rating = scrim.Team1Rating;
//                     scrimTeam2Rating = scrim.Team2Rating;
//                     scrimTeam1Change = scrim.Team1RatingChange;
//                     scrimTeam2Change = scrim.Team2RatingChange;
//                 }
//             }

//             // Look up current team ratings as fallback
//             var team1 = (await CoreService.Teams.GetByIdAsync(team1Id, DatabaseComponent.Repository)).Data;
//             var team2 = (await CoreService.Teams.GetByIdAsync(team2Id, DatabaseComponent.Repository)).Data;
//             if (team1 == null || team2 == null)
//             {
//                 return new CreateProvenPotentialRecordResponse(request.MatchId, false);
//             }

//             double challengerRating = scrimTeam1Rating ?? (team1.ScrimmageTeamStats.ContainsKey(teamSize) ? team1.ScrimmageTeamStats[teamSize].CurrentRating : 0.0);
//             double opponentRating = scrimTeam2Rating ?? (team2.ScrimmageTeamStats.ContainsKey(teamSize) ? team2.ScrimmageTeamStats[teamSize].CurrentRating : 0.0);

//             // Confidence from recent matches window
//             var team1Confidence = await RatingCalculator.CalculateConfidenceAsync(team1Id, teamSize);
//             var team2Confidence = await RatingCalculator.CalculateConfidenceAsync(team2Id, teamSize);

//             var records = new List<ProvenPotentialRecord>();

//             if (team1Confidence < MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL)
//             {
//                 records.Add(new ProvenPotentialRecord
//                 {
//                     OriginalMatchId = request.MatchId,
//                     ChallengerId = team1Id,
//                     OpponentId = team2Id,
//                     ChallengerRating = challengerRating,
//                     OpponentRating = opponentRating,
//                     ChallengerConfidence = team1Confidence,
//                     OpponentConfidence = team2Confidence,
//                     AppliedThresholds = new HashSet<double>(),
//                     ChallengerOriginalRatingChange = scrimTeam1Change ?? 0.0,
//                     OpponentOriginalRatingChange = scrimTeam2Change ?? 0.0,
//                     RatingAdjustment = 0.0,
//                     TeamSize = teamSize,
//                     LastCheckedAt = DateTime.UtcNow,
//                     IsComplete = false,
//                 });
//             }

//             if (team2Confidence < MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL)
//             {
//                 records.Add(new ProvenPotentialRecord
//                 {
//                     OriginalMatchId = request.MatchId,
//                     ChallengerId = team2Id,
//                     OpponentId = team1Id,
//                     ChallengerRating = opponentRating,
//                     OpponentRating = challengerRating,
//                     ChallengerConfidence = team2Confidence,
//                     OpponentConfidence = team1Confidence,
//                     AppliedThresholds = new HashSet<double>(),
//                     ChallengerOriginalRatingChange = scrimTeam2Change ?? 0.0,
//                     OpponentOriginalRatingChange = scrimTeam1Change ?? 0.0,
//                     RatingAdjustment = 0.0,
//                     TeamSize = teamSize,
//                     LastCheckedAt = DateTime.UtcNow,
//                     IsComplete = false,
//                 });
//             }

//             foreach (var provenPotentialRecord in records)
//             {
//                 await CoreService.ProvenPotentialRecords.CreateAsync(provenPotentialRecord, DatabaseComponent.Repository);
//             }

//             return new CreateProvenPotentialRecordResponse(request.MatchId, records.Count > 0);
//         }

//         /// <summary>
//         /// Runs proven potential checks for all teams with active records. Call this on demand.
//         /// </summary>
//         public async Task RunProvenPotentialChecksAsync()
//         {
//             try
//             {
//                 // Get all teams with active proven potential records
//                 var activeRecords = await CoreService.ProvenPotentialRecords.GetAllAsync(DatabaseComponent.Repository);
//                 var teamsWithRecords = activeRecords.Data?.SelectMany(
//                     r => new[] { r.ChallengerId, r.OpponentId }).Distinct();

//                 foreach (var teamId in teamsWithRecords ?? Enumerable.Empty<Guid>())
//                 {
//                     // Get current team rating directly from database
//                     var teamResult = await CoreService.Teams.GetByIdAsync(teamId, DatabaseComponent.Repository);
//                     var team = teamResult.Data;
//                     var currentRating = 0.0;
//                     if (team != null)
//                     {
//                         // Use best available TeamSize rating (TwoVTwo as default if present)
//                         if (team.ScrimmageTeamStats.TryGetValue(TeamSize.TwoVTwo, out var stats))
//                         {
//                             currentRating = stats.CurrentRating;
//                         }
//                         else if (team.ScrimmageTeamStats.Any())
//                         {
//                             currentRating = team.ScrimmageTeamStats.First().Value.CurrentRating;
//                         }
//                     }

//                     // Check proven potential for this team
//                     await HandleCheckProvenPotentialRequest(teamId, currentRating);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 await CoreService.ErrorHandler.CaptureAsync(
//                     ex, "Failed to run proven potential checks", nameof(RunProvenPotentialChecksAsync));
//             }
//         }
//     }
// }
