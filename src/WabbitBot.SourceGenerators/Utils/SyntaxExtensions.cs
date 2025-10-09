using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WabbitBot.SourceGenerators.Utils;

/// <summary>
/// Extension methods for syntax nodes.
/// </summary>
public static class SyntaxExtensions
{
    /// <summary>
    /// Checks if a syntax node has an attribute with the specified name (syntax-only check).
    /// </summary>
    public static bool HasAttribute(this SyntaxNode node, string attributeName)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        return classDecl
            .AttributeLists.SelectMany(al => al.Attributes)
            .Any(attr => attr.Name.ToString().Contains(attributeName));
    }
}
