using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;


namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    /// <summary>
    /// Handler for rating-related events and requests.
    /// </summary>
    public class RatingCalculatorHandler : CoreBaseHandler
    {
        private readonly RatingCalculatorService _ratingService;

        public RatingCalculatorHandler(RatingCalculatorService ratingService)
            : base(CoreEventBus.Instance)
        {
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
        }

        public override Task InitializeAsync()
        {
            // Subscribe to rating-related events
            EventBus.Subscribe<AllTeamOpponentDistributionsRequest>(async request =>
                await HandleAllTeamOpponentDistributionsRequest(request));

            // Subscribe to confidence calculation requests
            EventBus.Subscribe<CalculateConfidenceRequest>(async request =>
                await HandleCalculateConfidenceRequest(request));

            // Subscribe to rating change calculation requests
            EventBus.Subscribe<CalculateRatingChangeRequest>(async request =>
                await HandleCalculateRatingChangeRequest(request));

            return Task.CompletedTask;
        }

        private async Task<AllTeamOpponentDistributionsResponse> HandleAllTeamOpponentDistributionsRequest(
            AllTeamOpponentDistributionsRequest request)
        {
            // Delegate to the rating service
            return await _ratingService.CalculateAllTeamOpponentDistributions(request);
        }

        private async Task<CalculateConfidenceResponse> HandleCalculateConfidenceRequest(
            CalculateConfidenceRequest request)
        {
            var confidence = await _ratingService.CalculateConfidenceAsync(request.TeamId, request.GameSize);
            return new CalculateConfidenceResponse { Confidence = confidence };
        }

        private async Task<CalculateRatingChangeResponse> HandleCalculateRatingChangeRequest(
            CalculateRatingChangeRequest request)
        {
            var (team1Change, team2Change) = await _ratingService.CalculateRatingChangeAsync(
                request.Team1Id, request.Team2Id, request.Team1Rating, request.Team2Rating,
                request.GameSize, request.Team1Score, request.Team2Score);

            return new CalculateRatingChangeResponse
            {
                Team1Change = team1Change,
                Team2Change = team2Change
            };
        }
    }
}