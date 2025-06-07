using WabbitBot.Common.Configuration;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Configuration;

public class BotConfigurationReader : IBotConfigurationReader
{
    private readonly BotConfiguration _config;

    public BotConfigurationReader(BotConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public string GetToken() => _config.Token;
}