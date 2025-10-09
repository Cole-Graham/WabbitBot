namespace WabbitBot.DiscBot.App.Utilities
{
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
                _ => "image/jpeg",
            };

            return new AttachmentHint
            {
                CanonicalFileName = canonicalFileName,
                ContentType = contentType,
            };
        }
    }
}

