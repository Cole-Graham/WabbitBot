using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-internal events for map management - not forwarded to GlobalEventBus
/// </summary>
public partial class MapsSavedEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required List<Map> Maps { get; init; }
}

public partial class MapsExportedEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required string Path { get; init; }
}

public partial class MapsImportedEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required List<Map> Maps { get; init; }
}

public partial class MapAddedEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Map Map { get; init; }
}

public partial class MapUpdatedEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Map Map { get; init; }
    public required Map PreviousMap { get; init; }
}

public partial class MapRemovedEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Map Map { get; init; }
}

public partial class MapThumbnailUpdatedEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Map Map { get; init; }
    public string? OldFilename { get; init; }
    public required string NewFilename { get; init; }
}

public partial class MapThumbnailRemovedEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public required Map Map { get; init; }
    public string? OldFilename { get; init; }
}
