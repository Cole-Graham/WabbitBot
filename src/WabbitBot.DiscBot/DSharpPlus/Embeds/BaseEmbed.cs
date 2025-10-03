using DSharpPlus.Entities;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

/// <summary>
/// Base class for embed visual models (legacy pattern, being phased out).
/// New embed models should use POCO visual models instead.
/// This stub exists to allow generated code to compile during transition.
/// </summary>
[Obsolete("Use POCO visual models instead. This base class will be removed in Step 6.")]
public abstract class BaseEmbed
{
    /// <summary>
    /// Converts this embed model to a DiscordEmbedBuilder.
    /// </summary>
    public virtual DiscordEmbedBuilder ToEmbedBuilder()
    {
        // Minimal implementation for compilation
        return new DiscordEmbedBuilder();
    }
}

