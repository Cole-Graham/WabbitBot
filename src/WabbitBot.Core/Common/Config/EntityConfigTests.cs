using System.Linq;
using WabbitBot.Core.Common.Models;
using Xunit;

namespace WabbitBot.Core.Common.Config
{
    /// <summary>
    /// Tests for entity configurations
    /// </summary>
    public class EntityConfigTests
    {
        #region Core Entity Tests
        [Fact]
        public void PlayerDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.Player;

            Assert.Equal("players", config.TableName);
            Assert.Equal("player_archive", config.ArchiveTableName);
            Assert.Equal("id", config.IdColumn);
            Assert.Equal(500, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(30), config.DefaultCacheExpiry);

            Assert.Contains("id", config.Columns);
            Assert.Contains("name", config.Columns);
            Assert.Contains("team_ids", config.Columns);
            Assert.Contains("previous_user_ids", config.Columns);
            Assert.Contains("created_at", config.Columns);
            Assert.Contains("updated_at", config.Columns);
        }

        [Fact]
        public void TeamDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.Team;

            Assert.Equal("teams", config.TableName);
            Assert.Equal("team_archive", config.ArchiveTableName);
            Assert.Equal("id", config.IdColumn);
            Assert.Equal(200, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(20), config.DefaultCacheExpiry);

            Assert.Contains("roster", config.Columns);
            Assert.Contains("stats", config.Columns);
        }

        [Fact]
        public void UserDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.User;

            Assert.Equal("users", config.TableName);
            Assert.Equal("user_archive", config.ArchiveTableName);
            Assert.Equal(1000, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(15), config.DefaultCacheExpiry);
        }
        #endregion

        #region Game Entity Tests
        [Fact]
        public void GameDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.Game;

            Assert.Equal("games", config.TableName);
            Assert.Equal("game_archive", config.ArchiveTableName);
            Assert.Equal(300, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(10), config.DefaultCacheExpiry);

            Assert.Contains("team1_player_ids", config.Columns);
            Assert.Contains("team2_player_ids", config.Columns);
            Assert.Contains("state_history", config.Columns);
        }

        [Fact]
        public void GameStateSnapshotDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.GameStateSnapshot;

            Assert.Equal("game_state_snapshots", config.TableName);
            Assert.Equal("game_state_snapshot_archive", config.ArchiveTableName);
            Assert.Equal(500, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(5), config.DefaultCacheExpiry);

            Assert.Contains("game_id", config.Columns);
            Assert.Contains("additional_data", config.Columns);
            Assert.Contains("team1_player_ids", config.Columns);
        }

        [Fact]
        public void MapConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.Map;

            Assert.Equal("maps", config.TableName);
            Assert.Equal("map_archive", config.ArchiveTableName);
            Assert.Equal(100, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromHours(6), config.DefaultCacheExpiry);
        }
        #endregion

        #region Leaderboard Entity Tests
        [Fact]
        public void LeaderboardDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.Leaderboard;

            Assert.Equal("leaderboards", config.TableName);
            Assert.Equal("leaderboard_archive", config.ArchiveTableName);
            Assert.Equal(50, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(30), config.DefaultCacheExpiry);

            Assert.Contains("id", config.Columns);
            Assert.Contains("rankings", config.Columns);
            Assert.Contains("created_at", config.Columns);
            Assert.Contains("updated_at", config.Columns);
        }

        [Fact]
        public void LeaderboardItemDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.LeaderboardItem;

            Assert.Equal("leaderboard_entries", config.TableName);
            Assert.Equal("leaderboard_entry_archive", config.ArchiveTableName);
            Assert.Equal(5000, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(5), config.DefaultCacheExpiry);

            Assert.Contains("leaderboard_id", config.Columns);
            Assert.Contains("player_id", config.Columns);
            Assert.Contains("team_id", config.Columns);
            Assert.Contains("rating", config.Columns);
            Assert.Contains("is_team", config.Columns);
        }

        [Fact]
        public void SeasonDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.Season;

            Assert.Equal("seasons", config.TableName);
            Assert.Equal("season_archive", config.ArchiveTableName);
            Assert.Equal(25, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(30), config.DefaultCacheExpiry);

            Assert.Contains("participating_teams", config.Columns);
            Assert.Contains("config", config.Columns);
        }

        [Fact]
        public void SeasonConfigDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.SeasonConfig;

            Assert.Equal("season_configs", config.TableName);
            Assert.Equal("season_config_archive", config.ArchiveTableName);
            Assert.Equal(100, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(60), config.DefaultCacheExpiry);

            Assert.Contains("season_id", config.Columns);
            Assert.Contains("rating_decay_enabled", config.Columns);
            Assert.Contains("decay_rate_per_week", config.Columns);
            Assert.Contains("minimum_rating", config.Columns);
        }

        [Fact]
        public void SeasonGroupDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.SeasonGroup;

            Assert.Equal("season_groups", config.TableName);
            Assert.Equal("season_group_archive", config.ArchiveTableName);
            Assert.Equal(50, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromHours(2), config.DefaultCacheExpiry);

            Assert.Contains("name", config.Columns);
            Assert.Contains("description", config.Columns);
        }
        #endregion

        #region Match Entity Tests
        [Fact]
        public void MatchDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.Match;

            Assert.Equal("matches", config.TableName);
            Assert.Equal("match_archive", config.ArchiveTableName);
            Assert.Equal(200, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(10), config.DefaultCacheExpiry);

            Assert.Contains("team1_player_ids", config.Columns);
            Assert.Contains("team2_player_ids", config.Columns);
            Assert.Contains("current_state_snapshot", config.Columns);
            Assert.Contains("state_history", config.Columns);
        }

        [Fact]
        public void MatchStateSnapshotDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.MatchStateSnapshot;

            Assert.Equal("match_state_snapshots", config.TableName);
            Assert.Equal("match_state_snapshot_archive", config.ArchiveTableName);
            Assert.Equal(300, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(10), config.DefaultCacheExpiry);

            Assert.Contains("match_id", config.Columns);
            Assert.Contains("games", config.Columns);
            Assert.Contains("available_maps", config.Columns);
        }
        #endregion

        #region Scrimmage Entity Tests
        [Fact]
        public void ScrimmageDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.Scrimmage;

            Assert.Equal("scrimmages", config.TableName);
            Assert.Equal("scrimmage_archive", config.ArchiveTableName);
            Assert.Equal("id", config.IdColumn);
            Assert.Equal(150, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(15), config.DefaultCacheExpiry);

            Assert.Contains("team1_roster_ids", config.Columns);
            Assert.Contains("team2_roster_ids", config.Columns);
            Assert.Contains("team1_rating", config.Columns);
            Assert.Contains("team2_rating", config.Columns);
        }

        [Fact]
        public void ProvenPotentialRecordConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.ProvenPotentialRecord;

            Assert.Equal("proven_potential_records", config.TableName);
            Assert.Equal("proven_potential_record_archive", config.ArchiveTableName);
            Assert.Equal(500, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(30), config.DefaultCacheExpiry);

            Assert.Contains("challenger_id", config.Columns);
            Assert.Contains("opponent_id", config.Columns);
            Assert.Contains("applied_thresholds", config.Columns);
            Assert.Contains("challenger_rating", config.Columns);
            Assert.Contains("opponent_rating", config.Columns);
            Assert.Contains("is_complete", config.Columns);
        }
        #endregion

        #region Tournament Entity Tests
        [Fact]
        public void TournamentDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.Tournament;

            Assert.Equal("tournaments", config.TableName);
            Assert.Equal("tournament_archive", config.ArchiveTableName);
            Assert.Equal(50, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(30), config.DefaultCacheExpiry);

            Assert.Contains("current_state_snapshot", config.Columns);
            Assert.Contains("state_history", config.Columns);
        }

        [Fact]
        public void TournamentStateSnapshotDbConfig_ShouldHaveCorrectSettings()
        {
            var config = EntityConfigFactory.TournamentStateSnapshot;

            Assert.Equal("tournament_state_snapshots", config.TableName);
            Assert.Equal("tournament_state_snapshot_archive", config.ArchiveTableName);
            Assert.Equal(200, config.MaxCacheSize);
            Assert.Equal(TimeSpan.FromMinutes(15), config.DefaultCacheExpiry);

            Assert.Contains("tournament_id", config.Columns);
            Assert.Contains("registered_team_ids", config.Columns);
            Assert.Contains("final_rankings", config.Columns);
        }
        #endregion

        [Fact]
        public void GetAllConfigurations_ShouldReturnAllConfigs()
        {
            var configs = EntityConfigFactory.GetAllConfigurations().ToList();

            Assert.Equal(13, configs.Count);

            // Core entities
            Assert.Contains(configs, c => c.TableName == "players");
            Assert.Contains(configs, c => c.TableName == "teams");
            Assert.Contains(configs, c => c.TableName == "users");

            // Game entities
            Assert.Contains(configs, c => c.TableName == "games");
            Assert.Contains(configs, c => c.TableName == "game_state_snapshots");
            Assert.Contains(configs, c => c.TableName == "maps");

            // Leaderboard entities
            Assert.Contains(configs, c => c.TableName == "leaderboards");
            Assert.Contains(configs, c => c.TableName == "leaderboard_entries");
            Assert.Contains(configs, c => c.TableName == "seasons");
            Assert.Contains(configs, c => c.TableName == "season_configs");
            Assert.Contains(configs, c => c.TableName == "season_groups");

            // Match entities
            Assert.Contains(configs, c => c.TableName == "matches");
            Assert.Contains(configs, c => c.TableName == "match_state_snapshots");

            // Scrimmage entities
            Assert.Contains(configs, c => c.TableName == "scrimmages");
            Assert.Contains(configs, c => c.TableName == "proven_potential_records");

            // Tournament entities
            Assert.Contains(configs, c => c.TableName == "tournaments");
            Assert.Contains(configs, c => c.TableName == "tournament_state_snapshots");
        }

        [Fact]
        public void Configurations_ShouldBeSingletonInstances()
        {
            var config1 = EntityConfigFactory.Player;
            var config2 = EntityConfigFactory.Player;
            var team1 = EntityConfigFactory.Team;
            var team2 = EntityConfigFactory.Team;
            var user1 = EntityConfigFactory.User;
            var user2 = EntityConfigFactory.User;

            var game1 = EntityConfigFactory.Game;
            var game2 = EntityConfigFactory.Game;
            var gameState1 = EntityConfigFactory.GameStateSnapshot;
            var gameState2 = EntityConfigFactory.GameStateSnapshot;
            var map1 = EntityConfigFactory.Map;
            var map2 = EntityConfigFactory.Map;

            var leaderboard1 = EntityConfigFactory.Leaderboard;
            var leaderboard2 = EntityConfigFactory.Leaderboard;
            var leaderboardItem1 = EntityConfigFactory.LeaderboardItem;
            var leaderboardItem2 = EntityConfigFactory.LeaderboardItem;
            var season1 = EntityConfigFactory.Season;
            var season2 = EntityConfigFactory.Season;
            var seasonConfig1 = EntityConfigFactory.SeasonConfig;
            var seasonConfig2 = EntityConfigFactory.SeasonConfig;
            var seasonGroup1 = EntityConfigFactory.SeasonGroup;
            var seasonGroup2 = EntityConfigFactory.SeasonGroup;

            var match1 = EntityConfigFactory.Match;
            var match2 = EntityConfigFactory.Match;
            var matchState1 = EntityConfigFactory.MatchStateSnapshot;
            var matchState2 = EntityConfigFactory.MatchStateSnapshot;

            var scrimmage1 = EntityConfigFactory.Scrimmage;
            var scrimmage2 = EntityConfigFactory.Scrimmage;
            var ppr1 = EntityConfigFactory.ProvenPotentialRecord;
            var ppr2 = EntityConfigFactory.ProvenPotentialRecord;

            var tournament1 = EntityConfigFactory.Tournament;
            var tournament2 = EntityConfigFactory.Tournament;
            var tournamentState1 = EntityConfigFactory.TournamentStateSnapshot;
            var tournamentState2 = EntityConfigFactory.TournamentStateSnapshot;

            Assert.Same(config1, config2);
            Assert.Same(team1, team2);
            Assert.Same(user1, user2);
            Assert.Same(game1, game2);
            Assert.Same(gameState1, gameState2);
            Assert.Same(map1, map2);
            Assert.Same(leaderboard1, leaderboard2);
            Assert.Same(leaderboardItem1, leaderboardItem2);
            Assert.Same(season1, season2);
            Assert.Same(seasonConfig1, seasonConfig2);
            Assert.Same(seasonGroup1, seasonGroup2);
            Assert.Same(match1, match2);
            Assert.Same(matchState1, matchState2);
            Assert.Same(scrimmage1, scrimmage2);
            Assert.Same(ppr1, ppr2);
            Assert.Same(tournament1, tournament2);
            Assert.Same(tournamentState1, tournamentState2);
        }
    }
}
