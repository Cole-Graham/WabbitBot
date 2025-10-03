using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WabbitBot.Generator.Shared.Analyzers;
using WabbitBot.SourceGenerators.Templates;
using WabbitBot.SourceGenerators.Utils;
namespace WabbitBot.SourceGenerators.Generators.Database
{
    /// <summary>
    /// Source generator that creates entity metadata constants from [EntityMetadata] attributes.
    /// Generates static configuration classes with table names, column names, and other metadata.
    /// </summary>
    [Generator]
    public class EntityMetadataGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            try
            {
                context.RegisterPostInitializationOutput(ctx => ctx.AddSource("__WG_Init_EntityMetadataGenerator.g.cs", "// EntityMetadataGenerator: Initialize hit"));
            }
            catch (Exception ex)
            {
                context.RegisterPostInitializationOutput(ctx => ctx.AddSource("__WG_Init_EntityMetadataGenerator_Error.g.cs", "// Init exception: " + ex.Message.Replace("\"", "'")));
            }

            var isCoreProject = context.CompilationProvider.IsCore();

            // Extract entity metadata per attributed class, then aggregate to a single emission
            var entityInfos = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: static (ctx, _) =>
                    {
                        var classDecl = (Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax)ctx.Node;
                        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                        if (classSymbol is null)
                            return null;

                        var attr = classSymbol.GetAttributes()
                            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == WabbitBot.Generator.Shared.AttributeNames.EntityMetadata);
                        return attr is null ? null : AttributeAnalyzer.ExtractEntityMetadataInfo(classSymbol, attr);
                    })
                .Where(static info => info is not null)
                .Collect();

            var entityConfigSource = entityInfos.Select((entities, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return GenerateEntityMetadataConfiguration(entities.OfType<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo>());
            });

            context.RegisterSourceOutput(entityConfigSource.Combine(isCoreProject), (spc, tuple) =>
            {
                if (!tuple.Right)
                    return;
                spc.AddSource("EntityMetadata.Generated.g.cs", tuple.Left);
            });

            // Generate DbConfig classes and the EntityConfigFactory for Core only
            var dbConfigsSource = entityInfos.Select((entities, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                var nonNull = entities.OfType<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo>();
                return GenerateEntityDbConfigs(nonNull);
            });

            var factorySource = entityInfos.Select((entities, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                var nonNull = entities.OfType<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo>();
                return GenerateEntityConfigFactory(nonNull);
            });

            context.RegisterSourceOutput(dbConfigsSource.Combine(isCoreProject), (spc, tuple) =>
            {
                if (!tuple.Right)
                    return;
                spc.AddSource("EntityDbConfigs.Generated.g.cs", tuple.Left);
            });

            context.RegisterSourceOutput(factorySource.Combine(isCoreProject), (spc, tuple) =>
            {
                if (!tuple.Right)
                    return;
                spc.AddSource("EntityConfigFactory.Generated.g.cs", tuple.Left);
            });
        }

        private SourceText GenerateEntityMetadataConfiguration(
            IEnumerable<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata)
        {
            var sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine(CreateFileHeader("EntityMetadataGenerator", "Auto-generated entity metadata configuration"));
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Collections.Generic;");
            sourceBuilder.AppendLine("using WabbitBot.Common.Models;");
            sourceBuilder.AppendLine("using WabbitBot.Core.Common.Models.Common;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace WabbitBot.Core.Common.Metadata");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Auto-generated entity metadata configuration");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static class EntityMetadataConfig");
            sourceBuilder.AppendLine("    {");

            // Generate entity metadata constants
            foreach (var metadata in entityMetadata)
            {
                sourceBuilder.AppendLine(CommonTemplates.CreateGeneratedDoc($"Metadata for {metadata.ClassName} entity."));
                sourceBuilder.AppendLine($"        public const string {metadata.ClassName}TableName = \"{metadata.TableName}\";");
                sourceBuilder.AppendLine($"        public const string {metadata.ClassName}ArchiveTableName = \"{metadata.ArchiveTableName}\";");
                sourceBuilder.AppendLine($"        public static readonly string[] {metadata.ClassName}Columns = new[] {{ {string.Join(", ", metadata.SnakeCaseColumnNames.Select(c => $"\"{c}\""))} }};");
                sourceBuilder.AppendLine();
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            return SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);
        }

        private SourceText GenerateEntityDbConfigs(
            IEnumerable<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CreateFileHeader("EntityMetadataGenerator", "Auto-generated DbConfig classes"));
            sb.AppendLine("using System;");
            sb.AppendLine("using WabbitBot.Core.Common.Config;");
            sb.AppendLine("using WabbitBot.Common.Models;");
            sb.AppendLine("using WabbitBot.Core.Common.Models.Common;");
            sb.AppendLine();
            sb.AppendLine("namespace WabbitBot.Core.Common.Config");
            sb.AppendLine("{");

            foreach (var m in entityMetadata)
            {
                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Auto-generated configuration for {m.ClassName} entities");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    public sealed class {m.ClassName}DbConfig : EntityConfig<{m.EntityType.ToDisplayString()}>, IEntityConfig");
                sb.AppendLine("    {");
                sb.AppendLine($"        public {m.ClassName}DbConfig() : base(");
                sb.AppendLine($"            tableName: \"{m.TableName}\",");
                sb.AppendLine($"            archiveTableName: \"{m.ArchiveTableName}\",");
                sb.AppendLine($"            columns: new[] {{ {string.Join(", ", m.SnakeCaseColumnNames.Select(c => $"\"{c}\""))} }},");
                sb.AppendLine($"            idColumn: \"id\",");
                sb.AppendLine($"            maxCacheSize: {m.MaxCacheSize},");
                sb.AppendLine($"            defaultCacheExpiry: TimeSpan.FromMinutes({m.CacheExpiryMinutes})");
                sb.AppendLine("        )");
                sb.AppendLine("        {");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            return SourceText.From(sb.ToString(), Encoding.UTF8);
        }

        private SourceText GenerateEntityConfigFactory(
            IEnumerable<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CreateFileHeader("EntityMetadataGenerator", "Auto-generated entity configuration factory"));
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using WabbitBot.Core.Common.Config;");
            sb.AppendLine();
            sb.AppendLine("namespace WabbitBot.Core.Common.Config");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Auto-generated extension providing access to entity configurations.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static partial class EntityConfigFactory");
            sb.AppendLine("    {");

            foreach (var m in entityMetadata)
            {
                var configClassName = m.ClassName + "DbConfig";
                var lazyField = "_" + char.ToLowerInvariant(m.ClassName[0]) + m.ClassName.Substring(1) + "DbConfig";
                sb.AppendLine($"        private static readonly Lazy<{configClassName}> {lazyField} = new(() => new {configClassName}());");
                sb.AppendLine($"        public static {configClassName} {m.ClassName} => {lazyField}.Value;");
                sb.AppendLine();
            }

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Gets all entity configurations.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static IEnumerable<IEntityConfig> GetAllConfigurations()");
            sb.AppendLine("        {");
            foreach (var m in entityMetadata)
            {
                sb.AppendLine($"            yield return {m.ClassName};");
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
