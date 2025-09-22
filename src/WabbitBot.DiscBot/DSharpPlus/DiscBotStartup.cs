using WabbitBot.Common.Events;
using WabbitBot.DiscBot.DiscBot.Events;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Utilities;

namespace WabbitBot.DiscBot.DSharpPlus;

/// <summary>
/// Application startup orchestration for the Discord bot
/// </summary>
public static class DiscBotStartup
{
    /// <summary>
    /// Initialize the Discord bot and its event infrastructure
    /// </summary>
    public static async Task InitializeAsync(IBotConfigurationService configReader)
    {
        // Database is already initialized in Program.cs (Core project)

        // Initialize thumbnail utility with configuration
        // Note: ThumbnailUtility is initialized in both Core and DiscBot for shared access
        var configuration = ((BotConfigurationService)configReader).Configuration;
        ThumbnailUtility.Initialize(configuration);

        // Initialize the Discord event bus
        await DiscordEventBus.Instance.InitializeAsync();

        // Create the Discord event handler (which will auto-subscribe via source generation)
        var handler = new DiscordEventHandler(configReader);
        await handler.InitializeAsync();
    }
}