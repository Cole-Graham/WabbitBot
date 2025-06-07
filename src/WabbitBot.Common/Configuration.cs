// In WabbitBot.Common
namespace WabbitBot.Common.Configuration;

// Only the minimal interface needed by DiscBot
public interface IBotConfigurationReader
{
    string GetToken();
    // Any other read-only config properties needed by DiscBot
}

// Only expose what DiscBot absolutely needs to know about config
public class BotConfigurationChangedEvent 
{
    public required string Property { get; init; }
    public object? NewValue { get; init; }
}