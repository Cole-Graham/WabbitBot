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

        private void ConfigureMatch(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Match>(entity =>
            {
                entity.ToTable("matches");

                // Configure JSONB columns for complex objects
                entity.Property(m => m.Team1PlayerIds)
                    .HasColumnName("team1_player_ids")
                    .HasColumnType("jsonb");

                entity.Property(m => m.Team2PlayerIds)
                    .HasColumnName("team2_player_ids")
                    .HasColumnType("jsonb");

                entity.Property(m => m.Games)
                    .HasColumnName("games")
                    .HasColumnType("jsonb");

                entity.Property(m => m.AvailableMaps)
                    .HasColumnName("available_maps")
                    .HasColumnType("jsonb");

                entity.Property(m => m.Team1MapBans)
                    .HasColumnName("team1_map_bans")
                    .HasColumnType("jsonb");

                entity.Property(m => m.Team2MapBans)
                    .HasColumnName("team2_map_bans")
                    .HasColumnType("jsonb");

                entity.Property(m => m.StateHistory)
                    .HasColumnName("state_history")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(m => m.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(m => m.Team1Id)
                    .HasColumnName("team1_id");

                entity.Property(m => m.Team2Id)
                    .HasColumnName("team2_id");

                entity.Property(m => m.EvenTeamFormat)
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

                // Foreign key relationships
                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(m => m.Team1Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(m => m.Team2Id)
                    .OnDelete(DeleteBehavior.Restrict);

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

                entity.Property(m => m.FinalGames)
                    .HasColumnName("final_games")
                    .HasColumnType("jsonb");

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
    }
}