using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Events.Core
{
    [EventGenerator(
        pubTargetClass: "WabbitBot.Core.Scrimmages.ScrimmageCore",
        subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.ScrimmageHandler"]
    )]
    public record ChallengeCreated(Guid ChallengeId) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    [EventGenerator(
        pubTargetClass: "WabbitBot.DiscBot.App.ScrimmageApp",
        subTargetClasses: ["WabbitBot.Core.Scrimmages.ScrimmageHandler"]
    )]
    public record ChallengeRequested(
        int TeamSize,
        Guid ChallengerTeamId,
        Guid OpponentTeamId,
        Guid[] SelectedPlayerIds,
        Guid IssuedByPlayerId,
        int BestOf
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    [EventGenerator(
        pubTargetClass: "WabbitBot.DiscBot.App.ScrimmageApp",
        subTargetClasses: ["WabbitBot.DiscBot.App.Handlers"]
    )]
    public record ChallengeAccepted(
        Guid ChallengeId,
        Guid OpponentTeamId,
        Guid[] OpponentSelectedPlayerIds,
        Guid AcceptedByPlayerId
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    [EventGenerator(
        pubTargetClass: "WabbitBot.DiscBot.App.Commands.ScrimmageCommands",
        subTargetClasses: ["WabbitBot.DiscBot.App.Handlers"]
    )]
    public record ChallengeDeclined(Guid ChallengeId, Guid OpponentTeamId, Guid DeclinedByPlayerId) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }
}
