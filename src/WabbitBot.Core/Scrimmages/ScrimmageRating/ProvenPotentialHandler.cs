using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Scrimmages.ScrimmageRating.Interface;

namespace WabbitBot.Core.Scrimmages.ScrimmageRating
{
    /// <summary>
    /// Handler for proven potential related events and requests.
    /// </summary>
    public class ProvenPotentialHandler : CoreBaseHandler
    {
        private readonly IProvenPotentialRepository _provenPotentialRepo;
        private readonly ProvenPotentialService _provenPotentialService;

        public ProvenPotentialHandler(
            IProvenPotentialRepository provenPotentialRepo,
            ProvenPotentialService provenPotentialService)
            : base(CoreEventBus.Instance)
        {
            _provenPotentialRepo = provenPotentialRepo ?? throw new ArgumentNullException(nameof(provenPotentialRepo));
            _provenPotentialService = provenPotentialService ?? throw new ArgumentNullException(nameof(provenPotentialService));
        }

        public override Task InitializeAsync()
        {
            // Subscribe to proven potential events
            EventBus.Subscribe<CheckProvenPotentialRequest>(async request =>
                await HandleCheckProvenPotentialRequest(request));

            EventBus.Subscribe<CreateProvenPotentialRecordRequest>(async request =>
                await HandleCreateProvenPotentialRecordRequest(request));

            return Task.CompletedTask;
        }

        private async Task<CheckProvenPotentialResponse> HandleCheckProvenPotentialRequest(
            CheckProvenPotentialRequest request)
        {
            // Delegate to the service
            return await _provenPotentialService.HandleCheckProvenPotentialRequest(request);
        }

        private async Task<CreateProvenPotentialRecordResponse> HandleCreateProvenPotentialRecordRequest(
            CreateProvenPotentialRecordRequest request)
        {
            // Delegate to the service
            return await _provenPotentialService.HandleCreateProvenPotentialRecordRequest(request);
        }
    }
}