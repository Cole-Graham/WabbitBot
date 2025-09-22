namespace WabbitBot.SourceGenerators.Templates;

/// <summary>
/// Template for generating embed factory code
/// </summary>
public static class EmbedTemplate
{
    /// <summary>
    /// Template for embed factory class
    /// </summary>
    public const string EmbedFactoryClass = @"
using DSharpPlus.Entities;
using WabbitBot.DiscBot.DSharpPlus.Embeds;

namespace WabbitBot.DiscBot.DSharpPlus.Generated;

/// <summary>
/// Factory for creating and managing Discord embeds. This class is generated at compile time.
/// </summary>
public static class EmbedFactories
{{
    private static readonly Dictionary<Type, Func<BaseEmbed>> _embedFactories = new();
    
    static EmbedFactories()
    {{
{0}
    }}
    
    /// <summary>
    /// Creates a new instance of the specified embed type.
    /// </summary>
    /// <typeparam name=""T"">The type of embed to create. Must inherit from BaseEmbed.</typeparam>
    /// <returns>A new instance of the specified embed type.</returns>
    /// <exception cref=""ArgumentException"">Thrown when no factory is registered for the specified type.</exception>
    public static T CreateEmbed<T>() where T : BaseEmbed 
    {{
        if (_embedFactories.TryGetValue(typeof(T), out var factory)) 
        {{
            return (T)factory();
        }}
        throw new ArgumentException($""No factory registered for embed type {{typeof(T)}}"");
    }}
    
    /// <summary>
    /// Converts an embed to a DiscordEmbedBuilder for sending to Discord.
    /// </summary>
    /// <typeparam name=""T"">The type of embed to convert. Must inherit from BaseEmbed.</typeparam>
    /// <param name=""embed"">The embed to convert.</param>
    /// <returns>A DiscordEmbedBuilder that can be sent to Discord.</returns>
    /// <exception cref=""ArgumentNullException"">Thrown when the embed is null.</exception>
    public static DiscordEmbedBuilder CreateDiscordEmbed<T>(T embed) where T : BaseEmbed 
    {{
        if (embed == null)
            throw new ArgumentNullException(nameof(embed), ""Embed cannot be null"");
            
        return embed.ToEmbedBuilder();
    }}

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
    {{
        if (channel == null)
            throw new ArgumentNullException(nameof(channel), ""Channel cannot be null"");
        if (configureEmbed == null)
            throw new ArgumentNullException(nameof(configureEmbed), ""Configure action cannot be null"");

        var embed = CreateEmbed<T>();
        configureEmbed(embed);
        await channel.SendMessageAsync(CreateDiscordEmbed(embed));
    }}
}}";

    /// <summary>
    /// Template for individual embed factory registration
    /// </summary>
    public const string EmbedFactoryRegistration = @"
        _embedFactories[typeof({0})] = () => new {0}();";

    /// <summary>
    /// Template for embed class
    /// </summary>
    public const string EmbedClass = @"
using DSharpPlus.Entities;
using WabbitBot.DiscBot.DSharpPlus.Embeds;
using WabbitBot.SourceGenerators.Attributes;

namespace {0}
{{
    [GenerateEmbedFactory]
    public class {1} : BaseEmbed
    {{
        public override DiscordEmbedBuilder ToEmbedBuilder()
        {{
            var builder = new DiscordEmbedBuilder()
                .WithTitle(""{2}"")
                .WithDescription(""{3}"")
                .WithColor(DiscordColor.Blue)
                .WithTimestamp(DateTimeOffset.UtcNow);

            return builder;
        }}
    }}
}}";
}
