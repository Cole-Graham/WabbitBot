using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WabbitBot.SourceGenerators;

[Generator]
public class CommandImplementationGenerator : ISourceGenerator
{
    private const string WabbitCommandAttribute = "WabbitBot.Common.Attributes.WabbitCommandAttribute";
    private const string DescriptionAttribute = "WabbitBot.Common.Attributes.DescriptionAttribute";
    private const string ChoiceProviderAttribute = "WabbitBot.Common.Attributes.ChoiceProviderAttribute";
    private const string AutoCompleteProviderAttribute = "WabbitBot.Common.Attributes.AutoCompleteProviderAttribute";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new CommandMethodSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not CommandMethodSyntaxReceiver receiver)
            return;

        var compilation = context.Compilation;
        var wabbitCommandSymbol = compilation.GetTypeByMetadataName(WabbitCommandAttribute);
        var choiceProviderSymbol = compilation.GetTypeByMetadataName(ChoiceProviderAttribute);
        var autoCompleteProviderSymbol = compilation.GetTypeByMetadataName(AutoCompleteProviderAttribute);

        if (wabbitCommandSymbol == null) return;

        foreach (var method in receiver.CommandMethods)
        {
            var model = compilation.GetSemanticModel(method.SyntaxTree);
            var methodSymbol = model.GetDeclaredSymbol(method);

            if (methodSymbol != null && HasWabbitCommandAttribute(methodSymbol, wabbitCommandSymbol))
            {
                var source = GenerateCommandImplementation(methodSymbol, choiceProviderSymbol, autoCompleteProviderSymbol);
                var fileName = $"{methodSymbol.ContainingType.Name}.{methodSymbol.Name}.g.cs";
                context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
            }
        }
    }

    private bool HasWabbitCommandAttribute(ISymbol symbol, INamedTypeSymbol attributeSymbol)
    {
        return symbol.GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));
    }

    private string GenerateCommandImplementation(
        IMethodSymbol methodSymbol,
        INamedTypeSymbol? choiceProviderSymbol,
        INamedTypeSymbol? autoCompleteProviderSymbol)
    {
        var containingType = methodSymbol.ContainingType;
        var namespaceName = containingType.ContainingNamespace.ToDisplayString();
        var commandName = GetCommandName(methodSymbol);
        var parameters = methodSymbol.Parameters.Skip(1); // Skip CommandContext

        var builder = new StringBuilder();
        builder.AppendLine($@"
using DSharpPlus.Commands;
using System.Threading.Tasks;

namespace {namespaceName}
{{
    public partial class {containingType.Name}
    {{
        // Generated implementation
        private partial Task {methodSymbol.Name}(CommandContext ctx)
        {{
            try
            {{");

        // Generate parameter binding code
        foreach (var param in parameters)
        {
            var paramName = param.Name;
            var paramType = param.Type;
            var defaultValue = param.HasExplicitDefaultValue ? GetDefaultValueString(param) : "null";

            builder.AppendLine($@"
                var {paramName}Raw = ctx.Options.GetValueOrDefault(""{paramName}"")?.ToString() ?? {defaultValue};");

            // Add choice provider handling
            if (HasAttribute(param, choiceProviderSymbol))
            {
                var providerType = GetProviderType(param, choiceProviderSymbol);
                builder.AppendLine($@"
                var {paramName}Provider = new {providerType}();
                var {paramName}Choices = {paramName}Provider.GetChoices();
                if (!{paramName}Choices.Any(c => c.Value == {paramName}Raw))
                {{
                    throw new CommandException($""Invalid choice for {paramName}: {{{{{paramName}Raw}}}}"");
                }}");
            }

            // Add type conversion
            builder.AppendLine(GenerateTypeConversion(paramName, paramType, defaultValue));
        }

        // Generate the actual method call
        var parameterList = string.Join(", ",
            new[] { "ctx" }.Concat(parameters.Select(p => $"{p.Name}Value")));

        builder.AppendLine($@"
                return {methodSymbol.Name}ImplementationAsync({parameterList});
            }}
            catch (Exception ex)
            {{
                throw new CommandException($""Error executing command: {{ex.Message}}"");
            }}
        }}
    }}
}}");

        return builder.ToString();
    }

    private string GetCommandName(IMethodSymbol methodSymbol)
    {
        var attr = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == WabbitCommandAttribute);

        return attr?.ConstructorArguments.FirstOrDefault().Value?.ToString()
            ?? methodSymbol.Name.Replace("Async", "");
    }

    private string GetDefaultValueString(IParameterSymbol param)
    {
        if (!param.HasExplicitDefaultValue) return "null";

        var value = param.ExplicitDefaultValue;
        if (value == null) return "null";

        return param.Type.SpecialType switch
        {
            SpecialType.System_String => $"\"{value}\"",
            SpecialType.System_Int32 => value.ToString(),
            SpecialType.System_Boolean => value.ToString().ToLower(),
            _ => "null"
        };
    }

    private string GenerateTypeConversion(string paramName, ITypeSymbol type, string defaultValue)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => $@"
                var {paramName}Value = {paramName}Raw ?? {defaultValue};",

            SpecialType.System_Int32 => $@"
                var {paramName}Value = {paramName}Raw != null 
                    ? int.Parse({paramName}Raw) 
                    : {defaultValue};",

            SpecialType.System_Boolean => $@"
                var {paramName}Value = {paramName}Raw != null 
                    ? bool.Parse({paramName}Raw) 
                    : {defaultValue};",

            _ => $@"
                var {paramName}Value = {defaultValue};"
        };
    }

    private bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeSymbol)
    {
        if (attributeSymbol == null) return false;
        return symbol.GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeSymbol));
    }

    private string GetProviderType(IParameterSymbol param, INamedTypeSymbol? attributeSymbol)
    {
        if (attributeSymbol == null) return string.Empty;

        var attr = param.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeSymbol));

        return attr?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? string.Empty;
    }
}

public class CommandMethodSyntaxReceiver : ISyntaxReceiver
{
    public List<MethodDeclarationSyntax> CommandMethods { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is MethodDeclarationSyntax methodDeclaration &&
            methodDeclaration.AttributeLists.Any())
        {
            CommandMethods.Add(methodDeclaration);
        }
    }
}