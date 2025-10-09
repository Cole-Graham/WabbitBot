namespace WabbitBot.DiscBot.App.Utilities
{
    /// <summary>
    /// Validates URLs used in Discord embeds and containers to prevent
    /// accidental exposure of internal file paths.
    ///
    /// Policy: Only CDN URLs (https://) or attachment:// URIs are permitted.
    /// Internal file paths (C:\, /var/www, etc.) are forbidden.
    /// </summary>
    public static class AssetUrlValidator
    {
        /// <summary>
        /// Validates that a URL is safe for use in Discord embeds/containers.
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <returns>True if the URL is safe, false otherwise</returns>
        public static bool IsValidAssetUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            // Allow attachment:// URIs (Discord's attachment reference scheme)
            if (url.StartsWith("attachment://", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Only allow HTTPS URLs (CDN, external hosting)
            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Additional validation: ensure it's a well-formed URI
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            // Ensure the scheme is https (redundant but explicit)
            if (uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates a URL and throws an exception if it's invalid.
        /// Use this in renderers when building visual components.
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <param name="context">Optional context for the error message (e.g., "thumbnail URL", "deck image")</param>
        /// <exception cref="InvalidOperationException">Thrown if the URL is invalid</exception>
        public static void ValidateOrThrow(string? url, string context = "Asset URL")
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException(
                    $"{context} cannot be null or empty. Use attachment:// or CDN URLs only."
                );
            }

            if (!IsValidAssetUrl(url))
            {
                // Determine the specific error
                if (
                    url.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                    || url.Contains(":\\", StringComparison.Ordinal)
                    || url.StartsWith("/", StringComparison.Ordinal)
                )
                {
                    throw new InvalidOperationException(
                        $"{context} appears to be an internal file path: {url}. "
                            + "Only HTTPS CDN URLs or attachment:// URIs are permitted in Discord embeds/containers."
                    );
                }

                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"{context} uses insecure HTTP: {url}. Only HTTPS URLs are permitted."
                    );
                }

                throw new InvalidOperationException(
                    $"{context} is invalid: {url}. Only HTTPS CDN URLs or attachment:// URIs are permitted."
                );
            }
        }

        /// <summary>
        /// Checks if a URL is an attachment reference (attachment://).
        /// </summary>
        /// <param name="url">The URL to check</param>
        /// <returns>True if it's an attachment reference</returns>
        public static bool IsAttachmentUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(url)
                && url.StartsWith("attachment://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a URL is a CDN URL (https://).
        /// </summary>
        /// <param name="url">The URL to check</param>
        /// <returns>True if it's a CDN URL</returns>
        public static bool IsCdnUrl(string? url)
        {
            return !string.IsNullOrWhiteSpace(url)
                && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}
