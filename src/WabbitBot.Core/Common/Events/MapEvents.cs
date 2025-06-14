using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Events;

public class MapsSavedEvent
{
    public required List<Map> Maps { get; init; }
}

public class MapsExportedEvent
{
    public required string Path { get; init; }
}

public class MapsImportedEvent
{
    public required List<Map> Maps { get; init; }
}

public class MapAddedEvent
{
    public required Map Map { get; init; }
}

public class MapUpdatedEvent
{
    public required Map Map { get; init; }
    public required Map PreviousMap { get; init; }
}

public class MapRemovedEvent
{
    public required Map Map { get; init; }
}

public class MapThumbnailUpdatedEvent
{
    public required Map Map { get; init; }
    public string? OldFilename { get; init; }
    public required string NewFilename { get; init; }
}

public class MapThumbnailRemovedEvent
{
    public required Map Map { get; init; }
    public string? OldFilename { get; init; }
}