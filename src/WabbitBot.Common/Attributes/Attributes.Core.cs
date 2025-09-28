using System;

namespace WabbitBot.Common.Attributes
{
    #region Database/Entity
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityMetadataAttribute(
        string? tableName = null,
        string? archiveTableName = null,
        string idColumn = "id",
        int maxCacheSize = 1000,
        int cacheExpiryMinutes = 60,
        string[]? explicitColumns = null,
        string[]? explicitJsonbColumns = null,
        string[]? explicitIndexedColumns = null,
        string? servicePropertyName = null) : Attribute
    {
        public string? TableName { get; } = tableName;
        public string? ArchiveTableName { get; } = archiveTableName;
        public string IdColumn { get; } = idColumn;
        public int MaxCacheSize { get; } = maxCacheSize;
        public int CacheExpiryMinutes { get; } = cacheExpiryMinutes;
        public string? ServicePropertyName { get; } = servicePropertyName;

        // Optional explicit overrides - generator will auto-detect most things
        public string[]? ExplicitColumns { get; } = explicitColumns;
        public string[]? ExplicitJsonbColumns { get; } = explicitJsonbColumns;
        public string[]? ExplicitIndexedColumns { get; } = explicitIndexedColumns;
    }
    #endregion
}