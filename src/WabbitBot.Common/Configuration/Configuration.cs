// Modern .NET Configuration System for WabbitBot
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Configuration
{
    /// <summary>
    /// Modern configuration service using IConfiguration and Options pattern
    /// </summary>
    public interface IBotConfigurationService
    {
        string GetToken();
        ulong? GetDebugGuildId();
        T GetSection<T>(string sectionName)
            where T : class, new();
        void ValidateConfiguration();
    }

    public class BotConfigurationService : IBotConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly BotOptions _botOptions;

        public BotConfigurationService(IConfiguration configuration, IOptions<BotOptions> botOptions)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _botOptions = botOptions?.Value ?? throw new ArgumentNullException(nameof(botOptions));
        }

        public string GetToken() => Environment.GetEnvironmentVariable("WABBITBOT_TOKEN") ?? _botOptions.Token;

        public ulong? GetDebugGuildId()
        {
            var debugGuildIdStr =
                Environment.GetEnvironmentVariable("WABBITBOT_DEBUG_GUILD_ID") ?? _configuration["Bot:DebugGuildId"];

            if (string.IsNullOrEmpty(debugGuildIdStr))
                return null;

            return ulong.TryParse(debugGuildIdStr, out var guildId) ? guildId : null;
        }

        public T GetSection<T>(string sectionName)
            where T : class, new()
        {
            var section = _configuration.GetSection(sectionName);
            var result = new T();
            section.Bind(result);
            return result;
        }

        public void ValidateConfiguration()
        {
            // Validate core bot settings
            if (string.IsNullOrEmpty(_botOptions.Token))
                throw new InvalidOperationException("Bot token is required");

            if (string.IsNullOrEmpty(_botOptions.Database.ConnectionString))
                throw new InvalidOperationException("Database connection string is required");

            // Validate scrimmage configuration
            if (_botOptions.Scrimmage.InitialRating < 0)
                throw new InvalidOperationException("Initial rating must be non-negative");

            if (_botOptions.Scrimmage.KFactor <= 0)
                throw new InvalidOperationException("K-factor must be positive");

            // Validate roster size ranges
            if (
                _botOptions.Scrimmage.RosterSizeRanges.Solo.Min <= 0
                || _botOptions.Scrimmage.RosterSizeRanges.Solo.Max <= 0
            )
                throw new InvalidOperationException("Solo roster size range must be positive");

            if (
                _botOptions.Scrimmage.RosterSizeRanges.Duo.Min <= 0
                || _botOptions.Scrimmage.RosterSizeRanges.Duo.Max <= 0
            )
                throw new InvalidOperationException("Duo roster size range must be positive");

            if (
                _botOptions.Scrimmage.RosterSizeRanges.Squad.Min <= 0
                || _botOptions.Scrimmage.RosterSizeRanges.Squad.Max <= 0
            )
                throw new InvalidOperationException("Squad roster size range must be positive");

            if (_botOptions.Scrimmage.RosterSizeRanges.Solo.Min > _botOptions.Scrimmage.RosterSizeRanges.Solo.Max)
                throw new InvalidOperationException("Solo roster minimum cannot exceed maximum");

            if (_botOptions.Scrimmage.RosterSizeRanges.Duo.Min > _botOptions.Scrimmage.RosterSizeRanges.Duo.Max)
                throw new InvalidOperationException("Duo roster minimum cannot exceed maximum");

            if (_botOptions.Scrimmage.RosterSizeRanges.Squad.Min > _botOptions.Scrimmage.RosterSizeRanges.Squad.Max)
                throw new InvalidOperationException("Squad roster minimum cannot exceed maximum");

            if (_botOptions.Scrimmage.MaxConcurrentScrimmages <= 0)
                throw new InvalidOperationException("Max concurrent scrimmages must be positive");

            if (_botOptions.Scrimmage.BestOf <= 0)
                throw new InvalidOperationException("Scrimmage best of must be positive");

            // Validate tournament configuration
            if (_botOptions.Tournament.BracketSize <= 0)
                throw new InvalidOperationException("Bracket size must be positive");

            if (_botOptions.Tournament.BestOf <= 0)
                throw new InvalidOperationException("Best of must be positive");

            // Validate match configuration
            if (_botOptions.Match.MaxGamesPerMatch <= 0)
                throw new InvalidOperationException("Max games per match must be positive");

            if (_botOptions.Match.DefaultBestOf <= 0)
                throw new InvalidOperationException("Default best of must be positive");

            // Validate leaderboard configuration
            if (_botOptions.Leaderboard.DisplayTopN <= 0)
                throw new InvalidOperationException("Display top N must be positive");

            if (_botOptions.Leaderboard.SeasonLengthDays <= 0)
                throw new InvalidOperationException("Season length must be positive");

            // Validate maps configuration
            if (_botOptions.Maps.Maps == null)
                throw new InvalidOperationException("Maps list is required");
        }
    }

    /// <summary>
    /// Static configuration provider for services that need configuration access
    /// </summary>
    public static class ConfigurationProvider
    {
        private static IBotConfigurationService? _configurationService;
        private static readonly object _lock = new();

        public static void Initialize(IBotConfigurationService configurationService)
        {
            lock (_lock)
            {
                _configurationService =
                    configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            }
        }

        public static IBotConfigurationService GetConfigurationService()
        {
            if (_configurationService is null)
            {
                throw new InvalidOperationException(
                    "Configuration service has not been initialized. Call Initialize() first."
                );
            }
            return _configurationService;
        }

        public static T GetSection<T>(string sectionName)
            where T : class, new()
        {
            return GetConfigurationService().GetSection<T>(sectionName);
        }
    }

    public record BotConfigurationChangedEvent(
        string Property,
        object? NewValue,
        EventBusType EventBusType = EventBusType.Global
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}

// Modern Configuration Models using Options Pattern
namespace WabbitBot.Common.Configuration
{
    public class BotOptions
    {
        public const string SectionName = "Bot";

        public required string Token { get; set; }
        public string LogLevel { get; set; } = "Information";
        public ulong? ServerId { get; set; } = null;
        public DatabaseOptions Database { get; set; } = new();
        public ChannelsOptions Channels { get; set; } = new();
        public RolesOptions Roles { get; set; } = new();
        public ActivityOptions Activity { get; set; } = new();
        public ScrimmageOptions Scrimmage { get; set; } = new();
        public TournamentOptions Tournament { get; set; } = new();
        public MatchOptions Match { get; set; } = new();
        public LeaderboardOptions Leaderboard { get; set; } = new();
        public MapsOptions Maps { get; set; } = new();
        public MapBansOptions MapBans { get; set; } = new();
        public DivisionsOptions Divisions { get; set; } = new();
        public RetentionOptions Retention { get; set; } = new();
        public StorageOptions Storage { get; set; } = new();
    }

    public class DatabaseOptions
    {
        public const string SectionName = "Bot:Database";

        /// <summary>
        /// PostgreSQL connection string. Format: Host=localhost;Database=wabbitbot;Username=user;Password=pass
        /// </summary>
        public string ConnectionString { get; set; } =
            "Host=localhost;Database=wabbitbot;Username=wabbitbot;Password=wabbitbot";

        /// <summary>
        /// Maximum number of connections in the connection pool
        /// </summary>
        public int MaxPoolSize { get; set; } = 10;
    }

    public class ChannelsOptions
    {
        public const string SectionName = "Bot:Channels";
        public ulong? BotChannel { get; set; } = null;
        public ulong? ReplayChannel { get; set; } = null;
        public ulong? DeckChannel { get; set; } = null;
        public ulong? SignupChannel { get; set; } = null;
        public ulong? StandingsChannel { get; set; } = null;
        public ulong? ScrimmageChannel { get; set; } = null;
    }

    public class RolesOptions
    {
        public const string SectionName = "Bot:Roles";
        public ulong? Whitelisted { get; set; } = null;
        public ulong? Admin { get; set; } = null;
        public ulong? Moderator { get; set; } = null;
    }

    public class ActivityOptions
    {
        public const string SectionName = "Bot:Activity";
        public string Type { get; set; } = "Playing";
        public string Name { get; set; } = "Wabbit Wars";
    }

    public class ScrimmageOptions
    {
        public const string SectionName = "Bot:Scrimmage";
        public RosterSizeRanges RosterSizeRanges { get; set; } = new();
        public int TeamJoinLimit { get; set; } = 5; // Number of teams a player can join
        public int RejoinTeamAfterDays { get; set; } = 7; // Timeout for re-joining a team
        public int MaxConcurrentScrimmages { get; set; } = 10;
        public string RatingSystem { get; set; } = "elo";
        public double InitialRating { get; set; } = 1000;
        public double KFactor { get; set; } = 40;
        public int BestOf { get; set; } = 1;

        // Rating System Constants
        public double EloDivisor { get; set; } = 400.0;
        public int MinimumRating { get; set; } = 600;
        public double BaseRatingChange { get; set; } = 40.0;
        public double MaxVarietyBonus { get; set; } = 0.2;
        public double MinVarietyBonus { get; set; } = -0.1;
        public int VarietyWindowDays { get; set; } = 30;
        public double MaxGapPercent { get; set; } = 0.2;
        public int CatchUpTargetRating { get; set; } = 1500;
        public int CatchUpThreshold { get; set; } = 200;
        public double CatchUpMaxBonus { get; set; } = 1.0;
        public bool CatchUpEnabled { get; set; } = true;
        public double MaxMultiplier { get; set; } = 2.0;
        public int VarietyBonusGamesThreshold { get; set; } = 20;
    }

    public class TournamentOptions
    {
        public const string SectionName = "Bot:Tournament";

        public string DefaultFormat { get; set; } = "single-elimination";
        public int BracketSize { get; set; } = 16;
        public int BestOf { get; set; } = 3;
        public int MapBanCount { get; set; } = 1;
        public bool AllowSpectators { get; set; } = true;
        public int MatchTimeoutMinutes { get; set; } = 20;
        public string TieBreakerMethod { get; set; } = "highestScore";
        public int MaxTournamentsPerDay { get; set; } = 3;
    }

    public class MatchOptions
    {
        public const string SectionName = "Bot:Match";

        public int MaxGamesPerMatch { get; set; } = 5;
        public int DefaultBestOf { get; set; } = 3;
        public int MapBanCount { get; set; } = 1;
        public bool DeckCodeRequired { get; set; } = true;
        public bool AllowDuplicateDecks { get; set; } = false;
        public bool DeckSubmissionPerGame { get; set; } = true;
        public int ResultTimeoutMinutes { get; set; } = 10;
    }

    public class LeaderboardOptions
    {
        public const string SectionName = "Bot:Leaderboard";

        public int DisplayTopN { get; set; } = 10;
        public string RankingAlgorithm { get; set; } = "elo";
        public RatingDecayOptions RatingDecay { get; set; } = new();
        public bool SeasonalResets { get; set; } = true;
        public int SeasonLengthDays { get; set; } = 90;
        public double TournamentWeight { get; set; } = 1.5;
        public double ScrimmageWeight { get; set; } = 1.0;
    }

    public class RatingDecayOptions
    {
        public bool Enabled { get; set; } = true;
        public double DecayRatePerWeek { get; set; } = 25;
        public double MinimumRating { get; set; } = 1000;
    }

    public class MapsOptions
    {
        public const string SectionName = "Bot:Maps";
        public List<MapConfiguration> Maps { get; set; } = new();
    }

    public class DivisionsOptions
    {
        public const string SectionName = "Bot:Divisions";

        public List<DivisionConfiguration> Divisions { get; set; } = new();
    }

    public class RetentionOptions
    {
        public const string SectionName = "Bot:Retention";
        public int ArchiveRetentionDays { get; set; } = 365;
        public int JobIntervalHours { get; set; } = 24;
    }

    public class StorageOptions
    {
        public const string SectionName = "Bot:Storage";

        /// <summary>
        /// Base directory for all data storage. Defaults to "data" (relative to app directory).
        /// For production, use absolute paths like "/var/lib/wabbitbot"
        /// </summary>
        public string BaseDataDirectory { get; set; } = "data";

        /// <summary>
        /// Directory for replay file storage (.rpl3 and .zip files)
        /// </summary>
        public string ReplaysDirectory { get; set; } = "data/replays";

        /// <summary>
        /// Base directory for all images
        /// </summary>
        public string ImagesDirectory { get; set; } = "data/images";

        /// <summary>
        /// Directory for map thumbnail images shown in Discord embeds
        /// </summary>
        public string MapsDirectory { get; set; } = "data/images/maps/discord";

        /// <summary>
        /// Directory for division icon images
        /// </summary>
        public string DivisionIconsDirectory { get; set; } = "data/divisions/icons";

        /// <summary>
        /// Directory for custom Discord component images (user-uploaded)
        /// </summary>
        public string DiscordComponentImagesDirectory { get; set; } = "data/images/discord";

        /// <summary>
        /// Directory for default Discord images shipped with the application.
        /// This directory should be read-only in production.
        /// </summary>
        public string DefaultDiscordImagesDirectory { get; set; } = "data/images/default/discord";

        /// <summary>
        /// Resolves a path, making it absolute if it's relative.
        /// Relative paths are resolved against the application base directory.
        /// </summary>
        public string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        }
    }

    public class MapConfiguration
    {
        public string Name { get; set; } = string.Empty; // Display name
        public string ScenarioName { get; set; } = string.Empty; // This is the name in the replay files and MapPack.ndf
        public string Size { get; set; } = string.Empty;
        public string Density { get; set; } = "Medium"; // "Low", "Medium", or "High"
        public string? ThumbnailFilename { get; set; }
        public bool IsInRandomPool { get; set; } = true;
        public bool IsInTournamentPool { get; set; } = true;
    }

    public class RosterSizeRanges
    {
        public RosterSizeRange Solo { get; set; } = new(1, 1);
        public RosterSizeRange Duo { get; set; } = new(4, 5);
        public RosterSizeRange Squad { get; set; } = new(8, 10);
    }

    public record RosterSizeRange(int Min, int Max);

    public class DivisionConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public string Faction { get; set; } = string.Empty; // "BLUFOR" or "REDFOR"
        public string? Description { get; set; }
        public string? IconFilename { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class MapBansOptions
    {
        public const string SectionName = "Bot:MapBans";

        public Dictionary<string, BanConfiguration> BestOf1 { get; set; } = new();
        public Dictionary<string, BanConfiguration> BestOf3 { get; set; } = new();
        public Dictionary<string, BanConfiguration> BestOf5 { get; set; } = new();
        public Dictionary<string, BanConfiguration> BestOf7 { get; set; } = new();

        /// <summary>
        /// Gets the ban configuration for a specific match length and pool size.
        /// </summary>
        /// <param name="bestOf">Match length (1, 3, 5, or 7)</param>
        /// <param name="poolSize">Size of the map pool</param>
        /// <returns>BanConfiguration if found, otherwise returns a default configuration with 0 bans</returns>
        public BanConfiguration GetBanConfig(int bestOf, int poolSize)
        {
            var poolKey = $"poolsize_{poolSize}";
            Dictionary<string, BanConfiguration>? matchDict = bestOf switch
            {
                1 => BestOf1,
                3 => BestOf3,
                5 => BestOf5,
                7 => BestOf7,
                _ => null,
            };

            if (matchDict is not null && matchDict.TryGetValue(poolKey, out var banConfig))
            {
                return banConfig;
            }

            // Return default configuration if not found
            return new BanConfiguration { GuaranteedBans = 0, CoinflipBans = 0 };
        }
    }

    public class BanConfiguration
    {
        public int GuaranteedBans { get; set; }
        public int CoinflipBans { get; set; }
    }
}
