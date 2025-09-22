using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

/// <summary>
/// Base class for all embeds in the bot. Provides common properties and functionality
/// for consistent styling and behavior across different types of embeds.
/// </summary>
[GenerateEmbedStyling]
public abstract class BaseEmbed
{
    /// <summary>
    /// The title of the embed. Should be concise and descriptive.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The description of the embed. Can contain multiple lines and formatting.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The color of the embed's side bar. Used to indicate status or type.
    /// </summary>
    public DiscordColor Color { get; set; } = DiscordColor.Blue;

    /// <summary>
    /// The timestamp shown in the embed footer. Usually when the embed was created/updated.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The footer text shown at the bottom of the embed.
    /// </summary>
    public string FooterText { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the footer icon.
    /// </summary>
    public string? FooterIconUrl { get; set; }

    /// <summary>
    /// The URL of the thumbnail image shown in the top right.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// The URL of the main image shown in the embed.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The author information shown at the top of the embed.
    /// </summary>
    public IEmbedAuthor? Author { get; set; }

    /// <summary>
    /// List of fields to display in the embed. Each field has a name and value.
    /// </summary>
    public IReadOnlyList<IEmbedField> Fields { get; protected set; } = new List<IEmbedField>();

    /// <summary>
    /// Converts the embed to a DiscordEmbedBuilder for sending to Discord.
    /// </summary>
    public virtual DiscordEmbedBuilder ToEmbedBuilder()
    {
        var builder = new DiscordEmbedBuilder()
            .WithTitle(Title)
            .WithDescription(Description)
            .WithColor(Color)
            .WithTimestamp(Timestamp);

        if (!string.IsNullOrEmpty(FooterText))
        {
            builder.WithFooter(FooterText, FooterIconUrl);
        }

        if (!string.IsNullOrEmpty(ThumbnailUrl))
        {
            builder.WithThumbnail(ThumbnailUrl);
        }

        if (!string.IsNullOrEmpty(ImageUrl))
        {
            builder.WithImageUrl(ImageUrl);
        }

        if (Author != null)
        {
            builder.WithAuthor(Author.Name, Author.IconUrl, Author.Url);
        }

        foreach (var field in Fields)
        {
            builder.AddField(field.Name, field.Value, field.IsInline);
        }

        return builder;
    }
}

/// <summary>
/// Represents a field in an embed.
/// </summary>
public class EmbedField : IEmbedField
{
    /// <summary>
    /// The name/title of the field.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The value/content of the field.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Whether the field should be displayed inline with other fields.
    /// </summary>
    public bool IsInline { get; set; }
}

/// <summary>
/// Represents the author information in an embed.
/// </summary>
public class EmbedAuthor : IEmbedAuthor
{
    /// <summary>
    /// The name of the author.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the author's icon.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// The URL that clicking the author's name will navigate to.
    /// </summary>
    public string? Url { get; set; }
}