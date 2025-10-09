using System;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Config
{
    /// <summary>
    /// Base class for entity configurations that define database mappings and settings
    /// </summary>
    public abstract class EntityConfig<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Gets the table name for this entity
        /// </summary>
        public string TableName { get; protected set; }

        /// <summary>
        /// Gets the archive table name for this entity
        /// </summary>
        public string ArchiveTableName { get; protected set; }

        /// <summary>
        /// Gets the column names for this entity
        /// </summary>
        public string[] Columns { get; protected set; }

        /// <summary>
        /// Gets the ID column name
        /// </summary>
        public string IdColumn { get; protected set; } = "id";

        /// <summary>
        /// Gets the maximum cache size for this entity
        /// </summary>
        public int MaxCacheSize { get; protected set; } = 1000;

        /// <summary>
        /// Gets the default cache expiry time
        /// </summary>
        public TimeSpan DefaultCacheExpiry { get; protected set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets the JSONB column names for this entity
        /// </summary>
        public string[] JsonbColumns { get; protected set; } = Array.Empty<string>();

        protected EntityConfig(
            string tableName,
            string archiveTableName,
            string[] columns,
            string idColumn = "id",
            int maxCacheSize = 1000,
            TimeSpan? defaultCacheExpiry = null,
            string[]? jsonbColumns = null
        )
        {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            ArchiveTableName = archiveTableName ?? throw new ArgumentNullException(nameof(archiveTableName));
            Columns = columns ?? throw new ArgumentNullException(nameof(columns));
            IdColumn = idColumn ?? throw new ArgumentNullException(nameof(idColumn));
            MaxCacheSize = maxCacheSize;
            DefaultCacheExpiry = defaultCacheExpiry ?? TimeSpan.FromHours(1);
            JsonbColumns = jsonbColumns ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// Factory class for creating and accessing entity configurations
    /// </summary>
    public static partial class EntityConfigFactory
    {
        // Generated members are provided by EntityMetadataGenerator at compile time.
    }

    /// <summary>
    /// Base interface for entity configurations
    /// </summary>
    public interface IEntityConfig
    {
        string TableName { get; }
        string ArchiveTableName { get; }
        string[] Columns { get; }
        string IdColumn { get; }
        int MaxCacheSize { get; }
        TimeSpan DefaultCacheExpiry { get; }
    }
}
