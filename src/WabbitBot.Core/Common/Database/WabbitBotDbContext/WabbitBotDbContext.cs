using System;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Matches.Data;
using WabbitBot.Core.Tournaments;
using WabbitBot.Core.Tournaments.Data;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Scrimmages.ScrimmageRating;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Entity Framework Core DbContext for WabbitBot with JSONB support
    /// </summary>
    public partial class WabbitBotDbContext : DbContext
    {
        public WabbitBotDbContext(DbContextOptions<WabbitBotDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure JSONB mappings for complex objects

            // COMMON entities
            ConfigureGame(modelBuilder);
            ConfigureGameStateSnapshot(modelBuilder); // Property class used in Game entity
            ConfigureMap(modelBuilder);
            ConfigurePlayer(modelBuilder);
            ConfigureTeam(modelBuilder);
            ConfigureUser(modelBuilder);

            // LEADERBOARD entities
            ConfigureLeaderboard(modelBuilder);
            ConfigureSeason(modelBuilder);
            ConfigureSeasonGroup(modelBuilder); // Property class used in Season entity

            // MATCH entities
            ConfigureMatch(modelBuilder);
            ConfigureMatchStateSnapshot(modelBuilder); // Property class used in Match entity

            // SCRIMMAGE entities
            ConfigureScrimmage(modelBuilder);
            ConfigureProvenPotentialRecord(modelBuilder); // Property class used in Scrimmage entity

            // TOURNAMENT entities
            ConfigureTournament(modelBuilder);
            ConfigureTournamentStateSnapshot(modelBuilder); // Property class used in Tournament entity

            // Configure indexes for performance
            ConfigureIndexes(modelBuilder);
        }

        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            // JSONB indexes for performance

            #region Player
            // initialschema.player.cs
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.TeamIds)
                .HasMethod("gin")
                .HasDatabaseName("idx_players_team_ids");

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.Name)
                .HasDatabaseName("idx_players_name");

            modelBuilder.Entity<Player>()
                .HasIndex(p => p.IsArchived)
                .HasDatabaseName("idx_players_is_archived");
            #endregion

            #region Team
            // initialschema.team.cs
            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Roster)
                .HasMethod("gin")
                .HasDatabaseName("idx_teams_roster");

            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Name)
                .HasDatabaseName("idx_teams_name");
            #endregion

            #region Game
            // initialschema.game.cs
            modelBuilder.Entity<Game>()
                .HasIndex(g => g.MatchId)
                .HasDatabaseName("idx_games_match_id");

            modelBuilder.Entity<Game>()
                .HasIndex(g => g.Team1PlayerIds)
                .HasMethod("gin")
                .HasDatabaseName("idx_games_team1_player_ids");

            modelBuilder.Entity<Game>()
                .HasIndex(g => g.Team2PlayerIds)
                .HasMethod("gin")
                .HasDatabaseName("idx_games_team2_player_ids");

            modelBuilder.Entity<GameStateSnapshot>()
                .HasIndex(gss => gss.GameId)
                .HasDatabaseName("idx_game_state_snapshots_game_id");

            modelBuilder.Entity<GameStateSnapshot>()
                .HasIndex(gss => gss.Timestamp)
                .HasDatabaseName("idx_game_state_snapshots_timestamp");

            modelBuilder.Entity<GameStateSnapshot>()
                .HasIndex(gss => gss.MatchId)
                .HasDatabaseName("idx_game_state_snapshots_match_id");
            #endregion

            #region Leaderboard
            // initialschema.leaderboards.cs
            modelBuilder.Entity<LeaderboardItem>()
                .HasIndex(le => le.Id)
                .HasDatabaseName("idx_leaderboard_items_id");

            modelBuilder.Entity<LeaderboardItem>()
                .HasIndex(le => le.Rating)
                .HasDatabaseName("idx_leaderboard_items_rating");

            modelBuilder.Entity<LeaderboardItem>()
                .HasIndex(le => le.LastUpdated)
                .HasDatabaseName("idx_leaderboard_items_last_updated");

            modelBuilder.Entity<Season>()
                .HasIndex(s => s.EvenTeamFormat)
                .HasDatabaseName("idx_seasons_game_size");

            modelBuilder.Entity<SeasonConfig>()
                .HasIndex(sc => sc.Id)
                .HasDatabaseName("idx_season_configs_id");

            modelBuilder.Entity<SeasonGroup>()
                .HasIndex(sg => sg.Name)
                .HasDatabaseName("idx_season_groups_name");
            #endregion

            #region Map
            // initialschema.map.cs
            modelBuilder.Entity<Map>()
                .HasIndex(m => m.Name)
                .HasDatabaseName("idx_maps_name");

            modelBuilder.Entity<Map>()
                .HasIndex(m => m.Size)
                .HasDatabaseName("idx_maps_size");

            modelBuilder.Entity<Map>()
                .HasIndex(m => m.IsActive)
                .HasDatabaseName("idx_maps_is_active");
            #endregion

            #region Match
            // initialschema.match.cs
            modelBuilder.Entity<Match>()
                .HasIndex(m => m.Team1Id)
                .HasDatabaseName("idx_matches_team1_id");

            modelBuilder.Entity<Match>()
                .HasIndex(m => m.Team2Id)
                .HasDatabaseName("idx_matches_team2_id");

            modelBuilder.Entity<MatchStateSnapshot>()
                .HasIndex(mss => mss.MatchId)
                .HasDatabaseName("idx_match_state_snapshots_match_id");

            modelBuilder.Entity<MatchStateSnapshot>()
                .HasIndex(mss => mss.Timestamp)
                .HasDatabaseName("idx_match_state_snapshots_timestamp");

            modelBuilder.Entity<MatchStateSnapshot>()
                .HasIndex(mss => mss.WinnerId)
                .HasDatabaseName("idx_match_state_snapshots_winner_id");
            #endregion

            #region Scrimmage
            // initialschema.scrimmage.cs
            modelBuilder.Entity<Scrimmage>()
                .HasIndex(s => s.Team1Id)
                .HasDatabaseName("idx_scrimmages_team1_id");

            modelBuilder.Entity<Scrimmage>()
                .HasIndex(s => s.Team2Id)
                .HasDatabaseName("idx_scrimmages_team2_id");

            modelBuilder.Entity<ProvenPotentialRecord>()
                .HasIndex(ppr => ppr.ChallengerId)
                .HasDatabaseName("idx_proven_potential_records_challenger_id");

            modelBuilder.Entity<ProvenPotentialRecord>()
                .HasIndex(ppr => ppr.OpponentId)
                .HasDatabaseName("idx_proven_potential_records_opponent_id");

            modelBuilder.Entity<ProvenPotentialRecord>()
                .HasIndex(ppr => ppr.EvenTeamFormat)
                .HasDatabaseName("idx_proven_potential_records_game_size");
            #endregion

            #region Tournament
            // initialschema.tournament.cs
            modelBuilder.Entity<Tournament>()
                .HasIndex(t => t.EvenTeamFormat)
                .HasDatabaseName("idx_tournaments_game_size");

            modelBuilder.Entity<TournamentStateSnapshot>()
                .HasIndex(tss => tss.TournamentId)
                .HasDatabaseName("idx_tournament_state_snapshots_tournament_id");

            modelBuilder.Entity<TournamentStateSnapshot>()
                .HasIndex(tss => tss.Timestamp)
                .HasDatabaseName("idx_tournament_state_snapshots_timestamp");

            modelBuilder.Entity<TournamentStateSnapshot>()
                .HasIndex(tss => tss.WinnerTeamId)
                .HasDatabaseName("idx_tournament_state_snapshots_winner_team_id");
            #endregion

            #region User
            // initialschema.user.cs
            modelBuilder.Entity<User>()
                .HasIndex(u => u.DiscordId)
                .IsUnique()
                .HasDatabaseName("idx_users_discord_id");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .HasDatabaseName("idx_users_username");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.IsActive)
                .HasDatabaseName("idx_users_is_active");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.PlayerId)
                .HasDatabaseName("idx_users_player_id");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.LastActive)
                .HasDatabaseName("idx_users_last_active");
            #endregion
        }
    }
}
