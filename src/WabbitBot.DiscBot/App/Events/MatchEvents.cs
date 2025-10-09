using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Attributes;


namespace WabbitBot.DiscBot.App.Events
{
    #region Match Provisioning
    /// <summary>
    /// DiscBot-local event requesting thread creation for a match.
    /// </summary>
    [EventGenerator(
        pubTargetClass: "WabbitBot.DiscBot.App.Handlers.MatchHandler",
        subTargetClasses: ["WabbitBot.DiscBot.App.Handlers.MatchHandler"])]
    public record ScrimThreadCreateRequested(
        ulong ScrimmageChannelId,
        Guid MatchId
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
    }

    /// <summary>
    /// DiscBot-local event requesting container creation for a match.
    /// </summary>
    public record MatchContainerRequested(
        Guid MatchId,
        ulong ChannelId,
        ulong Team1ThreadId,
        ulong Team2ThreadId
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
    }

    /// <summary>
    /// DiscBot-local event confirming a match thread was created.
    /// </summary>
    public record MatchThreadCreated(
        Guid MatchId,
        ulong ChannelId,
        ulong Team1ThreadId,
        ulong Team2ThreadId
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
    }
    #endregion

    #region Map Ban
    /// <summary>
    /// DiscBot-local event requesting map ban DM start for a player.
    /// </summary>
    public record MapBanDmStartRequested(
        Guid MatchId,
        ulong PlayerDiscordUserId
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
    }

    /// <summary>
    /// DiscBot-local event requesting map ban DM update (preview).
    /// </summary>
    public record MapBanDmUpdateRequested(
        Guid MatchId,
        ulong PlayerId,
        string[] Selections
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
    }

    /// <summary>
    /// DiscBot-local event requesting map ban DM confirmation (lock UI).
    /// </summary>
    public record MapBanDmConfirmRequested(
        Guid MatchId,
        ulong PlayerId,
        string[] Selections
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
    }

    /// <summary>
    /// DiscBot-local event from interaction when player selects provisional map bans.
    /// </summary>
    public record PlayerMapBanSelected(
        Guid MatchId,
        ulong PlayerId,
        string[] Selections
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
    }

    /// <summary>
    /// DiscBot-local event from interaction when player confirms their map bans.
    /// </summary>
    public record PlayerMapBanConfirmed(
        Guid MatchId,
        ulong PlayerId,
        string[] Selections
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.DiscBot;
    }
    #endregion

    #region Global Match Events

    /// <summary>
    /// Global event confirming match is provisioned (Discord UI ready).
    /// Published by MatchRenderer after creating thread and container.
    /// </summary>
    public record MatchProvisioned(
        Guid MatchId
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }

    /// <summary>
    /// Global event published when a match series completes.
    /// Cross-boundary integration fact for persistence, rating updates, leaderboards.
    /// </summary>
    public record MatchCompleted(
        Guid MatchId,
        Guid WinnerTeamId
    ) : IEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public EventBusType EventBusType { get; init; } = EventBusType.Global;
    }
    #endregion
}

