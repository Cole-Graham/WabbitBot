using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WabbitBot.SourceGenerators;

[Generator]
public class CommandGenerator : ISourceGenerator
{
    private const string WabbitCommandAttribute = "WabbitBot.DiscBot.Attributes.WabbitCommandAttribute";
    private const string DescriptionAttribute = "WabbitBot.DiscBot.Attributes.DescriptionAttribute";

    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will gather command classes
        context.RegisterForSyntaxNotifications(() => new CommandSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not CommandSyntaxReceiver receiver)
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
            context.AddSource("CommandRegistration.g.cs", SourceText.From(source, Encoding.UTF8));
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

namespace WabbitBot.DiscBot
{
    public static class CommandRegistration
    {
        public static async Task RegisterAllCommandsAsync(CommandsExtension commands)
        {");

        foreach (var commandClass in commandClasses)
        {
            builder.AppendLine($@"
            await commands.RegisterCommands<{commandClass.ToDisplayString()}>();");
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
        if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
            classDeclaration.AttributeLists.Any())
        {
            CommandClasses.Add(classDeclaration);
        }
    }
}
