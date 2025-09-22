using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Entity Framework Core DbContext for WabbitBot with JSONB support
    /// </summary>
    public partial class WabbitBotDbContext : DbContext
    {
        public DbSet<Game> Games { get; set; } = null!;
        public DbSet<GameStateSnapshot> GameStateSnapshots { get; set; } = null!;

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

                entity.Property(g => g.StateHistory)
                    .HasColumnName("state_history")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(g => g.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(g => g.MatchId)
                    .HasColumnName("match_id");

                entity.Property(g => g.MapId)
                    .HasColumnName("map_id");

                entity.Property(g => g.EvenTeamFormat)
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

                entity.Property(g => g.UserId)
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

                entity.Property(g => g.EvenTeamFormat)
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