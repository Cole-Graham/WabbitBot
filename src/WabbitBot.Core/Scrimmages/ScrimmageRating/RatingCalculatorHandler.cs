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

            return Task.CompletedTask;
        }

        private async Task<AllTeamOpponentDistributionsResponse> HandleAllTeamOpponentDistributionsRequest(
            AllTeamOpponentDistributionsRequest request)
        {
            // Delegate to the rating service
            return await _ratingService.CalculateAllTeamOpponentDistributions(request);
        }
    }
}