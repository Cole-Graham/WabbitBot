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
        public DbSet<Team> Teams { get; set; } = null!;
        public DbSet<TeamVarietyStats> TeamVarietyStats { get; set; } = null!;

        private void ConfigureTeam(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Team>(entity =>
            {
                entity.ToTable("teams");

                // Configure JSONB columns for complex objects
                entity.Property(t => t.Roster)
                    .HasColumnName("roster")
                    .HasColumnType("jsonb");

                entity.Property(t => t.Stats)
                    .HasColumnName("stats")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(t => t.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(t => t.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(t => t.TeamCaptainId)
                    .HasColumnName("team_captain_id");

                entity.Property(t => t.TeamSize)
                    .HasColumnName("team_size");

                entity.Property(t => t.MaxRosterSize)
                    .HasColumnName("max_roster_size");

                entity.Property(t => t.LastActive)
                    .HasColumnName("last_active");

                entity.Property(t => t.IsArchived)
                    .HasColumnName("is_archived")
                    .HasDefaultValue(false);

                entity.Property(t => t.ArchivedAt)
                    .HasColumnName("archived_at");

                entity.Property(t => t.Tag)
                    .HasColumnName("tag")
                    .HasMaxLength(50);

                entity.Property(t => t.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(t => t.UpdatedAt)
                    .HasColumnName("updated_at");

                // Ignore navigation convenience properties
                entity.Ignore(t => t.VarietyStats);

                // Primary key
                entity.HasKey(t => t.Id);

                // Foreign key relationships
                entity.HasOne<Player>()
                    .WithMany()
                    .HasForeignKey(t => t.TeamCaptainId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureTeamVarietyStats(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamVarietyStats>(entity =>
            {
                entity.ToTable("team_variety_stats");

                entity.Property(tvs => tvs.Id)
                    .HasColumnName("id");

                entity.Property(tvs => tvs.TeamId)
                    .HasColumnName("team_id");

                entity.Property(tvs => tvs.TeamSize)
                    .HasColumnName("even_team_format");

                entity.Property(tvs => tvs.VarietyEntropy)
                    .HasColumnName("variety_entropy")
                    .HasColumnType("double precision");

                entity.Property(tvs => tvs.VarietyBonus)
                    .HasColumnName("variety_bonus")
                    .HasColumnType("double precision");

                entity.Property(tvs => tvs.TotalOpponents)
                    .HasColumnName("total_opponents");

                entity.Property(tvs => tvs.UniqueOpponents)
                    .HasColumnName("unique_opponents");

                entity.Property(tvs => tvs.LastCalculated)
                    .HasColumnName("last_calculated");

                entity.Property(tvs => tvs.LastUpdated)
                    .HasColumnName("last_updated");

                entity.Property(tvs => tvs.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(tvs => tvs.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(tvs => tvs.Id);

                // Foreign key relationship
                entity.HasOne(tvs => tvs.Team)
                    .WithMany()
                    .HasForeignKey(tvs => tvs.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint to ensure one stats record per team/team_size
                entity.HasIndex(tvs => new { tvs.TeamId, tvs.TeamSize })
                    .IsUnique()
                    .HasDatabaseName("uk_team_variety_stats_team_format");

                // Indexes
                entity.HasIndex(tvs => tvs.TeamId)
                    .HasDatabaseName("idx_team_variety_stats_team_id");

                entity.HasIndex(tvs => tvs.TeamSize)
                    .HasDatabaseName("idx_team_variety_stats_even_team_format");

                entity.HasIndex(tvs => tvs.LastUpdated)
                    .HasDatabaseName("idx_team_variety_stats_last_updated");
            });
        }

    }
}