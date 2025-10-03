using DSharpPlus;
using DSharpPlus.Entities;
using WabbitBot.Common.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.DSharpPlus.Utilities;

namespace WabbitBot.DiscBot.DSharpPlus.ComponentModels;

/// <summary>
/// Extension methods for Discord MessageBuilder to simplify working with VisualBuildResult.
/// Provides fluent API for adding containers, embeds, and attachments from visual models.
/// </summary>
/// <remarks>
/// Note: DSharpPlus BaseDiscordMessageBuilder already provides EnableV2Components() and AddContainerComponent().
/// See: https://dsharpplus.github.io/DSharpPlus/api/DSharpPlus.Entities.BaseDiscordMessageBuilder-1.html
/// </remarks>
public static class DiscordMessageBuilderExtensions
{
    /// <summary>
    /// Adds a visual component to the message builder.
    /// Automatically handles both Container and Embed patterns based on the result.
    /// Enforces URL policy: only HTTPS CDN URLs or attachment:// URIs are permitted.
    /// If the visual contains an AttachmentHint, loads and attaches the file.
    /// </summary>
    /// <param name="builder">The message builder to extend</param>
    /// <param name="visual">The visual build result containing container/embed and optional attachment</param>
    /// <returns>The message builder for fluent chaining</returns>
    /// <exception cref="InvalidOperationException">Thrown if visual contains invalid URLs</exception>
    public static async Task<DiscordMessageBuilder> WithVisual(
        this DiscordMessageBuilder builder,
        VisualBuildResult visual)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(visual);

        // Step 5l: Validate URLs before adding to builder
        ValidateVisualUrls(visual);

        // Add the visual component (Container or Embed)
        if (visual.Container is not null)
        {
            builder.AddContainerComponent(visual.Container);
        }
        else if (visual.Embed is not null)
        {
            builder.AddEmbed(visual.Embed);
        }
        else
        {
            throw new InvalidOperationException(
                "VisualBuildResult must have either Container or Embed set, but both were null.");
        }

        // Step 3i: Handle attachments when CDN URL not available
        if (visual.Attachment is not null)
        {
            await AddAttachmentAsync(builder, visual.Attachment);
        }

        return builder;
    }

    /// <summary>
    /// Validates all URLs in a visual component to ensure they comply with the asset URL policy.
    /// Policy: Only HTTPS CDN URLs or attachment:// URIs are permitted.
    /// Internal file paths are forbidden.
    /// </summary>
    /// <param name="visual">The visual build result to validate</param>
    /// <exception cref="InvalidOperationException">Thrown if any URL is invalid</exception>
    private static void ValidateVisualUrls(VisualBuildResult visual)
    {
        // Validate embed URLs (if using Embed pattern)
        if (visual.Embed is not null)
        {
            // Validate thumbnail URL
            if (!string.IsNullOrEmpty(visual.Embed.Thumbnail?.Url?.ToString()))
            {
                AssetUrlValidator.ValidateOrThrow(
                    visual.Embed.Thumbnail.Url.ToString(),
                    "Embed thumbnail URL");
            }

            // Validate image URL
            if (!string.IsNullOrEmpty(visual.Embed.Image?.Url?.ToString()))
            {
                AssetUrlValidator.ValidateOrThrow(
                    visual.Embed.Image.Url.ToString(),
                    "Embed image URL");
            }

            // Validate author icon URL
            if (!string.IsNullOrEmpty(visual.Embed.Author?.IconUrl?.ToString()))
            {
                AssetUrlValidator.ValidateOrThrow(
                    visual.Embed.Author.IconUrl.ToString(),
                    "Embed author icon URL");
            }

            // Validate footer icon URL
            if (!string.IsNullOrEmpty(visual.Embed.Footer?.IconUrl?.ToString()))
            {
                AssetUrlValidator.ValidateOrThrow(
                    visual.Embed.Footer.IconUrl.ToString(),
                    "Embed footer icon URL");
            }
        }

        // Container URL validation would go here when containers support image properties
        // Currently, containers use attachments via the AttachmentHint pattern instead
    }

    /// <summary>
    /// Loads and attaches a file to the message builder based on AttachmentHint.
    /// Step 3i: Renderer fallback for local file upload when CDN unavailable.
    /// </summary>
    /// <param name="builder">The message builder</param>
    /// <param name="hint">The attachment hint with canonical filename</param>
    private static async Task AddAttachmentAsync(DiscordMessageBuilder builder, AttachmentHint hint)
    {
        try
        {
            // For now, construct path using AppContext.BaseDirectory (both Core and DiscBot share it)
            // TODO: Future enhancement - request file bytes via event instead of direct file access
            var filePath = ResolveFilePath(hint.CanonicalFileName);

            if (!File.Exists(filePath))
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    new FileNotFoundException($"Attachment file not found: {filePath}"),
                    $"Failed to attach file: {hint.CanonicalFileName}",
                    nameof(AddAttachmentAsync));
                return;
            }

            // Load file and attach
            using var fileStream = File.OpenRead(filePath);
            builder.AddFile(hint.CanonicalFileName, fileStream, AddFileOptions.None);
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                $"Failed to attach file: {hint.CanonicalFileName}",
                nameof(AddAttachmentAsync));
        }
    }

    /// <summary>
    /// Resolves the full file path for a canonical filename.
    /// Both Core and DiscBot share AppContext.BaseDirectory, so we can construct paths.
    /// </summary>
    private static string ResolveFilePath(string canonicalFileName)
    {
        // Determine directory based on filename pattern
        string relativePath;

        if (canonicalFileName.Contains("thumbnail", StringComparison.OrdinalIgnoreCase))
        {
            relativePath = Path.Combine("data", "maps", "thumbnails", canonicalFileName);
        }
        else if (canonicalFileName.Contains("icon", StringComparison.OrdinalIgnoreCase))
        {
            relativePath = Path.Combine("data", "divisions", "icons", canonicalFileName);
        }
        else
        {
            // Default to data directory
            relativePath = Path.Combine("data", canonicalFileName);
        }

        return Path.Combine(AppContext.BaseDirectory, relativePath);
    }

    /// <summary>
    /// Determines the asset kind from the canonical filename for event metadata.
    /// </summary>
    private static string DetermineAssetKind(string canonicalFileName)
    {
        if (canonicalFileName.Contains("thumbnail", StringComparison.OrdinalIgnoreCase))
            return "mapthumbnail";
        if (canonicalFileName.Contains("icon", StringComparison.OrdinalIgnoreCase))
            return "divisionicon";
        return "unknown";
    }
}



