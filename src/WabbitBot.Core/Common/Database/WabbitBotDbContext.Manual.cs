using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Manual partial class for WabbitBotDbContext.
    /// Use this for infrastructure entities that don't use source generation.
    /// </summary>
    public partial class WabbitBotDbContext
    {
        /// <summary>
        /// Schema metadata for tracking database version history.
        /// This is manually configured since it's stable infrastructure.
        /// </summary>
        public DbSet<SchemaMetadata> SchemaMetadata { get; set; } = null!;

        /// <summary>
        /// Configure SchemaMetadata entity manually.
        /// This is a stable infrastructure table that rarely changes.
        /// Note: This method is called by the generated OnModelCreating.
        /// </summary>
        private void ConfigureSchemaMetadata(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SchemaMetadata>(entity =>
            {
                entity.ToTable("schema_metadata");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.SchemaVersion).HasColumnName("schema_version").HasMaxLength(50).IsRequired();

                entity.Property(e => e.AppliedAt).HasColumnName("applied_at").IsRequired();

                entity.Property(e => e.AppliedBy).HasColumnName("applied_by").HasMaxLength(255);

                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);

                entity.Property(e => e.IsBreakingChange).HasColumnName("is_breaking_change").IsRequired();

                entity.Property(e => e.CompatibilityNotes).HasColumnName("compatibility_notes").HasMaxLength(1000);

                entity.Property(e => e.MigrationName).HasColumnName("migration_name").HasMaxLength(255);

                // Indexes
                entity.HasIndex(e => e.SchemaVersion).HasDatabaseName("ix_schema_metadata_schema_version");

                entity.HasIndex(e => e.AppliedAt).HasDatabaseName("ix_schema_metadata_applied_at");

                entity.HasIndex(e => e.MigrationName).HasDatabaseName("ix_schema_metadata_migration_name");
            });
        }

        /// <summary>
        /// Configure Match entity navigation properties manually.
        /// Match has two foreign keys to Team (Team1 and Team2), which requires explicit configuration.
        /// Note: This is called after the generated ConfigureMatch method.
        /// </summary>
        partial void ConfigureMatchRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Match>(entity =>
            {
                // Configure Team1 navigation
                entity
                    .HasOne(m => m.Team1)
                    .WithMany(t => t.Matches)
                    .HasForeignKey(m => m.Team1Id)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure Team2 navigation
                entity.HasOne(m => m.Team2).WithMany().HasForeignKey(m => m.Team2Id).OnDelete(DeleteBehavior.Restrict);
            });
        }

        /// <summary>
        /// Configure Player and MashinaUser one-to-one relationship.
        /// Player is the dependent side (has the foreign key MashinaUserId).
        /// </summary>
        partial void ConfigurePlayerRelationships(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>(entity =>
            {
                entity
                    .HasOne(p => p.MashinaUser)
                    .WithOne(m => m.Player)
                    .HasForeignKey<Player>(p => p.MashinaUserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        /// <summary>
        /// Enable lazy loading for all entities.
        /// This allows navigation properties to be loaded automatically when accessed.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            // Enable lazy loading
            optionsBuilder.UseLazyLoadingProxies();
        }
    }
}
