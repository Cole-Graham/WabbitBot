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
        private const int MAX_MATCHES_FOR_PROVEN_POTENTIAL = 10;
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
                        evt.Team1Id, evt.Team2Id, team1Rating, team2Rating, evt.GameSize);

                    // For proven potential, we need a single rating change value
                    // Use the average of both changes as a reasonable approximation
                    var ratingChange = (int)((team1Change + team2Change) / 2.0);

                    // Request creation of proven potential record
                    var createRequest = new CreateProvenPotentialRecordRequest
                    {
                        MatchId = evt.MatchId,
                        Team1Id = evt.Team1Id,
                        Team2Id = evt.Team2Id,
                        Team1Rating = team1Rating,
                        Team2Rating = team2Rating,
                        Team1Confidence = team1Confidence,
                        Team2Confidence = team2Confidence,
                        RatingChange = (int)ratingChange
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

            foreach (var record in activeRecords)
            {
                // Skip if already complete
                if (record.IsComplete)
                    continue;

                // Skip if last checked recently (e.g. within last hour)
                if (record.LastCheckedAt.HasValue &&
                    DateTime.UtcNow - record.LastCheckedAt.Value < TimeSpan.FromHours(1))
                    continue;

                // Calculate original and current gaps
                var originalGap = Math.Abs(record.Team1Rating - record.Team2Rating);
                var currentGap = Math.Abs(request.CurrentRating - (record.Team1Id == request.TeamId ? record.Team2Rating : record.Team1Rating));

                // Calculate gap closure percentage
                var gapClosurePercent = (originalGap - currentGap) / originalGap;

                // Calculate next threshold to check
                var maxApplied = record.AppliedThresholds.Any() ? record.AppliedThresholds.Max() : 0.0;
                var nextThreshold = (Math.Floor(maxApplied / PROVEN_POTENTIAL_GAP_THRESHOLD) + 1) * PROVEN_POTENTIAL_GAP_THRESHOLD;

                // Skip if threshold not reached or already applied
                if (gapClosurePercent < nextThreshold || record.AppliedThresholds.Contains(nextThreshold))
                    continue;

                // Calculate new rating change
                var expectedScore = 1.0 / (1.0 + Math.Pow(10, (request.CurrentRating - record.Team2Rating) / 400.0));
                var baseChange = 16.0 * (1 - expectedScore);
                var confidenceMultiplier = 2.0 - record.Team1Confidence; // Use new formula: 2.0 - confidence
                var newRatingChange = (int)(baseChange * confidenceMultiplier);

                // Calculate adjustment
                var originalChange = record.RatingAdjustment;
                var ratingAdjustment = newRatingChange - originalChange;

                // Add adjustment to response
                response.Adjustments.Add(new RatingAdjustment
                {
                    Team1Id = record.Team1Id,
                    Team2Id = record.Team2Id,
                    Adjustment = ratingAdjustment
                });

                // Publish event to apply the rating adjustment
                await _eventBus.PublishAsync(new ApplyProvenPotentialAdjustmentEvent
                {
                    Team1Id = record.Team1Id,
                    Team2Id = record.Team2Id,
                    Adjustment = ratingAdjustment,
                    GameSize = GameSize.TwoVTwo, // TODO: Get actual game size from record
                    Reason = $"Proven potential adjustment: {nextThreshold:P0} gap closure threshold"
                });

                // Update record
                record.AppliedThresholds.Add(nextThreshold);
                record.RatingAdjustment = newRatingChange;
                record.LastCheckedAt = DateTime.UtcNow;
                record.IsComplete = record.AppliedThresholds.Count >= MAX_MATCHES_FOR_PROVEN_POTENTIAL;

                await _provenPotentialRepo.UpdateAsync(record);
            }

            response.HasAdjustments = response.Adjustments.Any();
            return response;
        }

        /// <summary>
        /// Handles a request to create a proven potential record.
        /// </summary>
        public async Task<CreateProvenPotentialRecordResponse> HandleCreateProvenPotentialRecordRequest(CreateProvenPotentialRecordRequest request)
        {
            // Only create record if at least one team has low confidence (< 1.0)
            if (request.Team1Confidence >= MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL &&
                request.Team2Confidence >= MAX_CONFIDENCE_FOR_PROVEN_POTENTIAL)
            {
                return new CreateProvenPotentialRecordResponse
                {
                    Created = false,
                    Reason = "Both teams have high confidence"
                };
            }

            var record = new ProvenPotentialRecord
            {
                OriginalMatchId = request.MatchId,
                Team1Id = request.Team1Id,
                Team2Id = request.Team2Id,
                Team1Rating = request.Team1Rating,
                Team2Rating = request.Team2Rating,
                Team1Confidence = request.Team1Confidence,
                Team2Confidence = request.Team2Confidence,
                AppliedThresholds = new HashSet<double>(),
                RatingAdjustment = request.RatingChange,
                LastCheckedAt = DateTime.UtcNow,
                IsComplete = false
            };

            await _provenPotentialRepo.AddAsync(record);

            return new CreateProvenPotentialRecordResponse
            {
                Created = true
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
                var teamsWithRecords = activeRecords.SelectMany(r => new[] { r.Team1Id, r.Team2Id }).Distinct();

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