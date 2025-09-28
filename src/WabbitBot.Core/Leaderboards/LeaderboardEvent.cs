using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Leaderboards;

/// <summary>
/// Events related to leaderboard operations.
/// Leaderboards are read-only views generated from Season data.
/// </summary>

/// <summary>
/// Event published when a leaderboard is successfully refreshed from Season data.
/// </summary>
public partial record LeaderboardRefreshedEvent(
    TeamSize TeamSize,
    int TeamCount,
    DateTime RefreshedAt = default
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public DateTime RefreshedAt { get; init; } = RefreshedAt == default ? DateTime.UtcNow : RefreshedAt;
}

/// <summary>
/// Event published when leaderboard refresh fails.
/// </summary>
public partial record LeaderboardRefreshFailedEvent(
    TeamSize TeamSize,
    string ErrorMessage,
    DateTime FailedAt = default
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public DateTime FailedAt { get; init; } = FailedAt == default ? DateTime.UtcNow : FailedAt;
}

/// <summary>
/// Event published when all leaderboards are refreshed.
/// </summary>
public partial record AllLeaderboardsRefreshedEvent(
    int TotalTeamsProcessed,
    DateTime RefreshedAt = default
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public DateTime RefreshedAt { get; init; } = RefreshedAt == default ? DateTime.UtcNow : RefreshedAt;
}
