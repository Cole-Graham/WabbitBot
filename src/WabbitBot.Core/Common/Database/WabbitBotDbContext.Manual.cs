using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models;

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
    }
}
