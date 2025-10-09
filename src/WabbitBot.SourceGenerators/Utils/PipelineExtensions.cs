// # Ext methods: e.g., context.ForAttributeWithSimpleName("EventBoundary")

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WabbitBot.SourceGenerators.Utils;

/// <summary>
/// Extension methods for IncrementalGeneratorInitializationContext to provide common filtering patterns.
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// Creates a provider that filters for classes inheriting from a specific base type.
    /// </summary>
    public static IncrementalValuesProvider<GeneratorSyntaxContext> ForClassWithBaseType(
        this IncrementalGeneratorInitializationContext context,
        string baseTypeName
    )
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
            predicate: (node, _) =>
            {
                if (node is not ClassDeclarationSyntax classDecl)
                    return false;

                return classDecl.BaseList?.Types.Any(type => type.Type.ToString() == baseTypeName) == true;
            },
            transform: (ctx, _) => ctx
        );
    }

    /// <summary>
    /// Filters the provider to only include items where the semantic symbol is available.
    /// </summary>
    public static IncrementalValuesProvider<T> WhereSemanticModelAvailable<T>(
        this IncrementalValuesProvider<T> provider
    )
        where T : struct
    {
        return provider.Where(item =>
        {
            if (item is GeneratorSyntaxContext syntaxCtx)
                return syntaxCtx.SemanticModel != null;
            if (item is GeneratorAttributeSyntaxContext attrCtx)
                return attrCtx.SemanticModel != null;
            return true;
        });
    }

    /// <summary>
    /// Transforms the provider to extract INamedTypeSymbol from the context.
    /// </summary>
    public static IncrementalValuesProvider<INamedTypeSymbol?> SelectSymbol<T>(
        this IncrementalValuesProvider<T> provider
    )
        where T : struct
    {
        return provider.Select(
            (ctx, _) =>
            {
                if (ctx is GeneratorSyntaxContext syntaxCtx)
                    return syntaxCtx.Node is TypeDeclarationSyntax typeDecl
                        ? syntaxCtx.SemanticModel.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol
                        : null;

                if (ctx is GeneratorAttributeSyntaxContext attrCtx)
                    return attrCtx.TargetSymbol as INamedTypeSymbol;

                return null;
            }
        );
    }

    /// <summary>
    /// Filters out null symbols from the provider.
    /// </summary>
    public static IncrementalValuesProvider<INamedTypeSymbol> WhereNotNull(
        this IncrementalValuesProvider<INamedTypeSymbol?> provider
    )
    {
        return provider.Where(symbol => symbol != null).Select((symbol, _) => symbol!);
    }
}
