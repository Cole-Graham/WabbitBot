using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-level events for file system operations.
/// These are infrastructure facts about file state changes, not database CRUD.
/// Published locally within Core for subscribers that need to react to file operations.
/// </summary>
/// <summary>
/// Published when a map thumbnail is successfully uploaded.
/// </summary>
/// <param name="CanonicalFileName">The secure filename assigned to the thumbnail</param>
/// <param name="OriginalFileName">The original filename from the upload</param>
/// <param name="FileSizeBytes">Size of the uploaded file in bytes</param>
public record ThumbnailUploadedEvent(string CanonicalFileName, string OriginalFileName, long FileSizeBytes) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}

/// <summary>
/// Published when a map thumbnail is successfully deleted.
/// </summary>
/// <param name="CanonicalFileName">The filename that was deleted</param>
public record ThumbnailDeletedEvent(string CanonicalFileName) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}

/// <summary>
/// Published when a division icon is successfully uploaded.
/// </summary>
/// <param name="CanonicalFileName">The secure filename assigned to the icon</param>
/// <param name="OriginalFileName">The original filename from the upload</param>
/// <param name="FileSizeBytes">Size of the uploaded file in bytes</param>
public record DivisionIconUploadedEvent(string CanonicalFileName, string OriginalFileName, long FileSizeBytes) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}

/// <summary>
/// Published when a division icon is successfully deleted.
/// </summary>
/// <param name="CanonicalFileName">The filename that was deleted</param>
public record DivisionIconDeletedEvent(string CanonicalFileName) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}

/// <summary>
/// Published when a custom Discord component image is successfully uploaded.
/// </summary>
/// <param name="CanonicalFileName">The canonical filename (e.g., "challenge_banner.jpg")</param>
/// <param name="FileSizeBytes">Size of the uploaded file in bytes</param>
public record DiscordComponentImageUploadedEvent(string CanonicalFileName, long FileSizeBytes) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}

/// <summary>
/// Published when a custom Discord component image is deleted (reverts to default).
/// </summary>
/// <param name="CanonicalFileName">The filename that was deleted</param>
public record DiscordComponentImageDeletedEvent(string CanonicalFileName) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
}
