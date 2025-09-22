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

                // Primary key
                entity.HasKey(t => t.Id);

                // Foreign key relationships
                entity.HasOne<Player>()
                    .WithMany()
                    .HasForeignKey(t => t.TeamCaptainId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

    }
}