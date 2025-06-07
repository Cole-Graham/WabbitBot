using DSharpPlus;
using WabbitBot.Common.Events;
using WabbitBot.Common.Configuration;

namespace WabbitBot.DiscBot.DSharpPlus;

public class DiscordBot
{
    private readonly DiscordClient _client;
    private readonly EventBus _eventBus;
    private readonly ErrorHandler _errorHandler;
    private readonly IGlobalEventBus _globalEventBus;
    private readonly IBotConfigurationReader _config;
    public DiscordBot(IGlobalEventBus globalEventBus, IBotConfigurationReader config)
    {
        _globalEventBus = globalEventBus;
        _config = config;
        _eventBus = new EventBus(globalEventBus);
        _errorHandler = new ErrorHandler(globalEventBus);

        // Create client using builder pattern
        _client = DiscordClientBuilder
            .CreateDefault(
                _config.GetToken(),  // Token set in InitializeAsync
                DiscordIntents.MessageContents |
                DiscordIntents.DirectMessages |
                DiscordIntents.GuildMessages |
                DiscordIntents.Guilds,
                null  // No service collection since we're avoiding DI
            )
            .ConfigureEventHandlers(b =>
            {
                b.HandleSocketClosed((client, args) => _errorHandler.HandleError(
                    new Exception($"Socket closed with code {args.CloseCode}: {args.CloseMessage}")
                ));
                b.HandleSessionCreated((client, args) => Task.CompletedTask);
                b.HandleZombied((client, args) => _errorHandler.HandleError(
                    new Exception("Discord connection zombied")
                ));
            })
            .Build();
    }

    private Task HandleConfigChange(BotConfigurationChangedEvent evt)
    {
        // Handle configuration changes that don't require restart
        // Note: Token changes would require bot restart
        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // TODO: Need to confirm correct DSharpPlus 5.0 token and connection handling
            await _client.ConnectAsync();
        }
        catch (Exception ex)
        {
            await _errorHandler.HandleError(ex);
            throw;
        }
    }

    public Task StartAsync()
    {
        _eventBus.RegisterEventHandlers(_client);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        try
        {
            if (_client != null)
            {
                await _client.DisconnectAsync();
                _client.Dispose();
            }
        }
        catch (Exception ex)
        {
            await _errorHandler.HandleError(ex);
            throw;
        }
    }
}
