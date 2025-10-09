using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Common.Events.Core
{
    [EventGenerator(
        pubTargetClass: "WabbitBot.Core.Scrimmages.ScrimmageCore",
        subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.ScrimmageHandler"]
    )]
    public record ScrimmageChallengeCreated(Guid ScrimmageChallengeId, Guid ChallengerTeamId, Guid OpponentTeamId)
        : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    public record ChallengeRequested(
        string ChallengerTeamName,
        string OpponentTeamName,
        ulong RequesterId,
        ulong ChannelId
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    public record ChallengeAccepted(
        Guid ChallengeId,
        Guid ChallengerTeamId,
        Guid OpponentTeamId,
        Guid IssuedByPlayerId,
        Guid AcceptedByPlayerId
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    public record ChallengeDeclined(Guid ChallengeId, Guid ChallengerTeamId, Guid OpponentTeamId, Guid IssuedByPlayerId)
        : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    public record ChallengeCancelled(Guid ChallengeId, Guid ChallengerTeamId, Guid OpponentTeamId, ulong RequesterId)
        : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    [EventGenerator(
        pubTargetClass: "WabbitBot.Core.Scrimmages.ScrimmageCore",
        subTargetClasses: [
            "WabbitBot.DiscBot.App.Handlers.ScrimmageHandler",
            "WabbitBot.DiscBot.App.Handlers.MatchHandler",
        ]
    )]
    public record ScrimmageCreated(Guid ScrimmageId) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    [EventGenerator(
        pubTargetClass: "WabbitBot.Core.Scrimmages.ScrimmageCore",
        subTargetClasses: [
            "WabbitBot.DiscBot.App.Handlers.ScrimmageHandler",
            "WabbitBot.DiscBot.App.Handlers.MatchHandler",
        ]
    )]
    public record MatchProvisioningRequested(Guid ScrimmageId, ulong ScrimmageChannelId) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }
}
