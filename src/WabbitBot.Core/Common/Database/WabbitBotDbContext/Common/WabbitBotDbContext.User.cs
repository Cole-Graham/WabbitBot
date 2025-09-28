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
        public DbSet<User> Users { get; set; } = null!;

        private void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                // Standard columns
                entity.Property(u => u.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(u => u.DiscordId)
                    .HasColumnName("discord_id")
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(u => u.Username)
                    .HasColumnName("username")
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(u => u.Nickname)
                    .HasColumnName("nickname")
                    .HasMaxLength(255);

                entity.Property(u => u.AvatarUrl)
                    .HasColumnName("avatar_url");

                entity.Property(u => u.JoinedAt)
                    .HasColumnName("joined_at");

                entity.Property(u => u.LastActive)
                    .HasColumnName("last_active");

                entity.Property(u => u.IsActive)
                    .HasColumnName("is_active")
                    .HasDefaultValue(true);

                entity.Property(u => u.PlayerId)
                    .HasColumnName("player_id")
                    .HasMaxLength(255);

                entity.Property(u => u.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(u => u.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(u => u.Id);

                // Foreign key relationships
                entity.HasOne<Player>()
                    .WithMany()
                    .HasForeignKey(u => u.PlayerId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(u => u.DiscordId)
                    .IsUnique()
                    .HasDatabaseName("idx_users_discord_id");

                entity.HasIndex(u => u.Username)
                    .HasDatabaseName("idx_users_username");

                entity.HasIndex(u => u.IsActive)
                    .HasDatabaseName("idx_users_is_active");

                entity.HasIndex(u => u.PlayerId)
                    .HasDatabaseName("idx_users_player_id");

                entity.HasIndex(u => u.LastActive)
                    .HasDatabaseName("idx_users_last_active");
            });
        }
    }
}