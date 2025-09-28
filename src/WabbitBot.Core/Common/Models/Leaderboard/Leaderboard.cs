using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    [EntityMetadata(
        tableName: "leaderboards",
        archiveTableName: "leaderboard_archive",
        maxCacheSize: 50,
        cacheExpiryMinutes: 10,
        servicePropertyName: "Leaderboards"
    )]
    public class Leaderboard : Entity, ILeaderboardEntity
    {
        public Dictionary<TeamSize, Dictionary<string, LeaderboardItem>> Rankings { get; set; } = new();

        public override Domain Domain => Domain.Leaderboard;

    }

    [EntityMetadata(
        tableName: "leaderboard_items",
        archiveTableName: "leaderboard_item_archive",
        maxCacheSize: 2000,
        cacheExpiryMinutes: 15,
        servicePropertyName: "LeaderboardItems"
    )]
    public class LeaderboardItem : Entity, ILeaderboardEntity
    {
        public List<Guid> PlayerIds { get; set; } = new();
        public Guid TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public double Rating { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsTeam { get; set; }

        public override Domain Domain => Domain.Leaderboard;

    }

    /// <summary>
    /// Represents a season for a specific game size.
    /// Each season belongs to a SeasonGroup for coordination.
    /// </summary>
    [EntityMetadata(
        tableName: "seasons",
        archiveTableName: "season_archive",
        maxCacheSize: 100,
        cacheExpiryMinutes: 30,
        servicePropertyName: "Seasons"
    )]
    public class Season : Entity, ILeaderboardEntity
    {
        public Guid SeasonGroupId { get; set; }
        public TeamSize TeamSize { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, string> ParticipatingTeams { get; set; } = new();
        public Guid SeasonConfigId { get; set; }
        public Dictionary<string, object> ConfigData { get; set; } = new();

        public override Domain Domain => Domain.Leaderboard;
    }


    [EntityMetadata(
        tableName: "season_configs",
        archiveTableName: "season_config_archive",
        maxCacheSize: 20,
        cacheExpiryMinutes: 60,
        servicePropertyName: "SeasonConfigs"
    )]
    public class SeasonConfig : Entity, ILeaderboardEntity
    {
        public bool RatingDecayEnabled { get; set; }
        public double DecayRatePerWeek { get; set; }
        public double MinimumRating { get; set; }

        public override Domain Domain => Domain.Leaderboard;

    }

    /// <summary>
    /// Represents a group of seasons that are coordinated together.
    /// All seasons in a group typically start and end at the same time,
    /// but each season is for a different game size.
    /// </summary>
    [EntityMetadata(
        tableName: "season_groups",
        archiveTableName: "season_group_archive",
        maxCacheSize: 10,
        cacheExpiryMinutes: 120,
        servicePropertyName: "SeasonGroups"
    )]
    public class SeasonGroup : Entity, ILeaderboardEntity
    {
        public string Name { get; set; } = string.Empty;

        public override Domain Domain => Domain.Leaderboard;
    }
}
