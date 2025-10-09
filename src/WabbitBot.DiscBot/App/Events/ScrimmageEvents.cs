using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.DiscBot.App.Events
{
    public record ScrimMatchThreadCreateRequested(
        Guid MatchId,
        Guid ScrimmageId
    ) : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}

