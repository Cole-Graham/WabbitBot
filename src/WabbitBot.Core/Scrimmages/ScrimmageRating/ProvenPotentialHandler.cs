using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Scrimmages.ScrimmageRating.Interface;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    /// <summary>
    /// Handler for proven potential related events and requests.
    /// </summary>
    public class ProvenPotentialHandler : CoreBaseHandler
    {
        private readonly IProvenPotentialRepository _provenPotentialRepo;
        private readonly ProvenPotentialService _provenPotentialService;
        private readonly SeasonRatingService _seasonRatingService;

        public ProvenPotentialHandler(
            IProvenPotentialRepository provenPotentialRepo,
            ProvenPotentialService provenPotentialService,
            SeasonRatingService seasonRatingService)
            : base(CoreEventBus.Instance)
        {
            _provenPotentialRepo = provenPotentialRepo ?? throw new ArgumentNullException(nameof(provenPotentialRepo));
            _provenPotentialService = provenPotentialService ?? throw new ArgumentNullException(nameof(provenPotentialService));
            _seasonRatingService = seasonRatingService ?? throw new ArgumentNullException(nameof(seasonRatingService));
        }

        public override Task InitializeAsync()
        {
            // Subscribe to proven potential check requests
            EventBus.Subscribe<CheckProvenPotentialRequest>(async request =>
            {
                try
                {
                    var response = await _provenPotentialService.HandleCheckProvenPotentialRequest(request);
                    await EventBus.PublishAsync(response);
                }
                catch (Exception ex)
                {
                    // TODO: Add error handler
                    Console.WriteLine($"Error handling CheckProvenPotentialRequest: {ex.Message}");
                }
            });

            // Subscribe to create proven potential record requests
            EventBus.Subscribe<CreateProvenPotentialRecordRequest>(async request =>
            {
                try
                {
                    var response = await _provenPotentialService.HandleCreateProvenPotentialRecordRequest(request);
                    await EventBus.PublishAsync(response);
                }
                catch (Exception ex)
                {
                    // TODO: Add error handler
                    Console.WriteLine($"Error handling CreateProvenPotentialRecordRequest: {ex.Message}");
                }
            });

            // Subscribe to apply proven potential adjustment events
            EventBus.Subscribe<ApplyProvenPotentialAdjustmentEvent>(async evt =>
            {
                try
                {
                    await ApplyRatingAdjustmentAsync(evt);
                }
                catch (Exception ex)
                {
                    // TODO: Add error handler
                    Console.WriteLine($"Error handling ApplyProvenPotentialAdjustmentEvent: {ex.Message}");
                }
            });

            return Task.CompletedTask;
        }

        private async Task ApplyRatingAdjustmentAsync(ApplyProvenPotentialAdjustmentEvent evt)
        {
            // Use SeasonRatingService to apply the rating adjustment
            // This ensures all rating updates go through the Season system
            await _seasonRatingService.ApplyRatingChangeAsync(
                evt.Team1Id,
                evt.GameSize,
                evt.Adjustment,
                evt.Reason
            );

            await _seasonRatingService.ApplyRatingChangeAsync(
                evt.Team2Id,
                evt.GameSize,
                evt.Adjustment,
                evt.Reason
            );

            Console.WriteLine($"Proven Potential Adjustment Applied: {evt.Team1Id} and {evt.Team2Id} (+{evt.Adjustment}) via Season system");
        }
    }
}