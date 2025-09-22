using WabbitBot.DiscBot.DiscBot.Base;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.DiscBot.DiscBot.Events;
using DSharpPlus.Commands;

namespace WabbitBot.DiscBot.DSharpPlus.Commands;

/// <summary>
/// Event-driven command registration handler that registers Discord commands when the client is ready
/// </summary>
[GenerateEventHandler(EventBusType = EventBusType.DiscBot, EnableMetrics = true)]
public partial class CommandRegistrationHandler : DiscordBaseHandler
{
    private CommandsExtension? _commands;

    public CommandRegistrationHandler(IDiscordEventBus discordEventBus) : base(discordEventBus) { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Subscribe to Discord client ready events
        EventBus.Subscribe<DiscordClientReadyEvent>(HandleDiscordClientReady);
    }

    [EventHandler(EventType = "DiscordClientReadyEvent")]
    public async Task HandleDiscordClientReady(DiscordClientReadyEvent evt)
    {
        // Commands are now registered during client setup in DiscordBot.cs
        // This event handler can be used for other initialization tasks
        await Task.CompletedTask;
    }
}
