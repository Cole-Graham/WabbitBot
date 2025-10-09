using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using WabbitBot.SourceGenerators.Utils;

namespace WabbitBot.SourceGenerators.Templates;

/// <summary>
/// Templates for generating embed-related code.
/// </summary>
public static class EmbedTemplates
{
    /// <summary>
    /// Generates the complete embed factory class.
    /// </summary>
    public static SourceText GenerateFactory(IEnumerable<string> classNames)
    {
        var factoryRegistrations = string.Join(
            "\n",
            classNames.Select(cn => $"        _embedFactories[typeof({cn})] = () => new {cn}();")
        );

        var content = $$"""
            {{CommonTemplates.CreateFileHeader("EmbedFactoryGenerator")}}
            using DSharpPlus.Entities;
            using WabbitBot.DiscBot.DSharpPlus.Embeds;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            namespace WabbitBot.DiscBot.DSharpPlus.Generated;

            /// <summary>
            /// Factory for creating and managing Discord embeds. Generated at compile time.
            /// </summary>
            public static class EmbedFactories
            {
                private static readonly Dictionary<Type, Func<BaseEmbed>> _embedFactories = new();

                static EmbedFactories()
                {
            {{factoryRegistrations}}
                }

                /// <summary>
                /// Creates a new instance of the specified embed type.
                /// </summary>
                public static T CreateEmbed<T>() where T : BaseEmbed
                {
                    if (_embedFactories.TryGetValue(typeof(T), out var factory))
                        return (T)factory();
                    throw new ArgumentException($"No factory registered for embed type {typeof(T)}");
                }

                /// <summary>
                /// Converts an embed to a DiscordEmbedBuilder.
                /// </summary>
                public static DiscordEmbedBuilder CreateDiscordEmbed(BaseEmbed embed)
                {
                    {{CommonTemplates.CreateNullCheck("embed")}}
                    return embed.ToEmbedBuilder(); // Assume BaseEmbed has this method
                }

                /// <summary>
                /// Creates and sends an embed to a Discord channel.
                /// </summary>
                public static async Task SendEmbedAsync<T>(DiscordChannel channel, Action<T> configureEmbed) where T : BaseEmbed
                {
                    {{CommonTemplates.CreateNullCheck("channel")}}
                    {{CommonTemplates.CreateNullCheck("configureEmbed")}}

                    var embed = CreateEmbed<T>();
                    configureEmbed(embed);
                    await channel.SendMessageAsync(CreateDiscordEmbed(embed));
                }
            }
            """;

        return content.ToSourceText();
    }
}
