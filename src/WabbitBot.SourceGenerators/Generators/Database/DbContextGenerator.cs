using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WabbitBot.Generator.Shared.Analyzers;
using WabbitBot.SourceGenerators.Templates;
using WabbitBot.SourceGenerators.Utils;
using static WabbitBot.SourceGenerators.Utils.GeneratorHelpers;

namespace WabbitBot.SourceGenerators.Generators.Database
{
    /// <summary>
    /// Source generator that creates EF Core DbContext configuration from [EntityMetadata] attributes.
    ///
    /// Generates:
    /// - DbSet properties for all entities
    /// - ConfigureXxx methods with table mappings
    /// - JSONB column type specifications
    /// - Index definitions (GIN for JSONB, regular for standard columns)
    /// - Foreign key relationships and constraints
    /// </summary>
    [Generator]
    public class DbContextGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            try
            {
                context.RegisterPostInitializationOutput(ctx => ctx.AddSource("__WG_Init_DbContextGenerator.g.cs", "// DbContextGenerator: Initialize hit"));
            }
            catch (Exception ex)
            {
                context.RegisterPostInitializationOutput(ctx => ctx.AddSource("__WG_Init_DbContextGenerator_Error.g.cs", "// Init exception: " + ex.Message.Replace("\"", "'")));
            }

            var isCoreProject = context.CompilationProvider.IsCore();

            // Extract entity metadata per attributed class, then aggregate
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

            var dbContextSource = entityInfos.Select((entities, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return GenerateDbContextConfiguration(entities.Where(e => e is not null)!);
            });

            context.RegisterSourceOutput(dbContextSource.Combine(isCoreProject), (spc, tuple) =>
            {
                if (!tuple.Right)
                    return;
                spc.AddSource("WabbitBotDbContext.Generated.g.cs", tuple.Left);
            });
        }

        private SourceText GenerateDbContextConfiguration(
            IEnumerable<WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata)
        {
            var sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine(CreateFileHeader("DbContextGenerator", "Auto-generated EF Core DbContext configuration"));
            sourceBuilder.AppendLine("using Microsoft.EntityFrameworkCore;");
            sourceBuilder.AppendLine("using WabbitBot.Common.Models;");
            sourceBuilder.AppendLine("using WabbitBot.Core.Common.Models;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace WabbitBot.Common.Data");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Partial class containing auto-generated DbContext configuration");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public partial class WabbitBotDbContext");
            sourceBuilder.AppendLine("    {");

            // Generate DbSet properties
            foreach (var metadata in entityMetadata)
            {
                var entityTypeName = GetFullEntityTypeName(metadata.ClassName, metadata.EntityType);
                sourceBuilder.AppendLine(CommonTemplates.CreateGeneratedDoc($"DbSet for {metadata.ClassName} entities."));
                sourceBuilder.AppendLine($"        public DbSet<{entityTypeName}> {GeneratorHelpers.ToPascalCase(metadata.ClassName)}s {{ get; set; }} = null!;");
                sourceBuilder.AppendLine();
            }

            // Generate Configure methods for each entity
            foreach (var metadata in entityMetadata)
            {
                GenerateEntityConfiguration(sourceBuilder, metadata);
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            return SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);
        }

        private void GenerateEntityConfiguration(StringBuilder sourceBuilder, WabbitBot.Generator.Shared.Metadata.EntityMetadataInfo metadata)
        {
            var entityTypeName = GetFullEntityTypeName(metadata.ClassName, metadata.EntityType);
            var methodName = $"Configure{GeneratorHelpers.ToPascalCase(metadata.ClassName)}";

            sourceBuilder.AppendLine($"        private void {methodName}(ModelBuilder modelBuilder)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine($"            modelBuilder.Entity<{entityTypeName}>(entity =>");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine($"                entity.ToTable(\"{metadata.TableName}\");");

            // Configure columns (skip if no columns defined)
            if (metadata.SnakeCaseColumnNames != null && metadata.SnakeCaseColumnNames.Length > 0)
            {
                foreach (var column in metadata.SnakeCaseColumnNames)
                {
                    sourceBuilder.AppendLine($"                entity.Property(e => e.{GeneratorHelpers.ToPascalCase(column)}).HasColumnName(\"{column}\");");
                }
            }

            sourceBuilder.AppendLine("            });");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
        }

        private string GetFullEntityTypeName(string className, INamedTypeSymbol entityType)
        {
            // Handle entities that need full namespace qualification
            var specialEntities = new Dictionary<string, string>
            {
                ["Stats"] = "WabbitBot.Core.Common.Models.Stats"
            };

            if (specialEntities.TryGetValue(className, out var fullName))
            {
                return fullName;
            }

            // Default: use the entity's full type name
            return entityType.ToDisplayString();
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
