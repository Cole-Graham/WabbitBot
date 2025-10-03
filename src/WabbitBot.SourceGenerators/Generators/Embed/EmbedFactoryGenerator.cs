using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using WabbitBot.SourceGenerators.Templates;
using WabbitBot.SourceGenerators.Utils;

namespace WabbitBot.SourceGenerators.Generators.Embed;

/// <summary>
/// Generates embed factory code for classes marked with [GenerateEmbedFactory].
/// Creates a static factory for instantiating and managing Discord embeds.
/// 
/// DEPRECATED: This generator is obsolete. Use ComponentFactoryGenerator instead.
/// This class will be removed in a future version.
/// </summary>
[Obsolete("Use ComponentFactoryGenerator for component models instead. This generator is deprecated and will be removed.")]
[Generator]
public class EmbedFactoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var isDiscBotProject = context.CompilationProvider.IsDiscBot();
        // Pipeline: Filter classes with [GenerateEmbedFactory] attribute that inherit BaseEmbed
        var embedClasses = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node.HasAttribute("GenerateEmbedFactory"),
                transform: (ctx, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
                    if (classSymbol != null &&
                        classSymbol.BaseType?.ToDisplayString() == "WabbitBot.DiscBot.DSharpPlus.Embeds.BaseEmbed")
                    {
                        return classSymbol.Name;
                    }
                    return null;
                })
            .Where(name => name != null)
            .Collect();

        // Generate factory class
        var factorySource = embedClasses.Select((classes, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return EmbedTemplates.GenerateFactory(classes.Where(c => c != null).Cast<string>().ToList());
        });

        context.RegisterSourceOutput(factorySource.Combine(isDiscBotProject), (spc, tuple) =>
        {
            if (!tuple.Right)
                return;
            spc.AddSource("EmbedFactories.g.cs", tuple.Left);
        });
    }
}
