using WabbitBot.Common.Events;
using WabbitBot.Common.Models;
using WabbitBot.Common.Configuration;
using WabbitBot.DiscBot.DSharpPlus;

namespace WabbitBot.DiscBot.DiscBot.Events;

public class DiscordEventHandler
{
    private readonly IGlobalEventBus _globalEventBus;
    private readonly IBotConfigurationReader _configReader;
    private DiscordBot? _discordBot;

    public DiscordEventHandler(IGlobalEventBus globalEventBus, IBotConfigurationReader config)
    {
        _globalEventBus = globalEventBus;
        _configReader = config ?? throw new ArgumentNullException(nameof(config));

        // Subscribe to relevant events
        _globalEventBus.Subscribe<StartupInitiatedEvent>(HandleStartupInitiated);
        _globalEventBus.Subscribe<SystemReadyEvent>(HandleSystemReady);
    }

    private async Task HandleStartupInitiated(StartupInitiatedEvent @event)
    {
        // Create the DiscordBot instance but don't start it yet
        _discordBot = new DiscordBot(_globalEventBus, _configReader);

        // Initialize the bot with the token
        await _discordBot.InitializeAsync();
    }

    private async Task HandleSystemReady(SystemReadyEvent @event)
    {
        if (_discordBot is null)
        {
            throw new InvalidOperationException("Discord bot not initialized before system ready event");
        }

        // Now start the bot
        await _discordBot.StartAsync();

        // Signal that the application is fully ready
        await _globalEventBus.PublishAsync(new ApplicationReadyEvent(TimeSpan.Zero, _configReader));
    }
}