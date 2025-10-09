using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using WabbitBot.Generator.Shared.Analyzers;
using WabbitBot.SourceGenerators.Templates;
using WabbitBot.SourceGenerators.Utils;
using static WabbitBot.SourceGenerators.Utils.GeneratorHelpers;

namespace WabbitBot.SourceGenerators.Generators.Database
{
    /// <summary>
    /// Source generator that creates DatabaseService accessors from [EntityMetadata] attributes.
    ///
    /// Generates:
    /// - Lazy<DatabaseService<TEntity>> fields for all entities
    /// - Public DatabaseService<TEntity> properties in CoreService.Database
    /// - Proper column specifications from EntityConfig metadata
    /// </summary>
    [Generator]
    public class DatabaseServiceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            try
            {
                context.RegisterPostInitializationOutput(ctx =>
                    ctx.AddSource(
                        "__WG_Init_DatabaseServiceGenerator.g.cs",
                        "// DatabaseServiceGenerator: Initialize hit"
                    )
                );
            }
            catch (Exception ex)
            {
                context.RegisterPostInitializationOutput(ctx =>
                    ctx.AddSource(
                        "__WG_Init_DatabaseServiceGenerator_Error.g.cs",
                        "// Init exception: " + ex.Message.Replace("\"", "'")
                    )
                );
            }

            var isCoreProject = context.CompilationProvider.IsCore();

            // Find all classes with [EntityMetadata] attributes
            var entityDeclarations = context
                .SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: static (ctx, _) =>
                    {
                        var classDecl = (ClassDeclarationSyntax)ctx.Node;
                        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                        if (classSymbol == null)
                            return null;

                        var entityMetadataAttr = classSymbol
                            .GetAttributes()
                            .FirstOrDefault(attr =>
                                attr.AttributeClass?.ToDisplayString()
                                == "WabbitBot.Common.Attributes.EntityMetadataAttribute"
                            );

                        if (entityMetadataAttr != null)
                        {
                            return AttributeAnalyzer.ExtractEntityMetadataInfo(classSymbol, entityMetadataAttr);
                        }

                        return null;
                    }
                )
                .Where(static info => info != null)
                .Collect();

            // Generate database service accessors
            var serviceSource = entityDeclarations.Select(
                (entities, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var nonNull = entities.OfType<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo>();
                    return GenerateDatabaseServiceAccessors(nonNull);
                }
            );

            context.RegisterSourceOutput(
                serviceSource.Combine(isCoreProject),
                (spc, tuple) =>
                {
                    if (!tuple.Right)
                        return;
                    spc.AddSource("DatabaseServiceAccessors.g.cs", tuple.Left);
                }
            );

            // Generate optional cache provider registrations partial
            var cacheRegSource = entityDeclarations.Select(
                (entities, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var nonNull = entities.OfType<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo>();
                    return GenerateCacheRegistration(nonNull);
                }
            );

            context.RegisterSourceOutput(
                cacheRegSource.Combine(isCoreProject),
                (spc, tuple) =>
                {
                    if (!tuple.Right)
                        return;
                    spc.AddSource("CoreService.CacheRegistrations.g.cs", tuple.Left);
                }
            );

            // Generate optional archive provider registrations partial
            var archiveRegSource = entityDeclarations.Select(
                (entities, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var nonNull = entities.OfType<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo>();
                    return GenerateArchiveRegistration(nonNull);
                }
            );

            context.RegisterSourceOutput(
                archiveRegSource.Combine(isCoreProject),
                (spc, tuple) =>
                {
                    if (!tuple.Right)
                        return;
                    spc.AddSource("CoreService.ArchiveRegistrations.g.cs", tuple.Left);
                }
            );
        }

        private SourceText GenerateDatabaseServiceAccessors(
            IEnumerable<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata
        )
        {
            var sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine(
                CreateFileHeader("CoreService.Database", "Auto-generated DatabaseService accessors")
            );
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using WabbitBot.Common.Data.Service;");
            sourceBuilder.AppendLine("using WabbitBot.Common.Data;");
            sourceBuilder.AppendLine("using WabbitBot.Common.Data.Interfaces;");
            sourceBuilder.AppendLine("using WabbitBot.Common.Models;");
            sourceBuilder.AppendLine("using WabbitBot.Core.Common.Models.Common;");
            sourceBuilder.AppendLine("using WabbitBot.Core.Common.Database;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace WabbitBot.Core.Common.Services");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Database service coordination for CoreService");
            sourceBuilder.AppendLine("    /// Provides unified DatabaseService instances for all entities");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static partial class CoreService");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        #region Auto-generated DatabaseService accessors");
            sourceBuilder.AppendLine();

            // Generate lazy fields and public properties for each entity
            foreach (var metadata in entityMetadata)
            {
                // Validate metadata
                if (string.IsNullOrEmpty(metadata.TableName))
                {
                    // Skip invalid entities
                    continue;
                }

                // Validate column names
                if (metadata.SnakeCaseColumnNames == null || metadata.SnakeCaseColumnNames.Length == 0)
                {
                    // Skip entities without columns
                    continue;
                }

                var entityTypeName = GetFullEntityTypeName(metadata.ClassName, metadata.EntityType);
                string propertyName;
                if (!string.IsNullOrEmpty(metadata.ServicePropertyName))
                {
                    // Honor explicit override exactly (no further pluralization)
                    propertyName = GeneratorHelpers.NormalizeServicePropertyName(metadata.ServicePropertyName!);
                }
                else
                {
                    // Default: match class name (singular, PascalCase)
                    propertyName = GeneratorHelpers.NormalizeServicePropertyName(metadata.DefaultServicePropertyName);
                }
                var lazyFieldName = $"_lazy{propertyName}";

                // Generate lazy field
                sourceBuilder.AppendLine(
                    $"        private static readonly Lazy<DatabaseService<{entityTypeName}>> {lazyFieldName} = new(() =>"
                );
                sourceBuilder.AppendLine($"        {{");
                sourceBuilder.AppendLine($"            var service = new DatabaseService<{entityTypeName}>(");
                sourceBuilder.AppendLine($"                \"{metadata.TableName}\",");
                sourceBuilder.AppendLine(
                    $"                new[] {{ {string.Join(", ", metadata.SnakeCaseColumnNames.Select(c => $"\"{c}\""))} }},"
                );
                sourceBuilder.AppendLine($"                \"{metadata.ArchiveTableName}\",");
                sourceBuilder.AppendLine(
                    $"                new[] {{ {string.Join(", ", metadata.SnakeCaseColumnNames.Select(c => $"\"{c}\""))} }},"
                );
                sourceBuilder.AppendLine($"                {metadata.MaxCacheSize},");
                sourceBuilder.AppendLine(
                    $"                System.TimeSpan.FromMinutes({metadata.CacheExpiryMinutes}),"
                );
                sourceBuilder.AppendLine($"                \"id\")");
                sourceBuilder.AppendLine($"            ;");
                sourceBuilder.AppendLine(
                    $"            var adapter = RepositoryAdapterRegistry.GetAdapter<{entityTypeName}>();"
                );
                sourceBuilder.AppendLine(
                    $"            if (adapter is not null) service.UseRepositoryAdapter(adapter);"
                );
                sourceBuilder.AppendLine(
                    $"            var cache = CacheProviderRegistry.GetProvider<{entityTypeName}>();"
                );
                sourceBuilder.AppendLine($"            if (cache is not null) service.UseCacheProvider(cache);");
                sourceBuilder.AppendLine(
                    $"            else service.UseCacheProvider(new InMemoryLruCacheProvider<{entityTypeName}>({metadata.MaxCacheSize}, System.TimeSpan.FromMinutes({metadata.CacheExpiryMinutes}))); "
                );
                sourceBuilder.AppendLine(
                    $"            var archive = ArchiveProviderRegistry.GetProvider<{entityTypeName}>();"
                );
                sourceBuilder.AppendLine($"            if (archive is not null) service.UseArchiveProvider(archive);");
                sourceBuilder.AppendLine(
                    $"            else service.UseArchiveProvider(new NoOpArchiveProvider<{entityTypeName}>());"
                );
                sourceBuilder.AppendLine($"            return service;");
                sourceBuilder.AppendLine($"        }});");

                // Generate public property
                var xml = CommonTemplates
                    .CreateGeneratedDoc($"Gets the DatabaseService for {metadata.ClassName} entities.")
                    .Split('\n')
                    .Select(l => l.Length == 0 ? l : "        " + l)
                    .Aggregate(new StringBuilder(), (b, l) => b.AppendLine(l), b => b.ToString())
                    .TrimEnd('\r', '\n');
                sourceBuilder.AppendLine(xml);
                sourceBuilder.AppendLine(
                    $"        public static DatabaseService<{entityTypeName}> {propertyName} => {lazyFieldName}.Value;"
                );
                sourceBuilder.AppendLine();
            }

            sourceBuilder.AppendLine("        #endregion // Auto-generated DatabaseService accessors");
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            return SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);
        }

        private string GetFullEntityTypeName(string className, INamedTypeSymbol entityType)
        {
            // Handle entities that need full namespace qualification
            var specialEntities = new Dictionary<string, string>
            {
                ["Stats"] = "WabbitBot.Core.Common.Models.Common.Stats",
            };

            if (specialEntities.TryGetValue(className, out var fullName))
            {
                return fullName;
            }

            // Default: use the entity's full type name
            return entityType.ToDisplayString();
        }

        private SourceText GenerateCacheRegistration(
            IEnumerable<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata
        )
        {
            var sb = new StringBuilder();
            sb.AppendLine(CreateFileHeader("CoreService.Database", "Auto-generated cache provider registrations"));
            sb.AppendLine("using System;");
            sb.AppendLine("using WabbitBot.Common.Data;");
            sb.AppendLine("using WabbitBot.Common.Data.Service;");
            sb.AppendLine("using WabbitBot.Core.Common.Models.Common;");
            sb.AppendLine();
            sb.AppendLine("namespace WabbitBot.Core.Common.Services");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class CoreService");
            sb.AppendLine("    {");
            sb.AppendLine("        static partial void RegisterCacheProviders_Generated()");
            sb.AppendLine("        {");

            foreach (var metadata in entityMetadata)
            {
                if (!metadata.EmitCacheRegistration)
                    continue;

                var entityTypeName = GetFullEntityTypeName(metadata.ClassName, metadata.EntityType);
                sb.AppendLine(
                    $"            CacheProviderRegistry.RegisterProvider<{entityTypeName}>(new InMemoryLruCacheProvider<{entityTypeName}>({metadata.MaxCacheSize}, System.TimeSpan.FromMinutes({metadata.CacheExpiryMinutes}))); "
                );
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return SourceText.From(sb.ToString(), Encoding.UTF8);
        }

        private SourceText GenerateArchiveRegistration(
            IEnumerable<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata
        )
        {
            var sb = new StringBuilder();
            sb.AppendLine(CreateFileHeader("CoreService.Database", "Auto-generated archive provider registrations"));
            sb.AppendLine("using System;");
            sb.AppendLine("using WabbitBot.Common.Data;");
            sb.AppendLine("using WabbitBot.Core.Common.Models.Common;");
            sb.AppendLine("using WabbitBot.Core.Common.Database;");
            sb.AppendLine();
            sb.AppendLine("namespace WabbitBot.Core.Common.Services");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class CoreService");
            sb.AppendLine("    {");
            sb.AppendLine("        static partial void RegisterArchiveProviders_Generated()");
            sb.AppendLine("        {");

            foreach (var metadata in entityMetadata)
            {
                if (!metadata.EmitArchiveRegistration)
                    continue;

                var entityTypeName = GetFullEntityTypeName(metadata.ClassName, metadata.EntityType);
                sb.AppendLine(
                    $"            ArchiveProviderRegistry.RegisterProvider<{entityTypeName}>(new EfArchiveProvider<{entityTypeName}>());"
                );
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return SourceText.From(sb.ToString(), Encoding.UTF8);
        }

        private string CreateFileHeader(string generatorName, string description)
        {
            return $$"""
                // <auto-generated>
                // This file was generated by {{generatorName}}
                // {{description}}
                // Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
                // </auto-generated>

                """;
        }
    }
}
