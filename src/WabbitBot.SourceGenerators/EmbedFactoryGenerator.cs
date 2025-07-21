using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace WabbitBot.SourceGenerators;

[Generator]
public class EmbedFactoryGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will be created on each generation pass
        context.RegisterForSyntaxNotifications(() => new EmbedSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Get the syntax receiver that was created during initialization
        if (context.SyntaxReceiver is not EmbedSyntaxReceiver receiver)
            return;

        // Find all classes marked with [GenerateEmbedFactory]
        var embedClasses = receiver.EmbedClasses;
        if (!embedClasses.Any())
            return;

        // Generate the factory code
        var sourceBuilder = new StringBuilder(@"
using DSharpPlus.Entities;
using WabbitBot.DiscBot.DSharpPlus.Embeds;

namespace WabbitBot.DiscBot.DSharpPlus.Generated;

/// <summary>
/// Factory for creating and managing Discord embeds. This class is generated at compile time.
/// </summary>
public static class EmbedFactories
{
    private static readonly Dictionary<Type, Func<BaseEmbed>> _embedFactories = new();
    
    static EmbedFactories()
    {
");

        // Add factory registrations for each embed class
        foreach (var embedClass in embedClasses)
        {
            var className = embedClass.Identifier.Text;
            sourceBuilder.AppendLine($"        _embedFactories[typeof({className})] = () => new {className}();");
        }

        sourceBuilder.Append(@"
    }
    
    /// <summary>
    /// Creates a new instance of the specified embed type.
    /// </summary>
    /// <typeparam name=""T"">The type of embed to create. Must inherit from BaseEmbed.</typeparam>
    /// <returns>A new instance of the specified embed type.</returns>
    /// <exception cref=""ArgumentException"">Thrown when no factory is registered for the specified type.</exception>
    public static T CreateEmbed<T>() where T : BaseEmbed 
    {
        if (_embedFactories.TryGetValue(typeof(T), out var factory)) 
        {
            return (T)factory();
        }
        throw new ArgumentException($""No factory registered for embed type {typeof(T)}"");
    }
    
    /// <summary>
    /// Converts an embed to a DiscordEmbedBuilder for sending to Discord.
    /// </summary>
    /// <typeparam name=""T"">The type of embed to convert. Must inherit from BaseEmbed.</typeparam>
    /// <param name=""embed"">The embed to convert.</param>
    /// <returns>A DiscordEmbedBuilder that can be sent to Discord.</returns>
    /// <exception cref=""ArgumentNullException"">Thrown when the embed is null.</exception>
    public static DiscordEmbedBuilder CreateDiscordEmbed<T>(T embed) where T : BaseEmbed 
    {
        if (embed == null)
            throw new ArgumentNullException(nameof(embed), ""Embed cannot be null"");
            
        return embed.ToEmbedBuilder();
    }

    /// <summary>
    /// Creates and sends an embed to a Discord channel.
    /// </summary>
    /// <typeparam name=""T"">The type of embed to create and send. Must inherit from BaseEmbed.</typeparam>
    /// <param name=""channel"">The Discord channel to send the embed to.</param>
    /// <param name=""configureEmbed"">Action to configure the embed before sending.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref=""ArgumentNullException"">Thrown when the channel or configureEmbed action is null.</exception>
    public static async Task SendEmbedAsync<T>(DiscordChannel channel, Action<T> configureEmbed) 
        where T : BaseEmbed
    {
        if (channel == null)
            throw new ArgumentNullException(nameof(channel), ""Channel cannot be null"");
        if (configureEmbed == null)
            throw new ArgumentNullException(nameof(configureEmbed), ""Configure action cannot be null"");

        var embed = CreateEmbed<T>();
        configureEmbed(embed);
        await channel.SendMessageAsync(CreateDiscordEmbed(embed));
    }
}");

        // Add the generated source to the compilation
        context.AddSource("EmbedFactories.g.cs", sourceBuilder.ToString());
    }
}

/// <summary>
/// Syntax receiver that finds all classes marked with [GenerateEmbedFactory]
/// </summary>
public class EmbedSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> EmbedClasses { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        // Look for class declarations
        if (syntaxNode is ClassDeclarationSyntax classDeclaration)
        {
            // Check if the class has the [GenerateEmbedFactory] attribute
            if (classDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(a => a.Name.ToString() == "GenerateEmbedFactory"))
            {
                // Verify the class inherits from BaseEmbed
                if (classDeclaration.BaseList?.Types
                    .Any(t => t.Type.ToString() == "BaseEmbed") == true)
                {
                    EmbedClasses.Add(classDeclaration);
                }
            }
        }
    }
}
