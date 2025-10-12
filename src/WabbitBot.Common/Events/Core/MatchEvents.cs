using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Events.Core;

/// <summary>
/// Core event indicating a match has been created.
/// Published locally within Core when match is created.
/// </summary>
[EventGenerator(
    pubTargetClass: "WabbitBot.Core.Common.Models.Common.MatchHandler",
    subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.ScrimmageHandler"]
)]
public record ScrimmageMatchCreated(Guid ScrimmageId, Guid MatchId) : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public EventBusType EventBusType { get; init; } = EventBusType.Global;
}
