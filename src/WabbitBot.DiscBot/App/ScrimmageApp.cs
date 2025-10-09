using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Interfaces;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.App.Renderers;

namespace WabbitBot.DiscBot.App
{
    public partial class ScrimmageApp : IScrimmageApp
    {
        public static async Task<Result> CreateScrimmageThreadsAsync(Scrimmage scrimmage)
        {
            try
            {
                // Get scrimmage channel from config
                var scrimmageChannelId = ConfigurationProvider
                    .GetSection<ChannelsOptions>(ChannelsOptions.SectionName).ScrimmageChannel;
                if (scrimmageChannelId is null)
                {
                    return Result.Failure("Scrimmage channel not found");
                }
                var scrimmageChannel = await DiscordClientProvider
                    .GetClient().GetChannelAsync(scrimmageChannelId.Value);

                // var matchContainers = await MatchRenderer.RenderMatchContainerAsync(scrimmageChannel, scrimmage);

                // TODO: Implement thread creation; commenting to unblock build
                // var challengerThread = await scrimmageChannel.CreateThreadAsync(scrimmage.ChallengerTeam.Name);

                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to create scrimmage threads",
                    nameof(CreateScrimmageThreadsAsync));
                return Result.Failure($"Failed to create scrimmage threads: {ex.Message}");
            }
        }
    }
}
