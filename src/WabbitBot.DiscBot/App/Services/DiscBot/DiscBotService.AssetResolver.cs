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
            /// <param name="assetId">Canonical asset ID (e.g., map name, division rank)</param>
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
                        await DiscBotService.ErrorHandler.CaptureAsync(
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

                    // Fall back to local path - create attachment hint
                    if (!string.IsNullOrEmpty(response.CanonicalFileName))
                    {
                        var hint = AttachmentHint.ForImage(response.CanonicalFileName);
                        return (null, hint);
                    }

                    // Asset not found
                    return (null, null);
                }
                catch (Exception ex)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
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
            /// <param name="divisionRank">Division rank (e.g., "bronze", "silver", "gold")</param>
            /// <returns>Tuple of (cdnUrl, attachmentHint)</returns>
            public static Task<(string? cdnUrl, AttachmentHint? attachmentHint)> ResolveDivisionIconAsync(
                string divisionRank
            )
            {
                return ResolveAssetAsync("divisionicon", divisionRank);
            }
        }
    }
}
