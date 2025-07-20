using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Scrimmages.Data;
using WabbitBot.Core.Scrimmages.ScrimmageRating.Interface;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    public class ProvenPotentialService
    {
        private readonly ICoreEventBus _eventBus;
        private readonly ICoreErrorHandler _errorHandler;
        private readonly IProvenPotentialRepository _provenPotentialRepo;
        private readonly RatingCalculatorService _ratingCalculator;
        private const double PROVEN_POTENTIAL_GAP_THRESHOLD = 0.1;
        private const int MAX_MATCHES_FOR_PROVEN_POTENTIAL = 16; // Match Python: max_matches_for_proven_potential
        private const double MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL = 1.0; // Match Python: only consider matches where player had low confidence (< 1.0)

        public ProvenPotentialService(
            ICoreEventBus eventBus,
            ICoreErrorHandler errorHandler,
            IProvenPotentialRepository provenPotentialRepo,
            RatingCalculatorService ratingCalculator)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _provenPotentialRepo = provenPotentialRepo ?? throw new ArgumentNullException(nameof(provenPotentialRepo));
            _ratingCalculator = ratingCalculator ?? throw new ArgumentNullException(nameof(ratingCalculator));
        }

        /// <summary>
        /// Initializes the service and subscribes to relevant events.
        /// </summary>
        public Task InitializeAsync()
        {
            // Subscribe to match completion event to check for proven potential
            _eventBus.Subscribe<ScrimmageCompletedEvent>(async evt =>
            {
                try
                {
                    // Use event bus to get team ratings
                    var team1RatingResp = await _eventBus.RequestAsync<GetTeamRatingRequest, GetTeamRatingResponse>(new GetTeamRatingRequest { TeamId = evt.Team1Id });
                    var team2RatingResp = await _eventBus.RequestAsync<GetTeamRatingRequest, GetTeamRatingResponse>(new GetTeamRatingRequest { TeamId = evt.Team2Id });
                    var team1Rating = team1RatingResp?.Rating ?? 0;
                    var team2Rating = team2RatingResp?.Rating ?? 0;

                    // Use confidence values from the event (calculated at match time)
                    var team1Confidence = evt.Team1Confidence;
                    var team2Confidence = evt.Team2Confidence;

                    // Calculate rating change using RatingCalculatorService
                    var (team1Change, team2Change) = await _ratingCalculator.CalculateRatingChangeAsync(
                        evt.Team1Id, evt.Team2Id, team1Rating, team2Rating, evt.GameSize, evt.Team1Score, evt.Team2Score);

                    // Request creation of proven potential record
                    var createRequest = new CreateProvenPotentialRecordRequest
                    {
                        MatchId = evt.MatchId,
                        ChallengerId = evt.Team1Id,
                        OpponentId = evt.Team2Id,
                        ChallengerRating = team1Rating,
                        OpponentRating = team2Rating,
                        ChallengerConfidence = team1Confidence,
                        OpponentConfidence = team2Confidence,
                        ChallengerOriginalRatingChange = team1Change,
                        OpponentOriginalRatingChange = team2Change,
                        GameSize = evt.GameSize
                    };

                    await _eventBus.PublishAsync(createRequest);
                }
                catch (Exception ex)
                {
                    await _errorHandler.HandleError(ex);
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles a request to check proven potential for a team.
        /// </summary>
        public async Task<CheckProvenPotentialResponse> HandleCheckProvenPotentialRequest(CheckProvenPotentialRequest request)
        {
            var response = new CheckProvenPotentialResponse();

            // Get active records for the team
            var activeRecords = await _provenPotentialRepo.GetActiveRecordsForTeamAsync(request.TeamId);

            // Sort by creation date (most recent first) and take only the most recent matches
            // This matches Python behavior: only look at the most recent 16 matches for proven potential
            var recentRecords = activeRecords
                .OrderByDescending(r => r.CreatedAt)
                .Take(MAX_MATCHES_FOR_PROVEN_POTENTIAL)
                .ToList();

            foreach (var record in recentRecords)
            {
                // Skip if already complete
                if (record.IsComplete)
                    continue;

                // Skip if last checked recently (e.g. within last hour)
                if (record.LastCheckedAt.HasValue &&
                    DateTime.UtcNow - record.LastCheckedAt.Value < TimeSpan.FromHours(1))
                    continue;

                // Determine which team is the challenger and which is the opponent
                var isChallengerPlayer = record.ChallengerId == request.TeamId;
                var challengerRatingAtTime = isChallengerPlayer ? record.ChallengerRating : record.OpponentRating;
                var opponentRatingAtTime = isChallengerPlayer ? record.OpponentRating : record.ChallengerRating;
                var challengerConfidenceAtTime = isChallengerPlayer ? record.ChallengerConfidence : record.OpponentConfidence;
                var opponentConfidenceAtTime = isChallengerPlayer ? record.OpponentConfidence : record.ChallengerConfidence;
                var challengerOriginalChange = isChallengerPlayer ? record.ChallengerOriginalRatingChange : record.OpponentOriginalRatingChange;
                var opponentOriginalChange = isChallengerPlayer ? record.OpponentOriginalRatingChange : record.ChallengerOriginalRatingChange;

                // Calculate original and current gaps
                var originalGap = Math.Abs(opponentRatingAtTime - challengerRatingAtTime);
                var currentGap = Math.Abs(opponentRatingAtTime - request.CurrentRating);

                // Calculate gap closure percentage
                var gapClosurePercent = (originalGap - currentGap) / originalGap;

                // Only consider matches where the challenger had low confidence (< 1.0)
                if (challengerConfidenceAtTime >= MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL)
                    continue;

                // Only consider matches where the opponent had high confidence (established player)
                if (opponentConfidenceAtTime < 1.0)
                    continue;

                // Calculate all unapplied thresholds that have been reached
                // Find the highest threshold that has been reached but not yet applied
                var maxApplied = record.AppliedThresholds.Count > 0 ? record.AppliedThresholds.Max() : 0.0;

                // Calculate all thresholds that should be applied
                // Start from the next threshold after maxApplied and go up to the gap closure
                var thresholdsToApply = new List<double>();
                var currentThreshold = (Math.Floor(maxApplied / PROVEN_POTENTIAL_GAP_THRESHOLD) + 1) * PROVEN_POTENTIAL_GAP_THRESHOLD;

                // Only apply thresholds that haven't been applied yet and are within the gap closure
                while (currentThreshold <= gapClosurePercent)
                {
                    if (!record.AppliedThresholds.Contains(currentThreshold))
                    {
                        thresholdsToApply.Add(currentThreshold);
                    }
                    currentThreshold += PROVEN_POTENTIAL_GAP_THRESHOLD;
                }

                // If no thresholds to apply, skip
                if (thresholdsToApply.Count == 0)
                    continue;

                // Calculate incremental compensation from new thresholds only
                // The compensation should be the highest new threshold reached
                var totalCompensationPercentage = thresholdsToApply.Max() - maxApplied;

                // Apply compensation to the original rating changes
                // Both players get compensation proportional to their original changes
                // Keep decimal precision to avoid rating drift
                var challengerAdjustment = challengerOriginalChange * totalCompensationPercentage;
                var opponentAdjustment = opponentOriginalChange * totalCompensationPercentage;

                // Mark all thresholds as applied
                foreach (var threshold in thresholdsToApply)
                {
                    record.AppliedThresholds.Add(threshold);
                }

                // Apply adjustments to both teams (matching Python implementation)
                // Both teams get compensation proportional to their original changes
                var challengerRatingAdjustment = (int)challengerAdjustment;
                var opponentRatingAdjustment = (int)opponentAdjustment;

                // Add challenger adjustment to response
                response.Adjustments.Add(new RatingAdjustment
                {
                    ChallengerId = record.ChallengerId,
                    OpponentId = record.OpponentId,
                    Adjustment = challengerRatingAdjustment
                });

                // Add opponent adjustment to response
                response.Adjustments.Add(new RatingAdjustment
                {
                    ChallengerId = record.OpponentId,
                    OpponentId = record.ChallengerId,
                    Adjustment = opponentRatingAdjustment
                });

                // Publish event to apply the challenger rating adjustment
                await _eventBus.PublishAsync(new ApplyProvenPotentialAdjustmentEvent
                {
                    ChallengerId = record.ChallengerId,
                    OpponentId = record.OpponentId,
                    Adjustment = challengerRatingAdjustment,
                    GameSize = record.GameSize,
                    Reason = $"Proven potential adjustment: {totalCompensationPercentage:P0} gap closure threshold"
                });

                // Publish event to apply the opponent rating adjustment
                await _eventBus.PublishAsync(new ApplyProvenPotentialAdjustmentEvent
                {
                    ChallengerId = record.OpponentId,
                    OpponentId = record.ChallengerId,
                    Adjustment = opponentRatingAdjustment,
                    GameSize = record.GameSize,
                    Reason = $"Proven potential adjustment: {totalCompensationPercentage:P0} gap closure threshold"
                });

                // Update record - thresholds already added above
                record.RatingAdjustment = record.RatingAdjustment + challengerAdjustment;
                record.LastCheckedAt = DateTime.UtcNow;
                // Note: Python version doesn't have a hard completion limit, it continues checking as long as there are matches
                // record.IsComplete = record.AppliedThresholds.Count >= MAX_MATCHES_FOR_PROVEN_POTENTIAL;

                await _provenPotentialRepo.UpdateAsync(record);
            }

            response.HasAdjustments = response.Adjustments.Count > 0;
            return response;
        }

        /// <summary>
        /// Handles a request to create a proven potential record.
        /// </summary>
        public async Task<CreateProvenPotentialRecordResponse> HandleCreateProvenPotentialRecordRequest(CreateProvenPotentialRecordRequest request)
        {
            // Create records for both teams if either has low confidence
            // This matches Python behavior where both players are checked for proven potential
            var records = new List<ProvenPotentialRecord>();

            // Create record for challenger if they had low confidence
            if (request.ChallengerConfidence < MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL)
            {
                var challengerRecord = new ProvenPotentialRecord
                {
                    OriginalMatchId = request.MatchId,
                    ChallengerId = request.ChallengerId,
                    OpponentId = request.OpponentId,
                    ChallengerRating = request.ChallengerRating,
                    OpponentRating = request.OpponentRating,
                    ChallengerConfidence = request.ChallengerConfidence,
                    OpponentConfidence = request.OpponentConfidence,
                    AppliedThresholds = new HashSet<double>(),
                    ChallengerOriginalRatingChange = request.ChallengerOriginalRatingChange,
                    OpponentOriginalRatingChange = request.OpponentOriginalRatingChange,
                    RatingAdjustment = 0.0, // Will be calculated during proven potential checks
                    GameSize = request.GameSize,
                    LastCheckedAt = DateTime.UtcNow,
                    IsComplete = false
                };
                records.Add(challengerRecord);
            }

            // Create record for opponent if they had low confidence
            if (request.OpponentConfidence < MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL)
            {
                var opponentRecord = new ProvenPotentialRecord
                {
                    OriginalMatchId = request.MatchId,
                    ChallengerId = request.OpponentId, // Opponent becomes the challenger for their record
                    OpponentId = request.ChallengerId, // Original challenger becomes the opponent
                    ChallengerRating = request.OpponentRating,
                    OpponentRating = request.ChallengerRating,
                    ChallengerConfidence = request.OpponentConfidence,
                    OpponentConfidence = request.ChallengerConfidence,
                    AppliedThresholds = new HashSet<double>(),
                    ChallengerOriginalRatingChange = request.OpponentOriginalRatingChange,
                    OpponentOriginalRatingChange = request.ChallengerOriginalRatingChange,
                    RatingAdjustment = 0.0, // Will be calculated during proven potential checks
                    GameSize = request.GameSize,
                    LastCheckedAt = DateTime.UtcNow,
                    IsComplete = false
                };
                records.Add(opponentRecord);
            }

            // Add all records to the repository
            foreach (var provenPotentialRecord in records)
            {
                await _provenPotentialRepo.AddAsync(provenPotentialRecord);
            }

            return new CreateProvenPotentialRecordResponse
            {
                Created = records.Count > 0
            };
        }

        /// <summary>
        /// Runs proven potential checks for all teams with active records. Call this on demand.
        /// </summary>
        public async Task RunProvenPotentialChecksAsync()
        {
            try
            {
                // Get all teams with active proven potential records
                var activeRecords = await _provenPotentialRepo.QueryAsync("IsComplete = 0");
                var teamsWithRecords = activeRecords.SelectMany(r => new[] { r.ChallengerId, r.OpponentId }).Distinct();

                foreach (var teamId in teamsWithRecords)
                {
                    // Get current team rating via event bus
                    var ratingResp = await _eventBus.RequestAsync<GetTeamRatingRequest, GetTeamRatingResponse>(new GetTeamRatingRequest { TeamId = teamId });
                    var currentRating = ratingResp?.Rating ?? 0;

                    // Check proven potential for this team
                    var checkRequest = new CheckProvenPotentialRequest
                    {
                        TeamId = teamId,
                        CurrentRating = currentRating
                    };

                    await HandleCheckProvenPotentialRequest(checkRequest);
                }
            }
            catch (Exception ex)
            {
                await _errorHandler.HandleError(ex);
            }
        }
    }
}