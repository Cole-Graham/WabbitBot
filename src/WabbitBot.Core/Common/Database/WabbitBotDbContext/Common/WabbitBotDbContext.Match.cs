using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Entity Framework Core DbContext for WabbitBot with JSONB support
    /// </summary>
    public partial class WabbitBotDbContext : DbContext
    {
        public DbSet<Match> Matches { get; set; } = null!;
        public DbSet<MatchStateSnapshot> MatchStateSnapshots { get; set; } = null!;
        public DbSet<MatchParticipant> MatchParticipants { get; set; } = null!;
        public DbSet<TeamOpponentEncounter> TeamOpponentEncounters { get; set; } = null!;
        public DbSet<Game> Games { get; set; } = null!;
        public DbSet<GameStateSnapshot> GameStateSnapshots { get; set; } = null!;

        private void ConfigureMatch(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Match>(entity =>
            {
                entity.ToTable("matches");

                // Configure JSONB columns for complex objects

                // TODO: These were moved to MatchParticipant
                // entity.Property(m => m.Team1PlayerIds)
                //     .HasColumnName("team1_player_ids")
                //     .HasColumnType("jsonb");

                // entity.Property(m => m.Team2PlayerIds)
                //     .HasColumnName("team2_player_ids")
                //     .HasColumnType("jsonb");

                entity.Property(m => m.AvailableMaps)
                    .HasColumnName("available_maps")
                    .HasColumnType("jsonb");

                entity.Property(m => m.Team1MapBans)
                    .HasColumnName("team1_map_bans")
                    .HasColumnType("jsonb");

                entity.Property(m => m.Team2MapBans)
                    .HasColumnName("team2_map_bans")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(m => m.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                // TODO: These were moved to MatchParticipant
                // entity.Property(m => m.Team1Id)
                //     .HasColumnName("team1_id");

                // entity.Property(m => m.Team2Id)
                //     .HasColumnName("team2_id");

                entity.Property(m => m.TeamSize)
                    .HasColumnName("even_team_format");

                entity.Property(m => m.StartedAt)
                    .HasColumnName("started_at");

                entity.Property(m => m.CompletedAt)
                    .HasColumnName("completed_at");

                entity.Property(m => m.WinnerId)
                    .HasColumnName("winner_id");

                entity.Property(m => m.ParentId)
                    .HasColumnName("parent_id");

                entity.Property(m => m.ParentType)
                    .HasColumnName("parent_type");

                entity.Property(m => m.BestOf)
                    .HasColumnName("best_of");

                entity.Property(m => m.PlayToCompletion)
                    .HasColumnName("play_to_completion");

                entity.Property(m => m.ChannelId)
                    .HasColumnName("channel_id");

                entity.Property(m => m.Team1ThreadId)
                    .HasColumnName("team1_thread_id");

                entity.Property(m => m.Team2ThreadId)
                    .HasColumnName("team2_thread_id");

                entity.Property(m => m.Team1MapBansSubmittedAt)
                    .HasColumnName("team1_map_bans_submitted_at");

                entity.Property(m => m.Team2MapBansSubmittedAt)
                    .HasColumnName("team2_map_bans_submitted_at");

                entity.Property(m => m.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(m => m.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(m => m.Id);

                // TODO: These were moved to MatchParticipant
                // Foreign key relationships
                // entity.HasOne<Team>()
                //     .WithMany()
                //     .HasForeignKey(m => m.Team1Id)
                //     .OnDelete(DeleteBehavior.Restrict);

                // entity.HasOne<Team>()
                //     .WithMany()
                //     .HasForeignKey(m => m.Team2Id)
                //     .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(m => m.WinnerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }



        private void ConfigureMatchStateSnapshot(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MatchStateSnapshot>(entity =>
            {
                entity.ToTable("match_state_snapshots");

                entity.Property(m => m.Id)
                    .HasColumnName("id");

                entity.Property(m => m.MatchId)
                    .HasColumnName("match_id");

                entity.Property(m => m.Timestamp)
                    .HasColumnName("timestamp");

                entity.Property(m => m.UserId)
                    .HasColumnName("user_id")
                    .HasMaxLength(255);

                entity.Property(m => m.PlayerName)
                    .HasColumnName("player_name")
                    .HasMaxLength(255);

                entity.Property(m => m.AdditionalData)
                    .HasColumnName("additional_data")
                    .HasColumnType("jsonb");

                entity.Property(m => m.StartedAt)
                    .HasColumnName("started_at");

                entity.Property(m => m.CompletedAt)
                    .HasColumnName("completed_at");

                entity.Property(m => m.CancelledAt)
                    .HasColumnName("cancelled_at");

                entity.Property(m => m.ForfeitedAt)
                    .HasColumnName("forfeited_at");

                entity.Property(m => m.WinnerId)
                    .HasColumnName("winner_id")
                    .HasMaxLength(255);

                entity.Property(m => m.CancelledByUserId)
                    .HasColumnName("cancelled_by_user_id")
                    .HasMaxLength(255);

                entity.Property(m => m.ForfeitedByUserId)
                    .HasColumnName("forfeited_by_user_id")
                    .HasMaxLength(255);

                entity.Property(m => m.ForfeitedTeamId)
                    .HasColumnName("forfeited_team_id")
                    .HasMaxLength(255);

                entity.Property(m => m.CancellationReason)
                    .HasColumnName("cancellation_reason")
                    .HasMaxLength(1000);

                entity.Property(m => m.ForfeitReason)
                    .HasColumnName("forfeit_reason")
                    .HasMaxLength(1000);

                entity.Property(m => m.CurrentGameNumber)
                    .HasColumnName("current_game_number");

                entity.Property(m => m.Games)
                    .HasColumnName("games")
                    .HasColumnType("jsonb");

                entity.Property(m => m.CurrentMapId)
                    .HasColumnName("current_map_id")
                    .HasMaxLength(255);

                entity.Property(m => m.FinalScore)
                    .HasColumnName("final_score")
                    .HasMaxLength(500);

                entity.Property(m => m.AvailableMaps)
                    .HasColumnName("available_maps")
                    .HasColumnType("jsonb");

                entity.Property(m => m.Team1MapBans)
                    .HasColumnName("team1_map_bans")
                    .HasColumnType("jsonb");

                entity.Property(m => m.Team2MapBans)
                    .HasColumnName("team2_map_bans")
                    .HasColumnType("jsonb");

                entity.Property(m => m.Team1BansSubmitted)
                    .HasColumnName("team1_bans_submitted");

                entity.Property(m => m.Team2BansSubmitted)
                    .HasColumnName("team2_bans_submitted");

                entity.Property(m => m.Team1BansConfirmed)
                    .HasColumnName("team1_bans_confirmed");

                entity.Property(m => m.Team2BansConfirmed)
                    .HasColumnName("team2_bans_confirmed");

                entity.Property(m => m.FinalMapPool)
                    .HasColumnName("final_map_pool")
                    .HasColumnType("jsonb");

                entity.Property(m => m.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(m => m.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(m => m.Id);

                // Indexes
                entity.HasIndex(m => m.MatchId)
                    .HasDatabaseName("idx_match_state_snapshots_match_id");

                entity.HasIndex(m => m.Timestamp)
                    .HasDatabaseName("idx_match_state_snapshots_timestamp");

                entity.HasIndex(m => m.WinnerId)
                    .HasDatabaseName("idx_match_state_snapshots_winner_id");
            });
        }

        private void ConfigureMatchParticipant(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MatchParticipant>(entity =>
            {
                entity.ToTable("match_participants");

                entity.Property(mp => mp.Id)
                    .HasColumnName("id");

                entity.Property(mp => mp.MatchId)
                    .HasColumnName("match_id");

                entity.Property(mp => mp.TeamId)
                    .HasColumnName("team_id");

                entity.Property(mp => mp.IsWinner)
                    .HasColumnName("is_winner");

                entity.Property(mp => mp.TeamNumber)
                    .HasColumnName("team_number");

                entity.Property(mp => mp.PlayerIds)
                    .HasColumnName("player_ids")
                    .HasColumnType("jsonb");

                entity.Property(mp => mp.JoinedAt)
                    .HasColumnName("joined_at");

                entity.Property(mp => mp.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(mp => mp.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(mp => mp.Id);

                // Foreign key relationships
                entity.HasOne(mp => mp.Match)
                    .WithMany(m => m.Participants)
                    .HasForeignKey(mp => mp.MatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mp => mp.Team)
                    .WithMany()
                    .HasForeignKey(mp => mp.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(mp => mp.MatchId)
                    .HasDatabaseName("idx_match_participants_match_id");

                entity.HasIndex(mp => mp.TeamId)
                    .HasDatabaseName("idx_match_participants_team_id");

                entity.HasIndex(mp => mp.IsWinner)
                    .HasDatabaseName("idx_match_participants_is_winner");
            });
        }

        private void ConfigureTeamOpponentEncounter(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamOpponentEncounter>(entity =>
            {
                entity.ToTable("team_opponent_encounters");

                entity.Property(toe => toe.Id)
                    .HasColumnName("id");

                entity.Property(toe => toe.TeamId)
                    .HasColumnName("team_id");

                entity.Property(toe => toe.OpponentId)
                    .HasColumnName("opponent_id");

                entity.Property(toe => toe.MatchId)
                    .HasColumnName("match_id");

                entity.Property(toe => toe.TeamSize)
                    .HasColumnName("even_team_format");

                entity.Property(toe => toe.EncounteredAt)
                    .HasColumnName("encountered_at");

                entity.Property(toe => toe.Won)
                    .HasColumnName("won");

                entity.Property(toe => toe.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(toe => toe.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(toe => toe.Id);

                // Foreign key relationships
                entity.HasOne(toe => toe.Team)
                    .WithMany(t => t.RecentOpponents)
                    .HasForeignKey(toe => toe.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(toe => toe.Opponent)
                    .WithMany()
                    .HasForeignKey(toe => toe.OpponentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(toe => toe.Match)
                    .WithMany(m => m.OpponentEncounters)
                    .HasForeignKey(toe => toe.MatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(toe => toe.TeamId)
                    .HasDatabaseName("idx_team_opponent_encounters_team_id");

                entity.HasIndex(toe => toe.OpponentId)
                    .HasDatabaseName("idx_team_opponent_encounters_opponent_id");

                entity.HasIndex(toe => toe.MatchId)
                    .HasDatabaseName("idx_team_opponent_encounters_match_id");

                entity.HasIndex(toe => toe.TeamSize)
                    .HasDatabaseName("idx_team_opponent_encounters_even_team_format");

                entity.HasIndex(toe => toe.EncounteredAt)
                    .HasDatabaseName("idx_team_opponent_encounters_encountered_at");

                entity.HasIndex(toe => new { toe.TeamId, toe.OpponentId })
                    .HasDatabaseName("idx_team_opponent_encounters_team_opponent");
            });
        }

        private void ConfigureGame(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>(entity =>
            {
                entity.ToTable("games");

                // Configure JSONB columns for complex objects
                entity.Property(g => g.Team1PlayerIds)
                    .HasColumnName("team1_player_ids")
                    .HasColumnType("jsonb");

                entity.Property(g => g.Team2PlayerIds)
                    .HasColumnName("team2_player_ids")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(g => g.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(g => g.MatchId)
                    .HasColumnName("match_id");

                entity.Property(g => g.MapId)
                    .HasColumnName("map_id");

                entity.Property(g => g.TeamSize)
                    .HasColumnName("even_team_format");

                entity.Property(g => g.GameNumber)
                    .HasColumnName("game_number");

                entity.Property(g => g.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(g => g.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(g => g.Id);

                // Foreign key relationships
                entity.HasOne<Match>()
                    .WithMany(m => m.Games)
                    .HasForeignKey(g => g.MatchId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Map>()
                    .WithMany()
                    .HasForeignKey(g => g.MapId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureGameStateSnapshot(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameStateSnapshot>(entity =>
            {
                entity.ToTable("game_state_snapshots");

                entity.Property(g => g.Id)
                    .HasColumnName("id");

                entity.Property(g => g.GameId)
                    .HasColumnName("game_id");

                entity.Property(g => g.Timestamp)
                    .HasColumnName("timestamp");

                entity.Property(g => g.PlayerId)
                    .HasColumnName("user_id")
                    .HasMaxLength(255);

                entity.Property(g => g.PlayerName)
                    .HasColumnName("player_name")
                    .HasMaxLength(255);

                entity.Property(g => g.AdditionalData)
                    .HasColumnName("additional_data")
                    .HasColumnType("jsonb");

                entity.Property(g => g.StartedAt)
                    .HasColumnName("started_at");

                entity.Property(g => g.CompletedAt)
                    .HasColumnName("completed_at");

                entity.Property(g => g.CancelledAt)
                    .HasColumnName("cancelled_at");

                entity.Property(g => g.ForfeitedAt)
                    .HasColumnName("forfeited_at");

                entity.Property(g => g.WinnerId)
                    .HasColumnName("winner_id")
                    .HasMaxLength(255);

                entity.Property(g => g.CancelledByUserId)
                    .HasColumnName("cancelled_by_user_id")
                    .HasMaxLength(255);

                entity.Property(g => g.ForfeitedByUserId)
                    .HasColumnName("forfeited_by_user_id")
                    .HasMaxLength(255);

                entity.Property(g => g.ForfeitedTeamId)
                    .HasColumnName("forfeited_team_id")
                    .HasMaxLength(255);

                entity.Property(g => g.CancellationReason)
                    .HasColumnName("cancellation_reason")
                    .HasMaxLength(1000);

                entity.Property(g => g.ForfeitReason)
                    .HasColumnName("forfeit_reason")
                    .HasMaxLength(1000);

                entity.Property(g => g.Team1DeckCode)
                    .HasColumnName("team1_deck_code")
                    .HasMaxLength(1000);

                entity.Property(g => g.Team2DeckCode)
                    .HasColumnName("team2_deck_code")
                    .HasMaxLength(1000);

                entity.Property(g => g.Team1DeckSubmittedAt)
                    .HasColumnName("team1_deck_submitted_at");

                entity.Property(g => g.Team2DeckSubmittedAt)
                    .HasColumnName("team2_deck_submitted_at");

                entity.Property(g => g.Team1DeckConfirmed)
                    .HasColumnName("team1_deck_confirmed");

                entity.Property(g => g.Team2DeckConfirmed)
                    .HasColumnName("team2_deck_confirmed");

                entity.Property(g => g.Team1DeckConfirmedAt)
                    .HasColumnName("team1_deck_confirmed_at");

                entity.Property(g => g.Team2DeckConfirmedAt)
                    .HasColumnName("team2_deck_confirmed_at");

                entity.Property(g => g.MatchId)
                    .HasColumnName("match_id")
                    .HasMaxLength(255);

                entity.Property(g => g.MapId)
                    .HasColumnName("map_id")
                    .HasMaxLength(255);

                entity.Property(g => g.TeamSize)
                    .HasColumnName("game_size");

                entity.Property(g => g.Team1PlayerIds)
                    .HasColumnName("team1_player_ids")
                    .HasColumnType("jsonb");

                entity.Property(g => g.Team2PlayerIds)
                    .HasColumnName("team2_player_ids")
                    .HasColumnType("jsonb");

                entity.Property(g => g.GameNumber)
                    .HasColumnName("game_number");

                entity.Property(g => g.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(g => g.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(g => g.Id);

                // Indexes
                entity.HasIndex(g => g.GameId)
                    .HasDatabaseName("idx_game_state_snapshots_game_id");

                entity.HasIndex(g => g.Timestamp)
                    .HasDatabaseName("idx_game_state_snapshots_timestamp");

                entity.HasIndex(g => g.MatchId)
                    .HasDatabaseName("idx_game_state_snapshots_match_id");
            });
        }
    }
}