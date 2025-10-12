using DSharpPlus.Entities;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;

namespace WabbitBot.DiscBot.App.Utilities;

/// <summary>
/// Static styling utilities for Discord embeds.
/// Provides consistent styling, colors, and formatting across all embeds.
/// </summary>
public static class EmbedStyling
{
    #region Universal Styling Utilities

    /// <summary>
    /// Gets the default embed color for general use
    /// </summary>
    public static DiscordColor GetDefaultColor() => new(66, 134, 244); // Blue

    /// <summary>
    /// Gets the challenge color for challenge messages
    /// </summary>
    public static DiscordColor GetChallengeColor() => new(255, 215, 0); // Gold

    /// <summary>
    /// Gets the success color for positive actions
    /// </summary>
    public static DiscordColor GetSuccessColor() => new(76, 175, 80); // Green

    /// <summary>
    /// Gets the warning color for cautionary messages
    /// </summary>
    public static DiscordColor GetWarningColor() => new(255, 152, 0); // Orange

    /// <summary>
    /// Gets the error color for failures and errors
    /// </summary>
    public static DiscordColor GetErrorColor() => new(244, 67, 54); // Red

    /// <summary>
    /// Gets the info color for informational messages
    /// </summary>
    public static DiscordColor GetInfoColor() => new(33, 150, 243); // Light Blue

    /// <summary>
    /// Creates a standard embed author
    /// </summary>
    public static DiscordEmbedBuilder.EmbedAuthor CreateStandardAuthor(
        string name,
        string? iconUrl = null,
        string? url = null
    )
    {
        return new DiscordEmbedBuilder.EmbedAuthor
        {
            Name = name,
            IconUrl = iconUrl,
            Url = url,
        };
    }

    /// <summary>
    /// Creates a standard embed footer
    /// </summary>
    public static DiscordEmbedBuilder.EmbedFooter CreateStandardFooter(string text, string? iconUrl = null)
    {
        return new DiscordEmbedBuilder.EmbedFooter { Text = text, IconUrl = iconUrl };
    }

    #endregion

    #region Formatting Utilities

    /// <summary>
    /// Formats a score with appropriate styling
    /// </summary>
    public static string FormatScore(int score) => $"**{score}**";

    /// <summary>
    /// Formats a username with Discord mention styling
    /// </summary>
    public static string FormatUsername(DiscordUser user) => user.Mention;

    /// <summary>
    /// Formats a username with Discord mention styling from ID
    /// </summary>
    public static string FormatUsername(ulong userId) => $"<@{userId}>";

    /// <summary>
    /// Formats a channel with Discord channel mention styling
    /// </summary>
    public static string FormatChannel(DiscordChannel channel) => channel.Mention;

    /// <summary>
    /// Formats a channel with Discord channel mention styling from ID
    /// </summary>
    public static string FormatChannel(ulong channelId) => $"<#{channelId}>";

    /// <summary>
    /// Formats a role with Discord role mention styling
    /// </summary>
    public static string FormatRole(DiscordRole role) => role.Mention;

    /// <summary>
    /// Formats a role with Discord role mention styling from ID
    /// </summary>
    public static string FormatRole(ulong roleId) => $"<@&{roleId}>";

    /// <summary>
    /// Formats text as inline code
    /// </summary>
    public static string FormatInlineCode(string text) => $"`{text}`";

    /// <summary>
    /// Formats text as code block
    /// </summary>
    public static string FormatCodeBlock(string text, string language = "") => $"```{language}\n{text}\n```";

    /// <summary>
    /// Formats text as bold
    /// </summary>
    public static string FormatBold(string text) => $"**{text}**";

    /// <summary>
    /// Formats text as italic
    /// </summary>
    public static string FormatItalic(string text) => $"*{text}*";

    /// <summary>
    /// Formats text as strikethrough
    /// </summary>
    public static string FormatStrikethrough(string text) => $"~~{text}~~";

    /// <summary>
    /// Formats text as underline
    /// </summary>
    public static string FormatUnderline(string text) => $"__{text}__";

    /// <summary>
    /// Creates a hyperlink
    /// </summary>
    public static string FormatLink(string text, string url) => $"[{text}]({url})";

    #endregion

    #region Match-Specific Styling

    /// <summary>
    /// Gets the color for a match status
    /// </summary>
    public static DiscordColor GetMatchStatusColor(MatchStatus status) =>
        status switch
        {
            MatchStatus.Created => GetDefaultColor(),
            MatchStatus.InProgress => GetInfoColor(),
            MatchStatus.Completed => GetSuccessColor(),
            MatchStatus.Cancelled => GetWarningColor(),
            MatchStatus.Forfeited => GetErrorColor(),
            _ => GetDefaultColor(),
        };

    /// <summary>
    /// Gets the display name for a match status
    /// </summary>
    public static string GetMatchStatusDisplayName(MatchStatus status) =>
        status switch
        {
            MatchStatus.Created => "⏳ Created",
            MatchStatus.InProgress => "⚡ In Progress",
            MatchStatus.Completed => "✅ Completed",
            MatchStatus.Cancelled => "🚫 Cancelled",
            MatchStatus.Forfeited => "🏳️ Forfeited",
            _ => "❓ Unknown",
        };

    #endregion

    #region Scrimmage-Specific Styling

    /// <summary>
    /// Gets the display name for a scrimmage status
    /// </summary>
    public static string GetScrimmageStatusDisplayName(ScrimmageStatus status) =>
        status switch
        {
            ScrimmageStatus.Accepted => "🟢 Accepted",
            ScrimmageStatus.Declined => "🚫 Declined",
            ScrimmageStatus.InProgress => "⚡ In Progress",
            ScrimmageStatus.Completed => "✅ Completed",
            ScrimmageStatus.Cancelled => "🚫 Cancelled",
            ScrimmageStatus.Forfeited => "🏳️ Forfeited",
            _ => "❓ Unknown",
        };

    /// <summary>
    /// Formats a rating value with appropriate styling
    /// </summary>
    public static string FormatRating(double rating) => FormatBold($"{rating:F0}");

    /// <summary>
    /// Formats a rating change with color and sign
    /// </summary>
    public static string FormatRatingChange(double change)
    {
        if (change > 0)
            return FormatBold($"+{change:F1}");
        if (change < 0)
            return FormatBold($"{change:F1}");
        return FormatBold("±0.0");
    }

    #endregion
}
