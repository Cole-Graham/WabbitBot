using System.ComponentModel;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Tournament;

namespace WabbitBot.Core.Common.Models.Leaderboard
{
    [EntityMetadata(
        tableName: "scrimmage_leaderboards",
        archiveTableName: "scrimmage_leaderboard_archive",
        maxCacheSize: 50,
        cacheExpiryMinutes: 10,
        servicePropertyName: "ScrimmageLeaderboards",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class ScrimmageLeaderboard : Entity, ILeaderboardEntity
    {
        // Navigation properties
        public Guid SeasonId { get; set; }
        public Season Season { get; set; } = new();
        public virtual ICollection<ScrimmageLeaderboardItem> LeaderboardItems { get; set; } =
            new List<ScrimmageLeaderboardItem>();

        // Data properties
        public string Name { get; set; } = string.Empty; // For Season names like "Fall 2024", "Season 1", "Preseason 2", etc.
        public TeamSize TeamSize { get; set; }
        public override Domain Domain => Domain.Leaderboard;
    }

    [EntityMetadata(
        tableName: "tournament_leaderboards",
        archiveTableName: "tournament_leaderboard_archive",
        maxCacheSize: 50,
        cacheExpiryMinutes: 10,
        servicePropertyName: "TournamentLeaderboards",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class TournamentLeaderboard : Entity, ILeaderboardEntity
    {
        // Navigation properties
        public Guid SeasonId { get; set; }
        public Season Season { get; set; } = new();
        public TeamSize TeamSize { get; set; }
        public virtual ICollection<TournamentLeaderboardItem> LeaderboardItems { get; set; } =
            new List<TournamentLeaderboardItem>();
        public override Domain Domain => Domain.Leaderboard;
    }

    [EntityMetadata(
        tableName: "scrimmage_leaderboard_items",
        archiveTableName: "scrimmage_leaderboard_item_archive",
        maxCacheSize: 2000,
        cacheExpiryMinutes: 15,
        servicePropertyName: "ScrimmageLeaderboardItems",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class ScrimmageLeaderboardItem : Entity, ILeaderboardEntity
    {
        // Navigation properties
        public Guid TeamId { get; set; }
        public Team Team { get; set; } = new();
        public List<Guid> PlayerIds { get; set; } = new();
        public ICollection<Player> Players { get; set; } = new List<Player>();
        public Guid ScrimmageLeaderboardId { get; set; }
        public ScrimmageLeaderboard ScrimmageLeaderboard { get; set; } = new();

        // Data properties
        public string Name { get; set; } = string.Empty;
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public DateTime LastUpdated { get; set; }
        public int Rank { get; set; }
        public double Rating { get; set; }
        public double RecentRatingChange { get; set; }

        public override Domain Domain => Domain.Leaderboard;
    }

    [EntityMetadata(
        tableName: "tournament_leaderboard_items",
        archiveTableName: "tournament_leaderboard_item_archive",
        maxCacheSize: 2000,
        cacheExpiryMinutes: 15,
        servicePropertyName: "TournamentLeaderboardItems",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class TournamentLeaderboardItem : Entity, ILeaderboardEntity
    {
        // Navigation properties
        public Guid TeamId { get; set; }
        public Team Team { get; set; } = new();
        public List<Guid> PlayerIds { get; set; } = new();
        public ICollection<Player> Players { get; set; } = new List<Player>();
        public Guid TournamentLeaderboardId { get; set; }
        public TournamentLeaderboard TournamentLeaderboard { get; set; } = new();

        // Navigation to tournaments this Team has played in

        // Data properties
        public string Name { get; set; } = string.Empty;
        public int TournamentPoints { get; set; }
        public List<int> TournamentPlacements { get; set; } = new();
        public int TournamentsPlayedCount { get; set; }
        public double AveragePlacement { get; set; }
        public int BestPlacement { get; set; }
        public int Rank { get; set; }
        public double Rating { get; set; }
        public double RecentRatingChange { get; set; }
        public DateTime LastUpdated { get; set; }

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
        servicePropertyName: "Seasons",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class Season : Entity, ILeaderboardEntity
    {
        // Navigation properties
        public Guid SeasonConfigId { get; set; }
        public SeasonConfig Config { get; set; } = new();
        public ICollection<ScrimmageLeaderboard> ScrimmageLeaderboards { get; set; } = new List<ScrimmageLeaderboard>();
        public ICollection<TournamentLeaderboard> TournamentLeaderboards { get; set; } =
            new List<TournamentLeaderboard>();

        // Data properties
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        public override Domain Domain => Domain.Leaderboard;
    }

    [EntityMetadata(
        tableName: "season_configs",
        archiveTableName: "season_config_archive",
        maxCacheSize: 20,
        cacheExpiryMinutes: 60,
        servicePropertyName: "SeasonConfigs",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class SeasonConfig : Entity, ILeaderboardEntity
    {
        public bool ResetScrimmageRatingsOnStart { get; set; }
        public bool ResetTournamentRatingsOnStart { get; set; }
        public bool ScrimmageRatingDecay { get; set; }
        public bool TournamentRatingDecay { get; set; }
        public double ScrimmageDecayRatePerWeek { get; set; }
        public double TournamentDecayRatePerWeek { get; set; }

        public override Domain Domain => Domain.Leaderboard;
    }
}
