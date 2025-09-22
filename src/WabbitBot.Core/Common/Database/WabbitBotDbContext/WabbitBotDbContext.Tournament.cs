using System;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Entity Framework Core DbContext for WabbitBot with JSONB support
    /// </summary>
    public partial class WabbitBotDbContext : DbContext
    {
        public DbSet<Tournament> Tournaments { get; set; } = null!;
        public DbSet<TournamentStateSnapshot> TournamentStateSnapshots { get; set; } = null!;

        private void ConfigureTournament(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tournament>(entity =>
            {
                entity.ToTable("tournaments");

                // Configure JSONB columns for complex objects
                entity.Property(t => t.StateHistory)
                    .HasColumnName("state_history")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(t => t.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(t => t.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(t => t.Description)
                    .HasColumnName("description");

                entity.Property(t => t.EvenTeamFormat)
                    .HasColumnName("even_team_format");

                entity.Property(t => t.StartDate)
                    .HasColumnName("start_date");

                entity.Property(t => t.EndDate)
                    .HasColumnName("end_date");

                entity.Property(t => t.MaxParticipants)
                    .HasColumnName("max_participants");

                entity.Property(t => t.BestOf)
                    .HasColumnName("best_of");

                entity.Property(t => t.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(t => t.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(t => t.Id);
            });
        }

        private void ConfigureTournamentStateSnapshot(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TournamentStateSnapshot>(entity =>
            {
                entity.ToTable("tournament_state_snapshots");

                entity.Property(t => t.Id)
                    .HasColumnName("id");

                entity.Property(t => t.TournamentId)
                    .HasColumnName("tournament_id");

                entity.Property(t => t.Timestamp)
                    .HasColumnName("timestamp");

                entity.Property(t => t.UserId)
                    .HasColumnName("user_id")
                    .HasMaxLength(255);

                entity.Property(t => t.PlayerName)
                    .HasColumnName("player_name")
                    .HasMaxLength(255);

                entity.Property(t => t.AdditionalData)
                    .HasColumnName("additional_data")
                    .HasColumnType("jsonb");

                entity.Property(t => t.RegistrationOpenedAt)
                    .HasColumnName("registration_opened_at");

                entity.Property(t => t.StartedAt)
                    .HasColumnName("started_at");

                entity.Property(t => t.CompletedAt)
                    .HasColumnName("completed_at");

                entity.Property(t => t.CancelledAt)
                    .HasColumnName("cancelled_at");

                entity.Property(t => t.Name)
                    .HasColumnName("name")
                    .HasMaxLength(255);

                entity.Property(t => t.Description)
                    .HasColumnName("description");

                entity.Property(t => t.StartDate)
                    .HasColumnName("start_date");

                entity.Property(t => t.MaxParticipants)
                    .HasColumnName("max_participants");

                entity.Property(t => t.WinnerTeamId)
                    .HasColumnName("winner_team_id")
                    .HasMaxLength(255);

                entity.Property(t => t.CancelledByUserId)
                    .HasColumnName("cancelled_by_user_id")
                    .HasMaxLength(255);

                entity.Property(t => t.CancellationReason)
                    .HasColumnName("cancellation_reason")
                    .HasMaxLength(1000);

                entity.Property(t => t.RegisteredTeamIds)
                    .HasColumnName("registered_team_ids")
                    .HasColumnType("jsonb");

                entity.Property(t => t.ParticipantTeamIds)
                    .HasColumnName("participant_team_ids")
                    .HasColumnType("jsonb");

                entity.Property(t => t.ActiveMatchIds)
                    .HasColumnName("active_match_ids")
                    .HasColumnType("jsonb");

                entity.Property(t => t.CompletedMatchIds)
                    .HasColumnName("completed_match_ids")
                    .HasColumnType("jsonb");

                entity.Property(t => t.AllMatchIds)
                    .HasColumnName("all_match_ids")
                    .HasColumnType("jsonb");

                entity.Property(t => t.FinalRankings)
                    .HasColumnName("final_rankings")
                    .HasColumnType("jsonb");

                entity.Property(t => t.CurrentParticipantCount)
                    .HasColumnName("current_participant_count");

                entity.Property(t => t.CurrentRound)
                    .HasColumnName("current_round");

                entity.Property(t => t.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(t => t.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(t => t.Id);

                // Indexes
                entity.HasIndex(t => t.TournamentId)
                    .HasDatabaseName("idx_tournament_state_snapshots_tournament_id");

                entity.HasIndex(t => t.Timestamp)
                    .HasDatabaseName("idx_tournament_state_snapshots_timestamp");

                entity.HasIndex(t => t.WinnerTeamId)
                    .HasDatabaseName("idx_tournament_state_snapshots_winner_team_id");
            });
        }
    }
}