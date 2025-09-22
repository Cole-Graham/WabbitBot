using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Entity Framework Core DbContext for WabbitBot with JSONB support
    /// </summary>
    public partial class WabbitBotDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; } = null!;

        private void ConfigurePlayer(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>(entity =>
            {
                entity.ToTable("players");

                // Configure JSONB columns for complex objects
                entity.Property(p => p.TeamIds)
                    .HasColumnName("team_ids")
                    .HasColumnType("jsonb");

                entity.Property(p => p.PreviousUserIds)
                    .HasColumnName("previous_user_ids")
                    .HasColumnType("jsonb");

                entity.Property(p => p.GameUsername)
                    .HasColumnName("game_username")
                    .HasMaxLength(255);

                entity.Property(p => p.PreviousGameUsernames)
                    .HasColumnName("previous_game_usernames")
                    .HasColumnType("jsonb");

                // Standard columns
                entity.Property(p => p.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(p => p.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(p => p.LastActive)
                    .HasColumnName("last_active");

                entity.Property(p => p.IsArchived)
                    .HasColumnName("is_archived")
                    .HasDefaultValue(false);

                entity.Property(p => p.ArchivedAt)
                    .HasColumnName("archived_at");

                entity.Property(p => p.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(p => p.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(p => p.Id);
            });
        }
    }
}