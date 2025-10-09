using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using WabbitBot.Analyzers.Descriptors;
using WabbitBot.Generator.Shared.Utils;

namespace WabbitBot.Analyzers.Analyzers;

/// <summary>
/// Analyzer for EntityMetadata attribute usage.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EntityMetadataAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(EventAnalyzerDescriptors.MissingTableName, EventAnalyzerDescriptors.InvalidCacheSize);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl);

        if (classSymbol == null)
            return;

        // Check for EntityMetadata attribute
        var entityMetadataAttr = classSymbol
            .GetAttributes()
            .FirstOrDefault(attr =>
                attr.AttributeClass?.ToDisplayString() == "WabbitBot.Common.Attributes.EntityMetadataAttribute"
            );

        if (entityMetadataAttr != null)
        {
            // Check for required TableName
            CheckTableName(context, classDecl, classSymbol, entityMetadataAttr);

            // Check cache size validity
            CheckCacheSize(context, classDecl, classSymbol, entityMetadataAttr);
        }
    }

    private void CheckTableName(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDecl,
        INamedTypeSymbol classSymbol,
        AttributeData attribute
    )
    {
        // Check if tableName is specified (either positional or named argument)
        var tableName = AttributeExtractor.GetAttributeArgument(attribute, "tableName");

        if (string.IsNullOrEmpty(tableName))
        {
            var diagnostic = Diagnostic.Create(
                EventAnalyzerDescriptors.MissingTableName,
                classDecl.Identifier.GetLocation(),
                classSymbol.Name
            );
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void CheckCacheSize(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDecl,
        INamedTypeSymbol classSymbol,
        AttributeData attribute
    )
    {
        // Check MaxCacheSize validity
        var maxCacheSize = AttributeExtractor.GetAttributeIntArgument(attribute, "MaxCacheSize");

        if (maxCacheSize.HasValue && maxCacheSize.Value <= 0)
        {
            var diagnostic = Diagnostic.Create(
                EventAnalyzerDescriptors.InvalidCacheSize,
                classDecl.Identifier.GetLocation(),
                classSymbol.Name,
                maxCacheSize.Value
            );
            context.ReportDiagnostic(diagnostic);
        }
    }
}
