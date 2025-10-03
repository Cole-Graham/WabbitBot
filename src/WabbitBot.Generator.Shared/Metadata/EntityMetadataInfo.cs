using Microsoft.CodeAnalysis;

namespace WabbitBot.Generator.Shared.Metadata;

/// <summary>
/// Lightweight, immutable data model for parsed metadata from [EntityMetadata] attributes.
/// Used as a shared container for generators and analyzers to avoid duplicating parsing logic.
/// </summary>
public record EntityMetadataInfo(
    string ClassName,
    string TableName,
    string ArchiveTableName,
    int MaxCacheSize = 1000,
    int CacheExpiryMinutes = 60,
    bool EmitCacheRegistration = false,
    bool EmitArchiveRegistration = false,
    string[] ColumnNames = null!,
    INamedTypeSymbol EntityType = null!,
    string? ServicePropertyName = null)
{
    // Computed property for snake_case column names (common in databases)
    public string[] SnakeCaseColumnNames => ColumnNames.Select(ToSnakeCase).ToArray();
    public string DefaultServicePropertyName => ClassName; // singular, PascalCase

    private static string ToSnakeCase(string pascalCase)
    {
        return string.Concat(pascalCase.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}