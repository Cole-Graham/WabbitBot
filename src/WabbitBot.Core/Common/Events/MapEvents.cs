using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Events;

/// <summary>
/// Core-internal events for map management - not forwarded to GlobalEventBus
/// </summary>
public partial record MapsSavedEvent(
    List<Map> Maps,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default) : IEvent;

public partial record MapsExportedEvent(
    string Path,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default) : IEvent;

public partial record MapsImportedEvent(
    List<Map> Maps,
    EventBusType EventBusType = EventBusType.Core,
    Guid EventId = default,
    DateTime Timestamp = default) : IEvent;

// CRUD events removed: MapAddedEvent, MapUpdatedEvent, MapRemovedEvent, MapThumbnailUpdatedEvent, MapThumbnailRemovedEvent
// These were database operations and violate the critical principle that events are not for CRUD.
