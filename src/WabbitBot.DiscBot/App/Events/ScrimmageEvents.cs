using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.DiscBot.App.Events
{
    [EventGenerator(
        pubTargetClass: "WabbitBot.DiscBot.App.Handlers.ScrimmageHandler",
        subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.ScrimmageHandler"]
    )]
    public record ScrimmageThreadsCreated(
        Guid MatchId,
        ulong ScrimmageChannelId,
        ulong ChallengerThreadId,
        ulong OpponentThreadId
    ) : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
