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
        public DbSet<Scrimmage> Scrimmages { get; set; } = null!;
        public DbSet<ProvenPotentialRecord> ProvenPotentialRecords { get; set; } = null!;

        private void ConfigureScrimmage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Scrimmage>(entity =>
            {
                entity.ToTable("scrimmages");

                // Configure JSONB columns for complex objects
                entity.Property(s => s.Team1RosterIds)
                    .HasColumnName("team1_roster_ids")
                    .HasColumnType("jsonb");

                entity.Property(s => s.Team2RosterIds)
                    .HasColumnName("team2_roster_ids")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(s => s.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(s => s.Team1Id)
                    .HasColumnName("team1_id");

                entity.Property(s => s.Team2Id)
                    .HasColumnName("team2_id");

                entity.Property(s => s.TeamSize)
                    .HasColumnName("even_team_format");

                entity.Property(s => s.StartedAt)
                    .HasColumnName("started_at");

                entity.Property(s => s.CompletedAt)
                    .HasColumnName("completed_at");

                entity.Property(s => s.WinnerId)
                    .HasColumnName("winner_id");

                // TODO: Status was moved to ScrimmageStateSnapshot
                // entity.Property(s => s.Status)
                //     .HasColumnName("status");

                entity.Property(s => s.Team1Rating)
                    .HasColumnName("team1_rating");

                entity.Property(s => s.Team2Rating)
                    .HasColumnName("team2_rating");

                entity.Property(s => s.Team1RatingChange)
                    .HasColumnName("team1_rating_change");

                entity.Property(s => s.Team2RatingChange)
                    .HasColumnName("team2_rating_change");

                entity.Property(s => s.Team1Confidence)
                    .HasColumnName("team1_confidence");

                entity.Property(s => s.Team2Confidence)
                    .HasColumnName("team2_confidence");

                entity.Property(s => s.Team1Score)
                    .HasColumnName("team1_score");

                entity.Property(s => s.Team2Score)
                    .HasColumnName("team2_score");

                entity.Property(s => s.ChallengeExpiresAt)
                    .HasColumnName("challenge_expires_at");

                entity.Property(s => s.IsAccepted)
                    .HasColumnName("is_accepted");

                entity.Property(s => s.BestOf)
                    .HasColumnName("best_of");

                entity.Property(s => s.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(s => s.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(s => s.Id);

                // Foreign key relationships
                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(s => s.Team1Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(s => s.Team2Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(s => s.WinnerId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureProvenPotentialRecord(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProvenPotentialRecord>(entity =>
            {
                entity.ToTable("proven_potential_records");

                // Configure JSONB columns for complex objects
                entity.Property(ppr => ppr.AppliedThresholds)
                    .HasColumnName("applied_thresholds")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(ppr => ppr.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(ppr => ppr.OriginalMatchId)
                    .HasColumnName("original_match_id");

                entity.Property(ppr => ppr.ChallengerId)
                    .HasColumnName("challenger_id");

                entity.Property(ppr => ppr.OpponentId)
                    .HasColumnName("opponent_id");

                entity.Property(ppr => ppr.ChallengerRating)
                    .HasColumnName("challenger_rating");

                entity.Property(ppr => ppr.OpponentRating)
                    .HasColumnName("opponent_rating");

                entity.Property(ppr => ppr.ChallengerConfidence)
                    .HasColumnName("challenger_confidence");

                entity.Property(ppr => ppr.OpponentConfidence)
                    .HasColumnName("opponent_confidence");

                entity.Property(ppr => ppr.ChallengerOriginalRatingChange)
                    .HasColumnName("challenger_original_rating_change");

                entity.Property(ppr => ppr.OpponentOriginalRatingChange)
                    .HasColumnName("opponent_original_rating_change");

                entity.Property(ppr => ppr.RatingAdjustment)
                    .HasColumnName("rating_adjustment");

                entity.Property(ppr => ppr.TeamSize)
                    .HasColumnName("game_size");

                entity.Property(ppr => ppr.LastCheckedAt)
                    .HasColumnName("last_checked_at");

                entity.Property(ppr => ppr.IsComplete)
                    .HasColumnName("is_complete");

                entity.Property(ppr => ppr.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(ppr => ppr.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(ppr => ppr.Id);

                // Foreign key relationships
                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(ppr => ppr.ChallengerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Team>()
                    .WithMany()
                    .HasForeignKey(ppr => ppr.OpponentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}