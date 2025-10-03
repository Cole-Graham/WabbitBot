using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WabbitBot.Generator.Shared.Metadata;

namespace WabbitBot.Generator.Shared.Analyzers;

/// <summary>
/// Utility class for analyzing attributes on symbols.
/// </summary>
public static class AttributeAnalyzer
{
    /// <summary>
    /// Extracts command information from a WabbitCommand-attributed class.
    /// </summary>
    public static CommandInfo? ExtractCommandInfo(INamedTypeSymbol classSymbol)
    {
        var attribute = classSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == AttributeNames.WabbitCommand);

        if (attribute == null)
            return null;

        var name = attribute.ConstructorArguments.FirstOrDefault().Value as string;
        var group = attribute.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "Group")
            .Value.Value as string;

        return new CommandInfo(classSymbol.Name, name ?? classSymbol.Name.ToLower(), group);
    }

    /// <summary>
    /// Checks if a class has the specified attribute.
    /// </summary>
    public static bool HasAttribute(INamedTypeSymbol classSymbol, string attributeName)
    {
        return classSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == attributeName);
    }

    /// <summary>
    /// Gets all methods in a class that should trigger event generation.
    /// </summary>
    public static IEnumerable<IMethodSymbol> GetEventTriggeringMethods(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(method => method.DeclaredAccessibility == Accessibility.Public &&
                           !method.IsStatic &&
                           method.MethodKind == MethodKind.Ordinary);
    }

    /// <summary>
    /// Extracts EventBoundaryInfo from a class symbol with EventBoundary attribute.
    /// </summary>
    public static EventBoundaryInfo? ExtractEventBoundaryInfo(INamedTypeSymbol classSymbol)
    {
        var attribute = classSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == AttributeNames.EventBoundary);

        if (attribute == null)
            return null;

        var generateRequestResponse = attribute.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "GenerateRequestResponse")
            .Value.Value as bool? ?? false;

        var targetProjects = attribute.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "TargetProjects")
            .Value.Value as string;

        // Infer BusType from EventType attribute or namespace
        var busType = InferenceHelpers.InferBusType(classSymbol);

        return new EventBoundaryInfo(
            ClassName: classSymbol.Name,
            GenerateRequestResponse: generateRequestResponse,
            BusType: busType,
            TargetProjects: targetProjects);
    }

    /// <summary>
    /// Extracts all metadata from the compilation.
    /// </summary>
    public static CompilationAnalysisResult ExtractAll(Compilation compilation)
    {
        var entities = ExtractEntityMetadata(compilation);
        // Add other extractions here as needed
        return new CompilationAnalysisResult(entities);
    }

    /// <summary>
    /// Extracts entity metadata from all classes with [EntityMetadata] attributes.
    /// </summary>
    private static IEnumerable<EntityMetadataInfo> ExtractEntityMetadata(Compilation compilation)
    {
        var entityMetadata = new List<EntityMetadataInfo>();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var classDeclarations = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (classSymbol == null) continue;

                var entityMetadataAttr = classSymbol.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == AttributeNames.EntityMetadata);

                if (entityMetadataAttr != null)
                {
                    var metadata = ExtractEntityMetadataInfo(classSymbol, entityMetadataAttr);
                    if (metadata != null)
                    {
                        entityMetadata.Add(metadata);
                    }
                }
            }
        }

        return entityMetadata;
    }

    /// <summary>
    /// Extracts entity metadata info from a single entity class.
    /// </summary>
    public static EntityMetadataInfo? ExtractEntityMetadataInfo(INamedTypeSymbol classSymbol, AttributeData attribute)
    {
        try
        {
            // Extract table name from constructor arguments or named arguments
            var tableName = attribute.ConstructorArguments.FirstOrDefault().Value as string ??
                           attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "TableName").Value.Value as string ??
                           classSymbol.Name.ToLower();

            var archiveTableName = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ArchiveTableName").Value.Value as string ??
                                  $"{tableName}_archive";

            var maxCacheSize = (int)(attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "MaxCacheSize").Value.Value ?? 1000);
            var cacheExpiryMinutes = (int)(attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "CacheExpiryMinutes").Value.Value ?? 60);
            bool emitCacheRegistration = false;
            bool emitArchiveRegistration = false;
            var ctorArgs = attribute.ConstructorArguments;
            // Constructor parameters order:
            // 0: tableName, 1: archiveTableName, 2: idColumn, 3: maxCacheSize, 4: cacheExpiryMinutes,
            // 5: explicitColumns, 6: explicitJsonbColumns, 7: explicitIndexedColumns, 8: servicePropertyName,
            // 9: emitCacheRegistration, 10: emitArchiveRegistration
            if (ctorArgs.Length >= 10 && ctorArgs[9].Value is bool c)
                emitCacheRegistration = c;
            if (ctorArgs.Length >= 11 && ctorArgs[10].Value is bool a)
                emitArchiveRegistration = a;
            // Fallback to named args if ever exposed as settable properties in future
            if (!emitCacheRegistration)
            {
                emitCacheRegistration = (bool)(attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "EmitCacheRegistration").Value.Value ?? false);
            }
            if (!emitArchiveRegistration)
            {
                emitArchiveRegistration = (bool)(attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "EmitArchiveRegistration").Value.Value ?? false);
            }

            // Extract column names from public properties
            var columnNames = classSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(prop => prop.DeclaredAccessibility == Accessibility.Public && !prop.IsStatic)
                .Select(prop => prop.Name)
                .ToArray();

            // Optional service property override
            var servicePropertyName = attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == "ServicePropertyName").Value.Value as string;
            if (string.IsNullOrEmpty(servicePropertyName))
            {
                if (ctorArgs.Length >= 9 && ctorArgs[8].Value is string s && !string.IsNullOrWhiteSpace(s))
                {
                    servicePropertyName = s;
                }
            }

            return new EntityMetadataInfo(
                ClassName: classSymbol.Name,
                TableName: tableName,
                ArchiveTableName: archiveTableName,
                MaxCacheSize: maxCacheSize,
                CacheExpiryMinutes: cacheExpiryMinutes,
                EmitCacheRegistration: emitCacheRegistration,
                EmitArchiveRegistration: emitArchiveRegistration,
                ColumnNames: columnNames,
                EntityType: classSymbol,
                ServicePropertyName: servicePropertyName);
        }
        catch
        {
            // Return null if extraction fails
            return null;
        }
    }
}

/// <summary>
/// Information about a command class.
/// </summary>
public class CommandInfo
{
    public string ClassName { get; }
    public string CommandName { get; }
    public string? Group { get; } = null;

    public CommandInfo(string className, string commandName, string? group = null)
    {
        ClassName = className;
        CommandName = commandName;
        Group = group;
    }
}

/// <summary>
/// Result of analyzing the entire compilation.
/// </summary>
public class CompilationAnalysisResult
{
    public IEnumerable<EntityMetadataInfo> Entities { get; }

    public CompilationAnalysisResult(IEnumerable<EntityMetadataInfo> entities)
    {
        Entities = entities;
    }
}
