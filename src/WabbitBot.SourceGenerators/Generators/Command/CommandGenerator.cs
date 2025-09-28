using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WabbitBot.Analyzers.Descriptors;
using WabbitBot.Generator.Shared.Analyzers;
using WabbitBot.SourceGenerators.Templates;
using WabbitBot.SourceGenerators.Utils;

namespace WabbitBot.SourceGenerators.Generators.Command;

[Generator]
public class CommandGenerator : IIncrementalGenerator
{
    private const string WabbitCommandAttribute = "WabbitBot.Common.Attributes.WabbitCommandAttribute";
    private const string DescriptionAttribute = "System.ComponentModel.DescriptionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var isDiscBotProject = context.CompilationProvider.IsDiscBot();

        // Pipeline: Filter classes with [WabbitCommand] attribute
        var commandClasses = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node.HasAttribute("WabbitCommand"),
                transform: (ctx, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
                    return classSymbol!;
                })
            .Collect();

        // Generate registration handler
        var registrationSource = commandClasses.Select((classes, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return GenerateCommandRegistration(classes.Where(c => c != null).ToList()!);
        });


        // Generate event-raising partial classes
        var eventRaisingClasses = commandClasses
            .SelectMany((classes, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return classes.Where(c => c != null).Select(c => GenerateEventRaisingPartial(c!));
            })
            .Collect();

        context.RegisterSourceOutput(registrationSource.Combine(isDiscBotProject), (spc, tuple) =>
        {
            if (!tuple.Right)
                return;
            spc.AddSource("CommandRegistrationHandler.g.cs", tuple.Left);
        });

        context.RegisterSourceOutput(eventRaisingClasses.Combine(isDiscBotProject), (spc, tuple) =>
        {
            if (!tuple.Right)
                return;
            foreach (var cls in tuple.Left)
            {
                spc.AddSource(cls.FileName, cls.Content);
            }
        });
    }


    private SourceText GenerateCommandRegistration(List<INamedTypeSymbol> commandClasses)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using DSharpPlus.Commands;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine();
        builder.AppendLine("namespace WabbitBot.DiscBot.DSharpPlus.Commands");
        builder.AppendLine("{");
        builder.AppendLine("    public partial class CommandRegistrationHandler");
        builder.AppendLine("    {");
        builder.AppendLine("        private async Task RegisterGeneratedCommandsAsync(CommandsExtension commands)");
        builder.AppendLine("        {");

        foreach (var commandClass in commandClasses)
        {
            // Convert business logic class name to Discord integration class name
            var businessClassName = commandClass.Name;
            var discordClassName = businessClassName + "Discord";

            builder.AppendLine($"            // Auto-register generated Discord command class");
            builder.AppendLine($"            await commands.RegisterCommands<{discordClassName}>(0); // Guild ID 0 = global");
        }

        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return SourceText.From(builder.ToString(), Encoding.UTF8);
    }

    private (string FileName, SourceText Content) GenerateEventRaisingPartial(INamedTypeSymbol commandClass)
    {
        var className = commandClass.Name;
        var methods = commandClass.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public &&
                       !m.IsStatic &&
                       m.MethodKind == MethodKind.Ordinary)
            .ToList();

        var eventRaisers = new StringBuilder();
        var dependencies = new[] { ("WabbitBot.Common.Events.EventInterfaces.IDiscordEventBus", "eventBus") };

        foreach (var method in methods)
        {
            var eventName = InferenceHelpers.InferEventName(method.Name);
            var parameters = InferenceHelpers.ExtractEventParameters(method);
            var paramList = string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}"));
            var paramAssignments = string.Join(", ", parameters.Select(p => $"{p.Name}: {p.Name}"));

            var raiserMethod = SourceEmitter.CreateMethod(
                returnType: "ValueTask",
                methodName: $"Raise{method.Name}EventAsync",
                parameters: paramList,
                body: $$"""
                    ArgumentNullException.ThrowIfNull(_eventBus);
                    var @event = new {{eventName}}({{paramAssignments}}, EventBusType = EventBusType.DiscBot);
                    await _eventBus.PublishAsync(@event);
                    """,
                modifiers: "partial");

            eventRaisers.AppendLine(CommonTemplates.CreateGeneratedDoc($"Raises {eventName} event after command execution."));
            eventRaisers.AppendLine(raiserMethod);
            eventRaisers.AppendLine();
        }

        var constructorParams = string.Join(", ", dependencies.Select(d => $"{d.Item1} {d.Item2}"));
        var fieldAssignments = string.Join("\n    ",
            dependencies.Select(d => $"private readonly {d.Item1} _{d.Item2};"));

        var constructor = SourceEmitter.CreateMethod(
            returnType: "",
            methodName: className,
            parameters: constructorParams,
            body: string.Join("\n        ", dependencies.Select(d => $"_{d.Item2} = {d.Item2};")),
            modifiers: "public");

        var classContent = SourceEmitter.CreatePartialClass(className,
            $"{fieldAssignments}\n\n{constructor}\n\n{eventRaisers}");

        var content = $$"""
            // <auto-generated>
            // This file was generated by CommandGenerator
            // Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
            // </auto-generated>

            namespace WabbitBot.DiscBot.DSharpPlus.Commands;

            {{classContent}}
            """;

        return ($"{className}.Events.g.cs", content.ToSourceText());
    }

}