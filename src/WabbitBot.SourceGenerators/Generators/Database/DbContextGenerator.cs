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
                context.RegisterPostInitializationOutput(ctx =>
                    ctx.AddSource("__WG_Init_DbContextGenerator.g.cs", "// DbContextGenerator: Initialize hit")
                );
            }
            catch (Exception ex)
            {
                context.RegisterPostInitializationOutput(ctx =>
                    ctx.AddSource(
                        "__WG_Init_DbContextGenerator_Error.g.cs",
                        "// Init exception: " + ex.Message.Replace("\"", "'")
                    )
                );
            }

            var isCoreProject = context.CompilationProvider.IsCore();

            // Extract entity metadata per attributed class, then aggregate
            var entityInfos = context
                .SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                    transform: static (ctx, _) =>
                    {
                        var classDecl = (ClassDeclarationSyntax)ctx.Node;
                        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl);
                        if (classSymbol is null)
                            return null;

                        var attr = classSymbol
                            .GetAttributes()
                            .FirstOrDefault(a =>
                                a.AttributeClass?.ToDisplayString() == Generator.Shared.AttributeNames.EntityMetadata
                            );
                        return attr is null ? null : AttributeAnalyzer.ExtractEntityMetadataInfo(classSymbol, attr);
                    }
                )
                .Where(static info => info is not null)
                .Collect();

            var dbContextSource = entityInfos.Select(
                (entities, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return GenerateDbContextConfiguration(entities.Where(e => e is not null)!);
                }
            );

            context.RegisterSourceOutput(
                dbContextSource.Combine(isCoreProject),
                (spc, tuple) =>
                {
                    if (!tuple.Right)
                        return;
                    spc.AddSource("WabbitBotDbContext.Generated.g.cs", tuple.Left);
                }
            );

            // Generate archive model classes
            var archiveModelsSource = entityInfos.Select(
                (entities, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return GenerateArchiveModels(entities.Where(e => e is not null)!);
                }
            );

            context.RegisterSourceOutput(
                archiveModelsSource.Combine(isCoreProject),
                (spc, tuple) =>
                {
                    if (!tuple.Right)
                        return;
                    spc.AddSource("ArchiveModels.Generated.g.cs", tuple.Left);
                }
            );

            // Generate archive mappers (live <-> archive)
            var archiveMappersSource = entityInfos.Select(
                (entities, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    return GenerateArchiveMappers(entities.Where(e => e is not null)!);
                }
            );

            context.RegisterSourceOutput(
                archiveMappersSource.Combine(isCoreProject),
                (spc, tuple) =>
                {
                    if (!tuple.Right)
                        return;
                    spc.AddSource("ArchiveMappers.Generated.g.cs", tuple.Left);
                }
            );
        }

        private SourceText GenerateDbContextConfiguration(
            IEnumerable<Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata
        )
        {
            var sourceBuilder = new StringBuilder();

            sourceBuilder.AppendLine(
                CreateFileHeader("DbContextGenerator", "Auto-generated EF Core DbContext configuration")
            );
            sourceBuilder.AppendLine("using Microsoft.EntityFrameworkCore;");
            sourceBuilder.AppendLine("using Npgsql.EntityFrameworkCore.PostgreSQL;");
            sourceBuilder.AppendLine("using WabbitBot.Common.Models;");
            sourceBuilder.AppendLine("using WabbitBot.Core.Common.Models.Common;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace WabbitBot.Core.Common.Database");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Partial class containing auto-generated DbContext configuration");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public partial class WabbitBotDbContext : DbContext");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine(
                "        public WabbitBotDbContext(DbContextOptions<WabbitBotDbContext> options) : base(options)"
            );
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();

            // Generate DbSet properties
            foreach (var metadata in entityMetadata)
            {
                var entityTypeName = metadata.EntityType.ToDisplayString();
                sourceBuilder.AppendLine(
                    CommonTemplates.CreateGeneratedDoc($"DbSet for {metadata.ClassName} entities.")
                );
                var dbSetName = !string.IsNullOrWhiteSpace(metadata.ServicePropertyName)
                    ? NormalizeServicePropertyName(metadata.ServicePropertyName!)
                    : NormalizeServicePropertyName(metadata.DefaultServicePropertyName);
                sourceBuilder.AppendLine(
                    $"        public DbSet<{entityTypeName}> {dbSetName} {{ get; set; }} = null!;"
                );
                sourceBuilder.AppendLine();
            }

            // Generate Archive DbSet properties
            foreach (var metadata in entityMetadata)
            {
                var archiveTypeName = GetArchiveTypeName(metadata.ClassName);
                var archiveDbSetName = NormalizeServicePropertyName(metadata.DefaultServicePropertyName) + "Archives";
                sourceBuilder.AppendLine(
                    CommonTemplates.CreateGeneratedDoc($"DbSet for {metadata.ClassName} archive snapshots.")
                );
                sourceBuilder.AppendLine(
                    $"        public DbSet<{archiveTypeName}> {archiveDbSetName} {{ get; set; }} = null!;"
                );
                sourceBuilder.AppendLine("");
            }

            // Generate partial void declarations for custom relationship configurations
            sourceBuilder.AppendLine("        // Partial methods for custom relationship configuration");
            sourceBuilder.AppendLine(
                "        // Implement these in WabbitBotDbContext.Manual.cs for entities with complex relationships"
            );
            foreach (var metadata in entityMetadata)
            {
                sourceBuilder.AppendLine(
                    $"        partial void Configure{metadata.ClassName}Relationships(ModelBuilder modelBuilder);"
                );
            }
            sourceBuilder.AppendLine();

            // Generate OnModelCreating to invoke Configure methods
            sourceBuilder.AppendLine("        protected override void OnModelCreating(ModelBuilder modelBuilder)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            base.OnModelCreating(modelBuilder);");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine(
                "            // Call manual configuration methods (if they exist in the partial class)"
            );
            sourceBuilder.AppendLine("            ConfigureSchemaMetadata(modelBuilder);");
            sourceBuilder.AppendLine();
            foreach (var metadata in entityMetadata)
            {
                var methodName = $"Configure{metadata.ClassName}";
                sourceBuilder.AppendLine($"            {methodName}(modelBuilder);");
            }
            foreach (var metadata in entityMetadata)
            {
                var methodName = $"Configure{metadata.ClassName}Archive";
                sourceBuilder.AppendLine($"            {methodName}(modelBuilder);");
            }
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine(
                "            // Call custom relationship configuration methods (optional partial methods)"
            );
            foreach (var metadata in entityMetadata)
            {
                sourceBuilder.AppendLine($"            Configure{metadata.ClassName}Relationships(modelBuilder);");
            }
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();

            // Generate Configure methods for each entity
            foreach (var metadata in entityMetadata)
            {
                GenerateEntityConfiguration(sourceBuilder, metadata);
            }

            // Generate Archive configurations
            foreach (var metadata in entityMetadata)
            {
                GenerateArchiveEntityConfiguration(sourceBuilder, metadata);
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            return SourceText.From(sourceBuilder.ToString(), Encoding.UTF8);
        }

        private SourceText GenerateArchiveModels(
            IEnumerable<Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata
        )
        {
            var sb = new StringBuilder();
            sb.AppendLine(CreateFileHeader("DbContextGenerator", "Auto-generated archive models"));
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using WabbitBot.Common.Models;");
            sb.AppendLine();
            sb.AppendLine("namespace WabbitBot.Core.Common.Models.Common");
            sb.AppendLine("{");
            foreach (var m in entityMetadata)
            {
                // Gather scalar properties (exclude navs and collections of entities)
                var properties = m
                    .EntityType.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
                    .ToArray();

                bool IsEntityType(ITypeSymbol type)
                {
                    if (
                        type is INamedTypeSymbol nts
                        && nts.IsGenericType
                        && nts.Name == "Nullable"
                        && nts.TypeArguments.Length == 1
                    )
                    {
                        type = nts.TypeArguments[0];
                    }
                    var cur = type;
                    while (cur is INamedTypeSymbol named)
                    {
                        if (named.ToDisplayString() == "WabbitBot.Common.Models.Entity")
                            return true;
                        cur = named.BaseType;
                    }
                    return false;
                }

                // removed unused local function IsCollection to avoid CS8321

                var scalarProps = new List<IPropertySymbol>();
                foreach (var p in properties)
                {
                    var type = p.Type;
                    bool IsCollectionLike(ITypeSymbol t)
                    {
                        var display = t.ToDisplayString();
                        return display.StartsWith("System.Collections.Generic.ICollection<")
                            || display.StartsWith("System.Collections.Generic.IList<")
                            || display.StartsWith("System.Collections.Generic.IEnumerable<")
                            || display.StartsWith("System.Collections.Generic.IReadOnlyCollection<")
                            || display.StartsWith("System.Collections.Generic.List<")
                            || display.EndsWith("[]")
                            || t.AllInterfaces.Any(i =>
                                i.ToDisplayString().StartsWith("System.Collections.Generic.ICollection<")
                            );
                    }

                    var isCollection = IsCollectionLike(type);
                    var elementType = (type as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();

                    if (IsEntityType(type))
                    {
                        continue; // nav single
                    }
                    if (isCollection && elementType is not null && IsEntityType(elementType))
                    {
                        continue; // nav collection
                    }
                    // Skip base/metadata properties and Domain to avoid duplicates
                    if (
                        p.Name
                        is "Id"
                            or "CreatedAt"
                            or "UpdatedAt"
                            or "Domain"
                            or "ArchiveId"
                            or "EntityId"
                            or "Version"
                            or "ArchivedAt"
                            or "ArchivedBy"
                            or "Reason"
                    )
                        continue;

                    scalarProps.Add(p);
                }

                var archiveTypeName = GetArchiveTypeName(m.ClassName);
                sb.AppendLine($"    public class {archiveTypeName} : Entity");
                sb.AppendLine("    {");
                sb.AppendLine("        public Guid ArchiveId { get; set; }");
                sb.AppendLine("        public Guid EntityId { get; set; }");
                sb.AppendLine("        public int Version { get; set; }");
                sb.AppendLine("        public DateTime ArchivedAt { get; set; }");
                sb.AppendLine("        public Guid? ArchivedBy { get; set; }");
                sb.AppendLine("        public string? Reason { get; set; }");

                // Mirror scalar properties
                foreach (var p in scalarProps)
                {
                    var typeName = p.Type.ToDisplayString();
                    var isValueType = p.Type.IsValueType;
                    var isNonNullableRef = !isValueType && p.NullableAnnotation == NullableAnnotation.NotAnnotated;

                    static bool IsCollectionType(ITypeSymbol t)
                    {
                        var display = t.ToDisplayString();
                        return display.StartsWith("System.Collections.Generic.ICollection<")
                            || display.StartsWith("System.Collections.Generic.List<")
                            || display.EndsWith("[]")
                            || t.AllInterfaces.Any(i =>
                                i.ToDisplayString().StartsWith("System.Collections.Generic.ICollection<")
                            );
                    }

                    static bool IsDictionaryType(ITypeSymbol t)
                    {
                        return t.ToDisplayString().StartsWith("System.Collections.Generic.Dictionary<");
                    }

                    var initializer = string.Empty;
                    if (isNonNullableRef)
                    {
                        if (typeName == "string")
                        {
                            initializer = " = string.Empty;";
                        }
                        else if (IsCollectionType(p.Type) || IsDictionaryType(p.Type))
                        {
                            initializer = " = [];";
                        }
                        else
                        {
                            initializer = " = default!;";
                        }
                    }

                    sb.AppendLine($"        public {typeName} {p.Name} {{ get; set; }}{initializer}");
                }

                sb.AppendLine("        public override Domain Domain => Domain.Common;");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
            sb.AppendLine("}");
            return SourceText.From(sb.ToString(), Encoding.UTF8);
        }

        private SourceText GenerateArchiveMappers(
            IEnumerable<Generator.Shared.Metadata.EntityMetadataInfo> entityMetadata
        )
        {
            var sb = new StringBuilder();
            sb.AppendLine(CreateFileHeader("DbContextGenerator", "Auto-generated archive mappers"));
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using WabbitBot.Core.Common.Models.Common;");
            sb.AppendLine();
            sb.AppendLine("namespace WabbitBot.Core.Common.Database.Mappers");
            sb.AppendLine("{");
            foreach (var m in entityMetadata)
            {
                var entityTypeName = m.EntityType.ToDisplayString();
                var archiveTypeName = GetArchiveTypeName(m.ClassName);

                // Collect scalar properties mirrored in archive
                var props = m
                    .EntityType.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
                    .ToArray();

                static bool IsEntityType(ITypeSymbol type)
                {
                    if (
                        type is INamedTypeSymbol nts
                        && nts.IsGenericType
                        && nts.Name == "Nullable"
                        && nts.TypeArguments.Length == 1
                    )
                    {
                        type = nts.TypeArguments[0];
                    }
                    var cur = type;
                    while (cur is INamedTypeSymbol named)
                    {
                        if (named.ToDisplayString() == "WabbitBot.Common.Models.Entity")
                            return true;
                        cur = named.BaseType;
                    }
                    return false;
                }

                static bool IsCollection(ITypeSymbol type)
                {
                    var display = type.ToDisplayString();
                    return display.StartsWith("System.Collections.Generic.ICollection<")
                        || display.StartsWith("System.Collections.Generic.List<")
                        || display.EndsWith("[]")
                        || type.AllInterfaces.Any(i =>
                            i.ToDisplayString().StartsWith("System.Collections.Generic.ICollection<")
                        );
                }

                var scalarProps = new List<IPropertySymbol>();
                foreach (var p in props)
                {
                    var t = p.Type;
                    var isCollection = IsCollection(t);
                    var elementType = (t as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
                    if (IsEntityType(t))
                        continue;
                    if (isCollection && elementType is not null && IsEntityType(elementType))
                        continue;
                    if (p.Name is "Id" or "CreatedAt" or "UpdatedAt" or "Domain")
                        continue;
                    scalarProps.Add(p);
                }

                sb.AppendLine($"    internal static class {m.ClassName}ArchiveMapper");
                sb.AppendLine("    {");
                // MapToArchive
                sb.AppendLine(
                    $"        internal static void MapToArchive({entityTypeName} source, {archiveTypeName} dest)"
                );
                sb.AppendLine("        {");
                foreach (var p in scalarProps)
                {
                    var name = p.Name;
                    if (name is "ArchiveId" or "EntityId" or "Version" or "ArchivedAt" or "ArchivedBy" or "Reason")
                        continue;
                    sb.AppendLine($"            dest.{name} = source.{name};");
                }
                sb.AppendLine("        }");
                sb.AppendLine();

                // RestoreFromArchive
                sb.AppendLine($"        internal static {entityTypeName} RestoreFromArchive({archiveTypeName} src)");
                sb.AppendLine("        {");
                sb.AppendLine($"            return new {entityTypeName}");
                sb.AppendLine("            {");
                sb.AppendLine("                Id = src.EntityId,");
                sb.AppendLine("                CreatedAt = src.CreatedAt,");
                sb.AppendLine("                UpdatedAt = src.UpdatedAt,");
                foreach (var p in scalarProps)
                {
                    var name = p.Name;
                    if (name is "ArchiveId" or "EntityId" or "Version" or "ArchivedAt" or "ArchivedBy" or "Reason")
                        continue;
                    sb.AppendLine($"                {name} = src.{name},");
                }
                sb.AppendLine("            };");
                sb.AppendLine("        }");

                sb.AppendLine("    }");
                sb.AppendLine();
            }
            sb.AppendLine("}");
            return SourceText.From(sb.ToString(), Encoding.UTF8);
        }

        private void GenerateEntityConfiguration(
            StringBuilder sourceBuilder,
            Generator.Shared.Metadata.EntityMetadataInfo metadata
        )
        {
            var entityTypeName = metadata.EntityType.ToDisplayString();
            var methodName = $"Configure{metadata.ClassName}";

            sourceBuilder.AppendLine($"        private void {methodName}(ModelBuilder modelBuilder)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine($"            modelBuilder.Entity<{entityTypeName}>(entity =>");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine($"                entity.ToTable(\"{metadata.TableName}\");");
            sourceBuilder.AppendLine("                entity.HasKey(e => e.Id);");
            sourceBuilder.AppendLine(
                "                entity.Property(e => e.Id).HasColumnName(\"id\").ValueGeneratedOnAdd();"
            );
            sourceBuilder.AppendLine(
                "                entity.Property(e => e.CreatedAt).HasColumnName(\"created_at\");"
            );
            sourceBuilder.AppendLine(
                "                entity.Property(e => e.UpdatedAt).HasColumnName(\"updated_at\");"
            );
            AppendEntityPropertyConfigurations(sourceBuilder, metadata);
            sourceBuilder.AppendLine("            });");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
        }

        private void GenerateArchiveEntityConfiguration(
            StringBuilder sourceBuilder,
            Generator.Shared.Metadata.EntityMetadataInfo metadata
        )
        {
            var archiveTypeName = GetArchiveTypeName(metadata.ClassName);
            var methodName = $"Configure{metadata.ClassName}Archive";
            var archiveTable = metadata.TableName + "_archive";

            sourceBuilder.AppendLine($"        private void {methodName}(ModelBuilder modelBuilder)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine($"            modelBuilder.Entity<{archiveTypeName}>(entity =>");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine($"                entity.ToTable(\"{archiveTable}\");");
            sourceBuilder.AppendLine("                entity.HasKey(e => e.ArchiveId);");
            sourceBuilder.AppendLine(
                "                entity.Property(e => e.ArchiveId).HasColumnName(\"archive_id\");"
            );
            sourceBuilder.AppendLine("                entity.Property(e => e.EntityId).HasColumnName(\"entity_id\");");
            sourceBuilder.AppendLine("                entity.Property(e => e.Version).HasColumnName(\"version\");");
            sourceBuilder.AppendLine(
                "                entity.Property(e => e.ArchivedAt).HasColumnName(\"archived_at\");"
            );
            sourceBuilder.AppendLine(
                "                entity.Property(e => e.ArchivedBy).HasColumnName(\"archived_by\");"
            );
            sourceBuilder.AppendLine("                entity.Property(e => e.Reason).HasColumnName(\"reason\");");

            // Mirror scalar properties
            AppendArchivePropertyConfigurations(sourceBuilder, metadata);

            sourceBuilder.AppendLine(
                $"                entity.HasIndex(e => new {{ e.EntityId, e.Version }}).HasDatabaseName(\"idx_{archiveTable}_entity_version\");"
            );
            sourceBuilder.AppendLine(
                $"                entity.HasIndex(e => e.ArchivedAt).HasDatabaseName(\"idx_{archiveTable}_archived_at\");"
            );
            sourceBuilder.AppendLine("            });");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
        }

        private static void AppendEntityPropertyConfigurations(
            StringBuilder sourceBuilder,
            Generator.Shared.Metadata.EntityMetadataInfo metadata
        )
        {
            var properties = metadata
                .EntityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
                .ToArray();

            static bool IsEntityType(ITypeSymbol type)
            {
                if (
                    type is INamedTypeSymbol nts
                    && nts.IsGenericType
                    && nts.Name == "Nullable"
                    && nts.TypeArguments.Length == 1
                )
                {
                    type = nts.TypeArguments[0];
                }
                var cur = type;
                while (cur is INamedTypeSymbol named)
                {
                    if (named.ToDisplayString() == "WabbitBot.Common.Models.Entity")
                        return true;
                    cur = named.BaseType;
                }
                return false;
            }

            static bool IsCollection(ITypeSymbol type)
            {
                var display = type.ToDisplayString();
                if (
                    display.StartsWith("System.Collections.Generic.ICollection<")
                    || display.StartsWith("System.Collections.Generic.List<")
                    || display.EndsWith("[]")
                )
                    return true;
                // Also consider interfaces that implement ICollection<T>
                return type.AllInterfaces.Any(i =>
                    i.ToDisplayString().StartsWith("System.Collections.Generic.ICollection<")
                );
            }

            static bool IsDictionary(ITypeSymbol type) =>
                type.ToDisplayString().StartsWith("System.Collections.Generic.Dictionary<");

            static string ToSnakeCase(string s)
            {
                if (string.IsNullOrEmpty(s))
                    return s;
                var sb = new StringBuilder();
                for (int i = 0; i < s.Length; i++)
                {
                    var c = s[i];
                    if (char.IsUpper(c))
                    {
                        if (i > 0)
                            sb.Append('_');
                        sb.Append(char.ToLowerInvariant(c));
                    }
                    else
                        sb.Append(c);
                }
                return sb.ToString();
            }

            var navSingles = new List<IPropertySymbol>();
            var navCollections = new List<IPropertySymbol>();
            var scalarProps = new List<IPropertySymbol>();
            var jsonColumns = new List<(string PropName, string ColumnName)>();

            foreach (var p in properties)
            {
                var type = p.Type;
                var isCollection = IsCollection(type);
                var elementType =
                    type is INamedTypeSymbol nts && nts.IsGenericType ? nts.TypeArguments.FirstOrDefault() : null;

                // Skip computed/read-only properties (e.g., Domain)
                if (p.SetMethod is null || p.Name == "Domain")
                {
                    continue;
                }

                if (IsEntityType(type))
                {
                    navSingles.Add(p);
                }
                else if (isCollection && elementType is not null && IsEntityType(elementType))
                {
                    navCollections.Add(p);
                }
                else
                {
                    scalarProps.Add(p);
                }
            }

            foreach (var p in scalarProps)
            {
                var name = p.Name;
                if (name is "Id" or "CreatedAt" or "UpdatedAt")
                    continue;

                var column = ToSnakeCase(name);
                var line = $"                entity.Property(e => e.{name}).HasColumnName(\"{column}\")";

                string? assignedColumnType = null;
                var t = p.Type;
                var isCollection = IsCollection(t);
                var isDict = IsDictionary(t);
                if (isCollection || isDict)
                {
                    var elementType = (t as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
                    if (isDict)
                    {
                        assignedColumnType = "jsonb";
                    }
                    else if (elementType is not null && !IsEntityType(elementType))
                    {
                        var elemDisplay = elementType.ToDisplayString();
                        if (elemDisplay == "System.Guid")
                            assignedColumnType = "uuid[]";
                        else if (elemDisplay == "System.String")
                            assignedColumnType = "text[]";
                        else
                            assignedColumnType = "jsonb";
                    }
                }

                if (assignedColumnType is not null)
                {
                    line += $".HasColumnType(\"{assignedColumnType}\")";
                    if (assignedColumnType == "jsonb")
                        jsonColumns.Add((p.Name, column));
                }

                var isRequired = p.Type.IsValueType && !(p.NullableAnnotation == NullableAnnotation.Annotated);
                if (!p.Type.IsValueType && p.NullableAnnotation == NullableAnnotation.NotAnnotated)
                {
                    isRequired = true;
                }
                if (isRequired)
                    line += ".IsRequired()";

                line += ";";
                sourceBuilder.AppendLine(line);
            }

            // Create GIN indexes for JSONB columns
            foreach (var jc in jsonColumns)
            {
                sourceBuilder.AppendLine(
                    $"                entity.HasIndex(e => e.{jc.PropName}).HasDatabaseName(\"gin_idx_{metadata.TableName}_{jc.ColumnName}\").HasMethod(\"gin\");"
                );
            }

            // SKIP single navigation properties - they will be configured from the collection side
            // to avoid duplicate relationship configurations that create shadow foreign keys.
            // If we configure HasOne().WithMany() here and ALSO HasMany().WithOne() from the principal,
            // EF Core creates two separate relationships on the same FK, causing shadow properties like MatchId1.
            //
            // foreach (var nav in navSingles)
            // {
            //     var fkNameCandidate = nav.Name + "Id";
            //     var fk = scalarProps.FirstOrDefault(p => string.Equals(p.Name, fkNameCandidate, StringComparison.Ordinal));
            //     if (fk is null)
            //     {
            //         continue;
            //     }
            //
            //     var deleteBehavior = (fk.Type.IsValueType && fk.NullableAnnotation != NullableAnnotation.Annotated)
            //         ? "DeleteBehavior.Cascade"
            //         : "DeleteBehavior.SetNull";
            //
            //     sourceBuilder.AppendLine($"                entity.HasOne(e => e.{nav.Name}).WithMany().HasForeignKey(e => e.{fk.Name}).OnDelete({deleteBehavior});");
            //
            //     var fkColumn = ToSnakeCase(fk.Name);
            //     sourceBuilder.AppendLine($"                entity.HasIndex(e => e.{fk.Name}).HasDatabaseName(\"idx_{metadata.TableName}_{fkColumn}\");");
            // }

            // Instead, we still need to create indexes for foreign keys
            foreach (var nav in navSingles)
            {
                var fkNameCandidate = nav.Name + "Id";
                var fk = scalarProps.FirstOrDefault(p =>
                    string.Equals(p.Name, fkNameCandidate, StringComparison.Ordinal)
                );
                if (fk is null)
                {
                    continue;
                }

                var fkColumn = ToSnakeCase(fk.Name);
                sourceBuilder.AppendLine(
                    $"                entity.HasIndex(e => e.{fk.Name}).HasDatabaseName(\"idx_{metadata.TableName}_{fkColumn}\");"
                );
            }

            // Collection navigations: emit HasMany/WithOne when inverse and FK are discoverable
            foreach (var nav in navCollections)
            {
                if (nav.Type is not INamedTypeSymbol navType || navType.TypeArguments.Length != 1)
                    continue;
                var elementType = navType.TypeArguments[0];
                // Find back-reference property on the element type that points to this entity
                var backRef = elementType
                    .GetMembers()
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(pp => SymbolEqualityComparer.Default.Equals(pp.Type, metadata.EntityType));
                // Find FK property on the element type named {EntityName}Id
                var fkProp = elementType
                    .GetMembers()
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(pp => pp.Name == metadata.ClassName + "Id");
                if (backRef is null || fkProp is null)
                    continue;
                sourceBuilder.AppendLine(
                    $"                entity.HasMany(e => e.{nav.Name}).WithOne(e => e.{backRef.Name}).HasForeignKey(e => e.{fkProp.Name}).OnDelete(DeleteBehavior.Cascade);"
                );
            }

            var nameProp = scalarProps.FirstOrDefault(p =>
                p.Name == "Name" && p.Type.ToDisplayString() == "System.String"
            );
            if (nameProp is not null)
            {
                sourceBuilder.AppendLine(
                    $"                entity.HasIndex(e => e.Name).HasDatabaseName(\"idx_{metadata.TableName}_name\");"
                );
            }
        }

        private static void AppendArchivePropertyConfigurations(
            StringBuilder sourceBuilder,
            Generator.Shared.Metadata.EntityMetadataInfo metadata
        )
        {
            var properties = metadata
                .EntityType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
                .ToArray();

            static bool IsEntityType(ITypeSymbol type)
            {
                if (
                    type is INamedTypeSymbol nts
                    && nts.IsGenericType
                    && nts.Name == "Nullable"
                    && nts.TypeArguments.Length == 1
                )
                {
                    type = nts.TypeArguments[0];
                }
                var cur = type;
                while (cur is INamedTypeSymbol named)
                {
                    if (named.ToDisplayString() == "WabbitBot.Common.Models.Entity")
                        return true;
                    cur = named.BaseType;
                }
                return false;
            }

            static bool IsCollection(ITypeSymbol type)
            {
                var display = type.ToDisplayString();
                return display.StartsWith("System.Collections.Generic.ICollection<")
                    || display.StartsWith("System.Collections.Generic.List<")
                    || display.EndsWith("[]")
                    || type.AllInterfaces.Any(i =>
                        i.ToDisplayString().StartsWith("System.Collections.Generic.ICollection<")
                    );
            }

            static string ToSnakeCase(string s)
            {
                if (string.IsNullOrEmpty(s))
                    return s;
                var sb = new StringBuilder();
                for (int i = 0; i < s.Length; i++)
                {
                    var c = s[i];
                    if (char.IsUpper(c))
                    {
                        if (i > 0)
                            sb.Append('_');
                        sb.Append(char.ToLowerInvariant(c));
                    }
                    else
                        sb.Append(c);
                }
                return sb.ToString();
            }

            foreach (var p in properties)
            {
                var type = p.Type;
                var isCollection = IsCollection(type);
                var elementType =
                    type is INamedTypeSymbol nts && nts.IsGenericType ? nts.TypeArguments.FirstOrDefault() : null;

                // Skip navigations and collections of entities
                if (IsEntityType(type) || (isCollection && elementType is not null && IsEntityType(elementType)))
                {
                    continue;
                }

                var name = p.Name;
                if (
                    name
                    is "Id"
                        or "CreatedAt"
                        or "UpdatedAt"
                        or "Domain"
                        or "ArchiveId"
                        or "EntityId"
                        or "Version"
                        or "ArchivedAt"
                        or "ArchivedBy"
                        or "Reason"
                )
                    continue;

                var column = ToSnakeCase(name);
                var line = $"                entity.Property(e => e.{name}).HasColumnName(\"{column}\")";

                // Assign Postgres-specific types for archive scalar collections/dictionaries
                string? assignedColumnType = null;
                // Dictionary must map to jsonb regardless of ICollection interfaces
                if (type.ToDisplayString().StartsWith("System.Collections.Generic.Dictionary<"))
                {
                    assignedColumnType = "jsonb";
                }
                else if (IsCollection(type))
                {
                    if (elementType is not null && !IsEntityType(elementType))
                    {
                        var elemDisplay = elementType.ToDisplayString();
                        if (elemDisplay == "System.Guid")
                            assignedColumnType = "uuid[]";
                        else if (elemDisplay == "System.String")
                            assignedColumnType = "text[]";
                        else
                            assignedColumnType = "jsonb";
                    }
                }

                if (assignedColumnType is not null)
                {
                    line += $".HasColumnType(\"{assignedColumnType}\")";
                }

                line += ";";
                sourceBuilder.AppendLine(line);
            }
        }

        private static string GetArchiveTypeName(string className) => className + "Archive";

        // Pluralization is intentionally not handled here; DbSet names are provided via ServicePropertyName.

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
