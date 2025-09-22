using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Scrimmages.ScrimmageRating.Interface;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    /// <summary>
    /// Handler for proven potential related events and requests.
    /// </summary>
    [GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
    public partial class ProvenPotentialHandler : CoreHandler
    {
        private readonly IProvenPotentialRepository _provenPotentialRepo;
        private readonly ProvenPotentialService _provenPotentialService;

        public static ProvenPotentialHandler Instance { get; } = new ProvenPotentialHandler(
            WabbitBot.Core.Common.Data.DataServiceManager.ProvenPotentialRepository,
            new ProvenPotentialService());

        private ProvenPotentialHandler(
            IProvenPotentialRepository provenPotentialRepo,
            ProvenPotentialService provenPotentialService)
            : base(CoreEventBus.Instance)
        {
            _provenPotentialRepo = provenPotentialRepo ?? throw new ArgumentNullException(nameof(provenPotentialRepo));
            _provenPotentialService = provenPotentialService ?? throw new ArgumentNullException(nameof(provenPotentialService));
        }

        public override Task InitializeAsync()
        {
            // Register auto-generated event subscriptions
            RegisterEventSubscriptions();
            return Task.CompletedTask;
        }

        [EventHandler(Priority = 1, IsRequestResponse = true, EnableRetry = true, MaxRetryAttempts = 3)]
        private async Task<CheckProvenPotentialResponse> HandleCheckProvenPotentialRequest(CheckProvenPotentialRequest request)
        {
            var response = await _provenPotentialService.HandleCheckProvenPotentialRequest(request);
            await EventBus.PublishAsync(response);
            return response;
        }

        [EventHandler(Priority = 2, IsRequestResponse = true, EnableRetry = true, MaxRetryAttempts = 3)]
        private async Task<CreateProvenPotentialRecordResponse> HandleCreateProvenPotentialRecordRequest(CreateProvenPotentialRecordRequest request)
        {
            var response = await _provenPotentialService.HandleCreateProvenPotentialRecordRequest(request);
            await EventBus.PublishAsync(response);
            return response;
        }

        [EventHandler(Priority = 3, EnableRetry = true, MaxRetryAttempts = 3)]
        private async Task HandleScrimmageCompletedAsync(ScrimmageCompletedEvent evt)
        {
            try
            {
                // Fetch scrimmage data from repository
                var scrimmage = await WabbitBot.Core.Common.Data.DataServiceManager.ScrimmageRepository.GetByIdAsync(evt.ScrimmageId);
                if (scrimmage == null) return;

                // Use event bus to get team ratings
                var team1RatingResp = await EventBus.RequestAsync<GetTeamRatingRequest, GetTeamRatingResponse>(new GetTeamRatingRequest { TeamId = scrimmage.Team1Id });
                var team2RatingResp = await EventBus.RequestAsync<GetTeamRatingRequest, GetTeamRatingResponse>(new GetTeamRatingRequest { TeamId = scrimmage.Team2Id });
                var team1Rating = team1RatingResp?.Rating ?? 0;
                var team2Rating = team2RatingResp?.Rating ?? 0;

                // Calculate rating change using RatingCalculatorService (through event bus)
                var ratingChangeResp = await EventBus.RequestAsync<CalculateRatingChangeRequest, CalculateRatingChangeResponse>(
                    new CalculateRatingChangeRequest
                    {
                        Team1Id = scrimmage.Team1Id,
                        Team2Id = scrimmage.Team2Id,
                        Team1Rating = team1Rating,
                        Team2Rating = team2Rating,
                        EvenTeamFormat = scrimmage.EvenTeamFormat,
                        Team1Score = scrimmage.Team1Score,
                        Team2Score = scrimmage.Team2Score
                    });

                var team1Change = ratingChangeResp?.Team1Change ?? 0.0;
                var team2Change = ratingChangeResp?.Team2Change ?? 0.0;

                // Request creation of proven potential record
                var createRequest = new CreateProvenPotentialRecordRequest
                {
                    MatchId = Guid.Parse(evt.MatchId),
                    ChallengerId = scrimmage.Team1Id,
                    OpponentId = scrimmage.Team2Id,
                    ChallengerRating = team1Rating,
                    OpponentRating = team2Rating,
                    ChallengerConfidence = scrimmage.Team1Confidence,
                    OpponentConfidence = scrimmage.Team2Confidence,
                    ChallengerOriginalRatingChange = team1Change,
                    OpponentOriginalRatingChange = team2Change,
                    EvenTeamFormat = scrimmage.EvenTeamFormat
                };

                await EventBus.PublishAsync(createRequest);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex);
            }
        }

        [EventHandler(Priority = 4, EnableRetry = true, MaxRetryAttempts = 3)]
        private async Task HandleApplyProvenPotentialAdjustmentEvent(ApplyProvenPotentialAdjustmentEvent evt)
        {
            // Publish events to notify the Season system to apply rating adjustments
            // This follows vertical slice architecture - Scrimmage slice communicates via events

            // Apply adjustment to challenger
            await EventBus.PublishAsync(new ApplyTeamRatingChangeEvent
            {
                TeamId = evt.ChallengerId,
                RatingChange = evt.Adjustment,
                EvenTeamFormat = evt.EvenTeamFormat,
                Reason = $"{evt.Reason} (Proven Potential - Challenger)"
            });

            // Apply adjustment to opponent
            await EventBus.PublishAsync(new ApplyTeamRatingChangeEvent
            {
                TeamId = evt.OpponentId,
                RatingChange = evt.Adjustment,
                EvenTeamFormat = evt.EvenTeamFormat,
                Reason = $"{evt.Reason} (Proven Potential - Opponent)"
            });

            Console.WriteLine($"Proven Potential Adjustment Applied: {evt.ChallengerId} and {evt.OpponentId} (+{evt.Adjustment}) via Season system");
        }
    }
}