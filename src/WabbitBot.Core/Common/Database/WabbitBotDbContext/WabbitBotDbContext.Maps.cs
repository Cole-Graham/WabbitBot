using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Entity Framework Core DbContext for WabbitBot with JSONB support
    /// </summary>
    public partial class WabbitBotDbContext : DbContext
    {
        public DbSet<Map> Maps { get; set; } = null!;

        private void ConfigureMap(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Map>(entity =>
            {
                entity.ToTable("maps");

                // Standard columns
                entity.Property(m => m.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(m => m.Name)
                    .HasColumnName("name")
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(m => m.Description)
                    .HasColumnName("description");

                entity.Property(m => m.IsActive)
                    .HasColumnName("is_active")
                    .HasDefaultValue(true);

                entity.Property(m => m.Size)
                    .HasColumnName("size")
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(m => m.IsInRandomPool)
                    .HasColumnName("is_in_random_pool");

                entity.Property(m => m.IsInTournamentPool)
                    .HasColumnName("is_in_tournament_pool");

                entity.Property(m => m.ThumbnailFilename)
                    .HasColumnName("thumbnail_filename")
                    .HasMaxLength(255);

                entity.Property(m => m.CreatedAt)
                    .HasColumnName("created_at");

                entity.Property(m => m.UpdatedAt)
                    .HasColumnName("updated_at");

                // Primary key
                entity.HasKey(m => m.Id);

                // Indexes
                entity.HasIndex(m => m.Name)
                    .HasDatabaseName("idx_maps_name");

                entity.HasIndex(m => m.Size)
                    .HasDatabaseName("idx_maps_size");

                entity.HasIndex(m => m.IsActive)
                    .HasDatabaseName("idx_maps_is_active");
            });
        }
    }
}