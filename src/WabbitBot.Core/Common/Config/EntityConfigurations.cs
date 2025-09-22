using System;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Config
{
    /// <summary>
    /// Base class for entity configurations that define database mappings and settings
    /// </summary>
    public abstract class EntityConfig<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets the table name for this entity
        /// </summary>
        public string TableName { get; protected set; }

        /// <summary>
        /// Gets the archive table name for this entity
        /// </summary>
        public string ArchiveTableName { get; protected set; }

        /// <summary>
        /// Gets the column names for this entity
        /// </summary>
        public string[] Columns { get; protected set; }

        /// <summary>
        /// Gets the ID column name
        /// </summary>
        public string IdColumn { get; protected set; } = "id";

        /// <summary>
        /// Gets the maximum cache size for this entity
        /// </summary>
        public int MaxCacheSize { get; protected set; } = 1000;

        /// <summary>
        /// Gets the default cache expiry time
        /// </summary>
        public TimeSpan DefaultCacheExpiry { get; protected set; } = TimeSpan.FromHours(1);


        protected EntityConfig(
            string tableName,
            string archiveTableName,
            string[] columns,
            string idColumn = "id",
            int maxCacheSize = 1000,
            TimeSpan? defaultCacheExpiry = null)
        {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            ArchiveTableName = archiveTableName ?? throw new ArgumentNullException(nameof(archiveTableName));
            Columns = columns ?? throw new ArgumentNullException(nameof(columns));
            IdColumn = idColumn ?? throw new ArgumentNullException(nameof(idColumn));
            MaxCacheSize = maxCacheSize;
            DefaultCacheExpiry = defaultCacheExpiry ?? TimeSpan.FromHours(1);
        }
    }

    /// <summary>
    /// Factory class for creating and accessing entity configurations
    /// </summary>
    public static class EntityConfigFactory
    {
        #region Core Entities
        private static readonly Lazy<PlayerDbConfig> _playerDbConfig = new(() => new PlayerDbConfig());
        private static readonly Lazy<UserDbConfig> _userDbConfig = new(() => new UserDbConfig());
        private static readonly Lazy<TeamDbConfig> _teamDbConfig = new(() => new TeamDbConfig());
        #endregion

        #region Game Entities
        private static readonly Lazy<GameDbConfig> _gameDbConfig = new(() => new GameDbConfig());
        private static readonly Lazy<GameStateSnapshotDbConfig> _gameStateSnapshotDbConfig = new(() => new GameStateSnapshotDbConfig());
        private static readonly Lazy<MapConfig> _mapDbConfig = new(() => new MapConfig());
        #endregion

        #region Leaderboard Entities
        private static readonly Lazy<LeaderboardDbConfig> _leaderboardDbConfig = new(() => new LeaderboardDbConfig());
        private static readonly Lazy<LeaderboardItemDbConfig> _leaderboardItemDbConfig = new(() => new LeaderboardItemDbConfig());
        private static readonly Lazy<SeasonDbConfig> _seasonDbConfig = new(() => new SeasonDbConfig());
        private static readonly Lazy<SeasonConfigDbConfig> _seasonConfigDbConfig = new(() => new SeasonConfigDbConfig());
        private static readonly Lazy<SeasonGroupDbConfig> _seasonGroupDbConfig = new(() => new SeasonGroupDbConfig());
        #endregion

        #region Match Entities
        private static readonly Lazy<MatchDbConfig> _matchDbConfig = new(() => new MatchDbConfig());
        private static readonly Lazy<MatchStateSnapshotDbConfig> _matchStateSnapshotDbConfig = new(() => new MatchStateSnapshotDbConfig());
        #endregion

        #region Scrimmage Entities
        private static readonly Lazy<ScrimmageDbConfig> _scrimmageDbConfig = new(() => new ScrimmageDbConfig());
        private static readonly Lazy<ProvenPotentialRecordConfig> _provenPotentialRecordDbConfig = new(() => new ProvenPotentialRecordConfig());
        #endregion

        #region Tournament Entities
        private static readonly Lazy<TournamentDbConfig> _tournamentDbConfig = new(() => new TournamentDbConfig());
        private static readonly Lazy<TournamentStateSnapshotDbConfig> _tournamentStateSnapshotDbConfig = new(() => new TournamentStateSnapshotDbConfig());
        #endregion

        #region Core Entity Properties
        /// <summary>
        /// Gets the Player configuration
        /// </summary>
        public static PlayerDbConfig Player => _playerDbConfig.Value;

        /// <summary>
        /// Gets the User configuration
        /// </summary>
        public static UserDbConfig User => _userDbConfig.Value;

        /// <summary>
        /// Gets the Team configuration
        /// </summary>
        public static TeamDbConfig Team => _teamDbConfig.Value;
        #endregion

        #region Game Entity Properties
        /// <summary>
        /// Gets the Game configuration
        /// </summary>
        public static GameDbConfig Game => _gameDbConfig.Value;

        /// <summary>
        /// Gets the GameStateSnapshot configuration
        /// </summary>
        public static GameStateSnapshotDbConfig GameStateSnapshot => _gameStateSnapshotDbConfig.Value;

        /// <summary>
        /// Gets the Map configuration
        /// </summary>
        public static MapConfig Map => _mapDbConfig.Value;
        #endregion

        #region Leaderboard Entity Properties
        /// <summary>
        /// Gets the Leaderboard configuration
        /// </summary>
        public static LeaderboardDbConfig Leaderboard => _leaderboardDbConfig.Value;

        /// <summary>
        /// Gets the LeaderboardItem configuration
        /// </summary>
        public static LeaderboardItemDbConfig LeaderboardItem => _leaderboardItemDbConfig.Value;

        /// <summary>
        /// Gets the Season configuration
        /// </summary>
        public static SeasonDbConfig Season => _seasonDbConfig.Value;

        /// <summary>
        /// Gets the SeasonConfig configuration
        /// </summary>
        public static SeasonConfigDbConfig SeasonConfig => _seasonConfigDbConfig.Value;

        /// <summary>
        /// Gets the SeasonGroup configuration
        /// </summary>
        public static SeasonGroupDbConfig SeasonGroup => _seasonGroupDbConfig.Value;
        #endregion

        #region Match Entity Properties
        /// <summary>
        /// Gets the Match configuration
        /// </summary>
        public static MatchDbConfig Match => _matchDbConfig.Value;

        /// <summary>
        /// Gets the MatchStateSnapshot configuration
        /// </summary>
        public static MatchStateSnapshotDbConfig MatchStateSnapshot => _matchStateSnapshotDbConfig.Value;
        #endregion

        #region Scrimmage Entity Properties
        /// <summary>
        /// Gets the Scrimmage configuration
        /// </summary>
        public static ScrimmageDbConfig Scrimmage => _scrimmageDbConfig.Value;

        /// <summary>
        /// Gets the ProvenPotentialRecord configuration
        /// </summary>
        public static ProvenPotentialRecordConfig ProvenPotentialRecord => _provenPotentialRecordDbConfig.Value;
        #endregion

        #region Tournament Entity Properties
        /// <summary>
        /// Gets the Tournament configuration
        /// </summary>
        public static TournamentDbConfig Tournament => _tournamentDbConfig.Value;

        /// <summary>
        /// Gets the TournamentStateSnapshot configuration
        /// </summary>
        public static TournamentStateSnapshotDbConfig TournamentStateSnapshot => _tournamentStateSnapshotDbConfig.Value;
        #endregion

        /// <summary>
        /// Gets all entity configurations
        /// </summary>
        public static IEnumerable<IEntityConfig> GetAllConfigurations()
        {
            // Core entities
            yield return Player;
            yield return User;
            yield return Team;

            // Game entities
            yield return Game;
            yield return GameStateSnapshot;
            yield return Map;

            // Leaderboard entities
            yield return Leaderboard;
            yield return LeaderboardItem;
            yield return Season;
            yield return SeasonConfig;
            yield return SeasonGroup;

            // Match entities
            yield return Match;
            yield return MatchStateSnapshot;

            // Scrimmage entities
            yield return Scrimmage;
            yield return ProvenPotentialRecord;

            // Tournament entities
            yield return Tournament;
            yield return TournamentStateSnapshot;
        }
    }

    /// <summary>
    /// Base interface for entity configurations
    /// </summary>
    public interface IEntityConfig
    {
        string TableName { get; }
        string ArchiveTableName { get; }
        string[] Columns { get; }
        string IdColumn { get; }
        int MaxCacheSize { get; }
        TimeSpan DefaultCacheExpiry { get; }
    }
}