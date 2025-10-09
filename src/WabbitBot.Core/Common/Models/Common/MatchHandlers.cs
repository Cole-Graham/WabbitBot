using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Scrimmages
{
    public static class MatchHandlers
    {
        public static async Task<Result> HandleScrimmageCreatedAsync(ScrimmageCreated evt)
        {
            // Commented out pending generator publishers
            // var publishResult = await PublishMatchProvisioningRequestedAsync(evt.ScrimmageId);
            // if (!publishResult.Success)
            // {
            //     return Result.Failure("Failed to publish match provisioning requested");
            // }

            return Result.CreateSuccess();
        }

        public static async Task<Result> HandleMatchProvisioningRequestedAsync(MatchProvisioningRequested evt)
        {
            // Get scrimmage channel from config
            var scrimmageChannelConfig = ConfigurationProvider
                .GetSection<ChannelsOptions>(ChannelsOptions.SectionName)
                .ScrimmageChannel;
            if (scrimmageChannelConfig is null)
            {
                return Result.Failure("Scrimmage channel not found");
            }
            var scrimmageChannelId = scrimmageChannelConfig.Value;

            var matchCore = new MatchCore();
            var matchResult = await matchCore.CreateScrimmageMatchAsync(evt.ScrimmageId, scrimmageChannelId);
            if (!matchResult.Success)
            {
                return Result.Failure("Failed to create scrimmage match");
            }

            // Commented out pending generator publishers
            // await PublishScrimmageMatchCreatedAsync(matchResult.Data!.Id);

            return Result.CreateSuccess();
        }
    }
}
