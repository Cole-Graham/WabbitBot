using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Events;

/// <summary>
/// Request to resolve an asset (e.g., map thumbnail, division icon) to a usable URL or file path.
/// This is a Global request-response event for cross-boundary asset queries.
/// </summary>
/// <param name="AssetType">Type of asset being requested (e.g., "MapThumbnail", "DivisionIcon")</param>
/// <param name="AssetId">Identifier for the specific asset (e.g., map ID, division name)</param>
/// <param name="RequestId">Unique ID for request-response correlation</param>
public record AssetResolveRequested(string AssetType, string AssetId, Guid RequestId)
{
    public EventBusType EventBusType => EventBusType.Global;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Response containing resolved asset information.
/// Includes CDN URL (preferred) or relative path for local upload.
/// </summary>
/// <param name="AssetType">Type of asset resolved</param>
/// <param name="AssetId">Identifier for the asset</param>
/// <param name="CanonicalFileName">The canonical filename for the asset</param>
/// <param name="CdnUrl">Discord CDN URL if available (preferred)</param>
/// <param name="RelativePathUnderAppBase">Relative path under app base directory if CDN not available</param>
/// <param name="CorrelationId">ID matching the request (for request-response pattern)</param>
public record AssetResolved(
    string AssetType,
    string AssetId,
    string CanonicalFileName,
    string? CdnUrl,
    string? RelativePathUnderAppBase,
    Guid CorrelationId
)
{
    public EventBusType EventBusType => EventBusType.Global;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Request to ingest a file from a temporary location into Core's managed file system.
/// Used when DiscBot downloads an attachment and needs Core to validate/store it.
/// </summary>
/// <param name="TempFilePath">Temporary file path (DiscBot-local)</param>
/// <param name="AssetKind">Kind of asset (e.g., "MapThumbnail", "ReplayFile")</param>
/// <param name="Metadata">Additional metadata (e.g., original filename, uploader ID)</param>
/// <param name="RequestId">Unique ID for request-response correlation</param>
public record FileIngestRequested(
    string TempFilePath,
    string AssetKind,
    Dictionary<string, string> Metadata,
    Guid RequestId
)
{
    public EventBusType EventBusType => EventBusType.Global;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Response indicating successful file ingest.
/// Provides the canonical filename and CDN URL if available.
/// </summary>
/// <param name="CanonicalFileName">The canonical filename assigned by Core</param>
/// <param name="AssetKind">Kind of asset that was ingested</param>
/// <param name="CdnUrl">Discord CDN URL if available</param>
/// <param name="Metadata">Metadata from the request</param>
/// <param name="CorrelationId">ID matching the request</param>
public record FileIngested(
    string CanonicalFileName,
    string AssetKind,
    string? CdnUrl,
    Dictionary<string, string> Metadata,
    Guid CorrelationId
)
{
    public EventBusType EventBusType => EventBusType.Global;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Reports a CDN URL for a canonical file after Discord has uploaded it.
/// This allows Core to update its CDN metadata mapping.
/// Fire-and-forget event (not request-response).
/// </summary>
/// <param name="CanonicalFileName">The canonical filename</param>
/// <param name="CdnUrl">The Discord CDN URL</param>
/// <param name="SourceMessageId">The Discord message ID where the file was uploaded</param>
/// <param name="ChannelId">The channel where the message was sent</param>
public record FileCdnLinkReported(string CanonicalFileName, string CdnUrl, ulong SourceMessageId, ulong ChannelId)
{
    public EventBusType EventBusType => EventBusType.Global;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
