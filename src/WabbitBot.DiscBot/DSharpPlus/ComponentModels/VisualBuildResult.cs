using DSharpPlus.Entities;

namespace WabbitBot.DiscBot.DSharpPlus.ComponentModels;

/// <summary>
/// Result of building a visual component from a POCO model.
/// Supports both modern Container pattern and legacy Embed pattern.
/// </summary>
public record VisualBuildResult
{
    /// <summary>
    /// Modern container component (primary UI pattern).
    /// Supports rich layouts, interactive elements, and theming.
    /// Null if using Embed pattern instead.
    /// </summary>
    public DiscordContainerComponent? Container { get; init; }

    /// <summary>
    /// Legacy embed component (simple interaction responses only).
    /// Reserved for future simple displays per Discord best practices.
    /// Currently not in use - all displays use Container pattern.
    /// Null if using Container pattern instead.
    /// </summary>
    public DiscordEmbed? Embed { get; init; }

    /// <summary>
    /// Optional file attachment hint for assets (e.g., map thumbnails, deck images).
    /// When present, the Renderer should attach the file and reference it via attachment:// URL.
    /// </summary>
    public AttachmentHint? Attachment { get; init; }

    /// <summary>
    /// Creates a Container-based visual result (current standard).
    /// </summary>
    /// <param name="container">The Discord container component to display</param>
    /// <param name="attachment">Optional file attachment hint</param>
    /// <returns>A visual build result with Container set</returns>
    public static VisualBuildResult FromContainer(
        DiscordContainerComponent container,
        AttachmentHint? attachment = null)
    {
        return new VisualBuildResult
        {
            Container = container,
            Embed = null,
            Attachment = attachment,
        };
    }

    /// <summary>
    /// Creates an Embed-based visual result (future simple responses).
    /// </summary>
    /// <param name="embed">The Discord embed to display</param>
    /// <param name="attachment">Optional file attachment hint</param>
    /// <returns>A visual build result with Embed set</returns>
    public static VisualBuildResult FromEmbed(
        DiscordEmbed embed,
        AttachmentHint? attachment = null)
    {
        return new VisualBuildResult
        {
            Container = null,
            Embed = embed,
            Attachment = attachment,
        };
    }
}

/// <summary>
/// Hint for an asset that should be attached to a Discord message.
/// Used when CDN URLs are not available and files must be uploaded as attachments.
/// </summary>
public record AttachmentHint
{
    /// <summary>
    /// Canonical filename of the asset (e.g., "map_thumbnail_01.jpg", "deck_wabbit_fire.png").
    /// This is the stable identifier used to reference the asset in the FileSystemService.
    /// NOT an internal filesystem path - just the filename.
    /// </summary>
    public required string CanonicalFileName { get; init; }

    /// <summary>
    /// MIME content type of the asset (e.g., "image/jpeg", "image/png").
    /// Used for proper content type headers when uploading to Discord.
    /// </summary>
    public string ContentType { get; init; } = "application/octet-stream";

    /// <summary>
    /// Creates an attachment hint for an image file.
    /// Automatically infers content type from file extension.
    /// </summary>
    /// <param name="canonicalFileName">The canonical filename (e.g., "thumbnail.jpg")</param>
    /// <returns>An attachment hint with appropriate content type</returns>
    public static AttachmentHint ForImage(string canonicalFileName)
    {
        var extension = Path.GetExtension(canonicalFileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg", // Default to JPEG for unknown image types
        };

        return new AttachmentHint
        {
            CanonicalFileName = canonicalFileName,
            ContentType = contentType,
        };
    }
}

