using System;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Tracks database schema version history and compatibility information.
    /// Used to monitor version drift and ensure application-database compatibility.
    ///
    /// NOTE: This is infrastructure metadata, not a business domain entity.
    /// It does not inherit from Entity to avoid redundant fields (CreatedAt/UpdatedAt).
    /// DbContext configuration is manual since this table is stable and rarely modified.
    /// </summary>
    public class SchemaMetadata
    {
        /// <summary>
        /// Unique identifier for this metadata record
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Schema version identifier (e.g., "001-1.0", "002-1.5")
        /// </summary>
        public string SchemaVersion { get; set; } = string.Empty;

        /// <summary>
        /// When this schema version was applied to the database
        /// </summary>
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who or what applied this migration (e.g., "AutoMigration", "Admin", "Deployment")
        /// </summary>
        public string? AppliedBy { get; set; }

        /// <summary>
        /// Human-readable description of what changed in this version
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether this schema change breaks compatibility with older application versions
        /// </summary>
        public bool IsBreakingChange { get; set; }

        /// <summary>
        /// Notes about version compatibility (e.g., "Compatible with app versions 1.1.0+")
        /// </summary>
        public string? CompatibilityNotes { get; set; }

        /// <summary>
        /// Name of the EF Core migration that implements this schema version
        /// </summary>
        public string? MigrationName { get; set; }
    }
}
