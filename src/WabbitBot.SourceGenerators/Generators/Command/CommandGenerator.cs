using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WabbitBot.SourceGenerators.Generators.Command;

[Generator]
public class CommandGenerator : ISourceGenerator
{
    private const string WabbitCommandAttribute = "WabbitBot.Common.Attributes.WabbitCommandAttribute";
    private const string DescriptionAttribute = "System.ComponentModel.DescriptionAttribute";

    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will gather command classes
        context.RegisterForSyntaxNotifications(() => new CommandSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not CommandSyntaxReceiver receiver)
            return;

        // Only run if we found classes with Discord-specific attributes
        if (!receiver.CommandClasses.Any())
            return;

        var compilation = context.Compilation;
        var wabbitCommandSymbol = compilation.GetTypeByMetadataName(WabbitCommandAttribute);
        var descriptionSymbol = compilation.GetTypeByMetadataName(DescriptionAttribute);

        if (wabbitCommandSymbol == null)
        {
            // WabbitCommand attribute not found in compilation
            return;
        }

        var commandClasses = new List<INamedTypeSymbol>();

        foreach (var classDeclaration in receiver.CommandClasses)
        {
            var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (classSymbol != null && HasWabbitCommandAttribute(classSymbol, wabbitCommandSymbol))
            {
                commandClasses.Add(classSymbol);
            }
        }

        if (commandClasses.Any())
        {
            var source = GenerateCommandRegistration(commandClasses);
            var fileName = "CommandRegistrationHandler.g.cs";
            var sourceText = SourceText.From(source, Encoding.UTF8);

            // Check if this source has already been added to avoid duplicates
            if (!context.Compilation.SyntaxTrees.Any(tree => tree.FilePath.EndsWith(fileName)))
            {
                context.AddSource(fileName, sourceText);
            }
        }
    }

    private bool HasWabbitCommandAttribute(ISymbol symbol, INamedTypeSymbol attributeSymbol)
    {
        return symbol.GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));
    }

    private string GenerateCommandRegistration(List<INamedTypeSymbol> commandClasses)
    {
        var builder = new StringBuilder(@"
using DSharpPlus.Commands;
using System.Threading.Tasks;

namespace WabbitBot.DiscBot.DSharpPlus.Commands
{
    public partial class CommandRegistrationHandler
    {
        private async Task RegisterGeneratedCommandsAsync()
        {");

        foreach (var commandClass in commandClasses)
        {
            // Convert business logic class name to Discord integration class name
            var businessClassName = commandClass.Name;
            var discordClassName = businessClassName + "Discord";

            builder.AppendLine($@"
            // Commands are now registered during client setup in DiscordBot.cs
            // This method is kept for future extensibility");
        }

        builder.Append(@"
        }
    }
}");

        return builder.ToString();
    }

}

public class CommandSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> CommandClasses { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDeclaration)
        {
            // Only add classes that have Discord-specific event generation attributes
            if (HasDiscordEventGenerationAttribute(classDeclaration))
            {
                CommandClasses.Add(classDeclaration);
            }
        }
    }

    private bool HasDiscordEventGenerationAttribute(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .Any(attr =>
            {
                var attrName = attr.Name.ToString();
                return attrName.Contains("GenerateDiscordEventHandler") ||
                       attrName.EndsWith("GenerateDiscordEventHandler") ||
                       attrName.EndsWith("GenerateDiscordEventHandlerAttribute") ||
                       attrName.Contains("GenerateDiscordEventPublisher") ||
                       attrName.EndsWith("GenerateDiscordEventPublisher") ||
                       attrName.EndsWith("GenerateDiscordEventPublisherAttribute");
            });
    }
}