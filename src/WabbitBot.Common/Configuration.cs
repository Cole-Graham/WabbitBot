// In WabbitBot.Common

// Configuration service layer
namespace WabbitBot.Common.Configuration
{
    using WabbitBot.Common.Models;
    public interface IBotConfigurationReader
    {
        string GetToken();
        string GetDatabasePath();
        // Any other read-only config properties needed by DiscBot
    }

    public class BotConfigurationReader : IBotConfigurationReader
    {
        private readonly BotConfiguration _config;

        public BotConfigurationReader(BotConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string GetToken() => _config.Token;
        public string GetDatabasePath() => _config.Database.Path;
    }

    public class BotConfigurationChangedEvent
    {
        public required string Property { get; init; }
        public object? NewValue { get; init; }
    }
}

// Configuration data models
namespace WabbitBot.Common.Models
{
    public record BotConfiguration
    {
        public required string Token { get; init; }
        public string LogLevel { get; init; } = "Information";
        public ulong? ServerId { get; init; } = null;
        public DatabaseConfiguration Database { get; init; } = new();
        public ChannelsConfiguration Channels { get; init; } = new();
        public RolesConfiguration Roles { get; init; } = new();
        public ActivityConfiguration Activity { get; init; } = new();
    }

    public record DatabaseConfiguration
    {
        public string Path { get; init; } = "data/wabbitbot.db";
        public int MaxPoolSize { get; init; } = 10;
    }

    public record ChannelsConfiguration
    {
        public ulong? BotChannel { get; init; } = null;
        public ulong? ReplayChannel { get; init; } = null;
        public ulong? DeckChannel { get; init; } = null;
        public ulong? SignupChannel { get; init; } = null;
        public ulong? StandingsChannel { get; init; } = null;
        public ulong? ScrimmageChannel { get; init; } = null;
    }

    public record RolesConfiguration
    {
        public ulong? Whitelisted { get; init; } = null;
        public ulong? Admin { get; init; } = null;
        public ulong? Moderator { get; init; } = null;
    }

    public record ActivityConfiguration
    {
        public string Type { get; init; } = "Playing";
        public string Name { get; init; } = "Wabbit Wars";
    }
}