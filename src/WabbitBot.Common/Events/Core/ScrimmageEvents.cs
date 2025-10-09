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
        pubTargetClass: "WabbitBot.DiscBot.App.Commands.ScrimmageCommands",
        subTargetClasses: ["WabbitBot.Core.Scrimmages.ScrimmageHandler"]
    )]
    public record ChallengeRequested(
        int TeamSize,
        string ChallengerTeamName,
        string OpponentTeamName,
        string[] SelectedPlayerNames,
        ulong IssuedByDiscordUserId,
        int BestOf
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
        subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.ScrimmageHandler"]
    )]
    public record ChallengeDeclined(Guid ChallengeId, Guid OpponentTeamId, Guid DeclinedByPlayerId) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    [EventGenerator(
        pubTargetClass: "WabbitBot.DiscBot.App.Commands.ScrimmageCommands",
        subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.ScrimmageHandler"]
    )]
    public record ChallengeCancelled(Guid ChallengeId, Guid CancelledByPlayerId) : IEvent
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
