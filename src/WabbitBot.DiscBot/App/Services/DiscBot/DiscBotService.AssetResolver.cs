using WabbitBot.Common.Events;
using WabbitBot.DiscBot.App.Utilities;

namespace WabbitBot.DiscBot.App.Services.DiscBot
{
    public static partial class DiscBotService
    {
        public static class AssetResolver
        {
            /// <summary>
            /// Resolves an asset by type and ID, returning either a CDN URL or an AttachmentHint.
            /// Uses GlobalEventBus request-response pattern to query Core's FileSystemService.
            /// </summary>
            /// <param name="assetType">Type of asset (e.g., "mapthumbnail", "divisionicon")</param>
            /// <param name="assetId">Canonical asset ID (e.g., map name, division name)</param>
            /// <param name="timeout">Optional timeout for the request (default: 5 seconds)</param>
            /// <returns>
            /// Tuple of (cdnUrl, attachmentHint):
            /// - If CDN URL is available: (cdnUrl, null)
            /// - If only local path: (null, attachmentHint)
            /// - If asset not found: (null, null)
            /// </returns>
            public static async Task<(string? cdnUrl, AttachmentHint? attachmentHint)> ResolveAssetAsync(
                string assetType,
                string assetId,
                TimeSpan? timeout = null
            )
            {
                try
                {
                    // Get the global event bus
                    var globalBus = GlobalEventBusProvider.GetGlobalEventBus();

                    // Create request with unique request ID
                    var request = new AssetResolveRequested(assetType, assetId, Guid.NewGuid());

                    // Send request and wait for response
                    var response = await globalBus.RequestAsync<AssetResolveRequested, AssetResolved>(
                        request,
                        timeout ?? TimeSpan.FromSeconds(5)
                    );

                    if (response is null)
                    {
                        // Timeout or no response
                        await ErrorHandler.CaptureAsync(
                            new TimeoutException($"Asset resolution timed out for {assetType}/{assetId}"),
                            $"Failed to resolve asset: {assetType}/{assetId}",
                            nameof(ResolveAssetAsync)
                        );
                        return (null, null);
                    }

                    // Prefer CDN URL if available
                    if (!string.IsNullOrEmpty(response.CdnUrl))
                    {
                        return (response.CdnUrl, null);
                    }

                    // Fall back to local path - create attachment hint with relative path
                    if (!string.IsNullOrEmpty(response.CanonicalFileName))
                    {
                        var hint = AttachmentHint.ForImage(
                            response.CanonicalFileName,
                            response.RelativePathUnderAppBase
                        );
                        return (null, hint);
                    }

                    // Asset not found
                    return (null, null);
                }
                catch (Exception ex)
                {
                    await ErrorHandler.CaptureAsync(
                        ex,
                        $"Failed to resolve asset: {assetType}/{assetId}",
                        nameof(ResolveAssetAsync)
                    );
                    return (null, null);
                }
            }

            /// <summary>
            /// Resolves a map thumbnail, returning either a CDN URL or an AttachmentHint.
            /// </summary>
            /// <param name="mapName">Name of the map</param>
            /// <returns>Tuple of (cdnUrl, attachmentHint)</returns>
            public static Task<(string? cdnUrl, AttachmentHint? attachmentHint)> ResolveMapThumbnailAsync(
                string mapName
            )
            {
                return ResolveAssetAsync("mapthumbnail", mapName);
            }

            /// <summary>
            /// Resolves a division icon, returning either a CDN URL or an AttachmentHint.
            /// </summary>
            /// <param name="divisionName">Division name (e.g., "US 3rd Armored", "US 8th Infantry", etc.)</param>
            /// <returns>Tuple of (cdnUrl, attachmentHint)</returns>
            public static Task<(string? cdnUrl, AttachmentHint? attachmentHint)> ResolveDivisionIconAsync(
                string divisionName
            )
            {
                return ResolveAssetAsync("divisionicon", divisionName);
            }

            /// <summary>
            /// Resolves a game banner, returning either a CDN URL or an AttachmentHint.
            /// </summary>
            /// <param name="bannerFileName">Banner filename (e.g., "game_1_banner.jpg")</param>
            /// <returns>Tuple of (cdnUrl, attachmentHint)</returns>
            public static Task<(string? cdnUrl, AttachmentHint? attachmentHint)> ResolveGameBannerAsync(
                string bannerFileName
            )
            {
                return ResolveAssetAsync("gamebanner", bannerFileName);
            }

            /// <summary>
            /// Resolves a Discord component image (banner, header, etc.), returning either a CDN URL or file path.
            /// Checks for custom images first, then falls back to defaults from data/images/default/discord.
            /// </summary>
            /// <param name="imageName">The canonical image filename (e.g., "challenge_banner.jpg", "match_banner.jpg")</param>
            /// <returns>Tuple of (urlOrPath, attachmentHint, isCdnUrl)</returns>
            public static (
                string? urlOrPath,
                AttachmentHint? attachmentHint,
                bool isCdnUrl
            ) ResolveDiscordComponentImage(string imageName)
            {
                try
                {
                    Console.WriteLine($"🔍 DEBUG: AssetResolver.ResolveDiscordComponentImage called with: {imageName}");
                    Console.WriteLine($"🔍 DEBUG: FileSystem service: {FileSystem}");

                    // Use FileSystemService to get the image (CDN URL or local path)
                    var result = FileSystem.GetDiscordComponentImage(imageName);
                    Console.WriteLine($"🔍 DEBUG: FileSystem.GetDiscordComponentImage returned: '{result}'");

                    if (result is null)
                    {
                        return (null, null, false);
                    }

                    // If result is a URL (starts with http/https), return it directly
                    if (
                        result.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                        || result.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        return (result, null, true);
                    }

                    // Otherwise, it's a local file path - create attachment hint for upload
                    // Extract relative path from full path by removing AppContext.BaseDirectory
                    var relativePath = result.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase)
                        ? result[AppContext.BaseDirectory.Length..]
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        : result;

                    var hint = AttachmentHint.ForImage(imageName, relativePath);
                    return (result, hint, false);
                }
                catch (Exception ex)
                {
                    _ = ErrorHandler.CaptureAsync(
                        ex,
                        $"Failed to resolve Discord component image: {imageName}",
                        nameof(ResolveDiscordComponentImage)
                    );
                    return (null, null, false);
                }
            }
        }
    }
}
