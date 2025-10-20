using WabbitBot.Common.Events;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// CDN metadata management for FileSystemService.
///
/// NOTE: CDN metadata is stored in-memory only and will be lost on bot restart.
/// This is acceptable because:
/// - The cache repopulates naturally as files are uploaded
/// - Discord CDN URLs may expire anyway (requiring re-upload)
/// - Persistence would add complexity for marginal benefit
///
/// If persistence becomes necessary, consider adding a database table or JSON file storage.
/// </summary>
public partial class FileSystemService
{
    /// <summary>
    /// Records a CDN URL for a canonical filename.
    /// Used when DiscBot uploads a file and reports the CDN URL.
    /// This cache is in-memory only and will be lost on restart.
    /// </summary>
    /// <param name="canonicalFileName">The canonical filename</param>
    /// <param name="cdnUrl">The Discord CDN URL</param>
    /// <param name="messageId">Optional Discord message ID</param>
    /// <param name="channelId">Optional Discord channel ID</param>
    public void RecordCdnMetadata(
        string canonicalFileName,
        string cdnUrl,
        ulong? messageId = null,
        ulong? channelId = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cdnUrl);

        var metadata = new CdnMetadata(cdnUrl, messageId, channelId, DateTime.UtcNow);

        lock (_cdnMetadataCache)
        {
            // Last-write-wins for idempotency
            _cdnMetadataCache[canonicalFileName] = metadata;
        }
    }

    /// <summary>
    /// Retrieves CDN metadata for a canonical filename.
    /// </summary>
    /// <param name="canonicalFileName">The canonical filename</param>
    /// <returns>CDN metadata if available, null otherwise</returns>
    public CdnMetadata? GetCdnMetadata(string canonicalFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalFileName);

        lock (_cdnMetadataCache)
        {
            return _cdnMetadataCache.TryGetValue(canonicalFileName, out var metadata) ? metadata : null;
        }
    }

    /// <summary>
    /// Resolves an asset to a usable URL or file path.
    /// Prefers CDN URL when available, otherwise returns full path for local upload.
    /// </summary>
    /// <param name="assetType">Type of asset (e.g., "MapThumbnail", "DivisionIcon")</param>
    /// <param name="assetId">Asset identifier (e.g., map ID, division name)</param>
    /// <returns>Asset resolution information</returns>
    public AssetResolved? ResolveAsset(string assetType, string assetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetType);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetId);

        string? fullPath = null;
        string? directory = null;

        // Determine the directory based on asset type
        switch (assetType.ToLowerInvariant())
        {
            case "mapthumbnail":
                directory = _thumbnailsDirectory;
                break;

            case "divisionicon":
                directory = _divisionIconsDirectory;
                break;

            default:
                return null;
        }

        // Find the actual file in the directory (don't assume extension)
        // Uses the shared helper method from the main FileSystemService partial class
        fullPath = FindFileByPattern(directory, $"{assetId}.*");

        if (fullPath is null)
        {
            return null; // No file found for this asset ID
        }

        var canonicalFileName = Path.GetFileName(fullPath);

        // Check for CDN metadata
        var cdnMetadata = GetCdnMetadata(canonicalFileName);

        return new AssetResolved(
            assetType,
            assetId,
            canonicalFileName,
            cdnMetadata?.CdnUrl, // Prefer CDN URL when available
            cdnMetadata is null ? fullPath : null, // Only provide full path if no CDN
            Guid.Empty
        ); // Correlation ID set by caller
    }
}
