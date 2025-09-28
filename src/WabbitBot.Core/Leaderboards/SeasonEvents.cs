using System;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Leaderboards;

/// <summary>
/// Event published when a season ends
/// </summary>
public partial record SeasonEndedEvent(
    Guid SeasonId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when rating decay is applied to a season
/// </summary>
public partial record SeasonRatingDecayAppliedEvent(
    Guid SeasonId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a team is registered for a season
/// </summary>
public partial record TeamRegisteredForSeasonEvent(
    Guid SeasonId,
    Guid TeamId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a team result is added to a season
/// </summary>
public partial record TeamResultAddedEvent(
    Guid SeasonId,
    Guid TeamId,
    string TeamSize,
    double RatingChange,
    bool IsWin
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business logic event for season validation - used as a mutable data container
/// where handlers can set validation properties
/// </summary>
public partial class SeasonValidationEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Guid SeasonId { get; }
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; }

    public SeasonValidationEvent(Guid seasonId)
    {
        SeasonId = seasonId;
        IsValid = true;
        ValidationMessage = string.Empty;
    }
}

/// <summary>
/// Event published when a season's participating teams change
/// </summary>
public partial record SeasonTeamsUpdatedEvent(
    Guid SeasonId,
    List<Guid> ParticipatingTeamIds
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a team's rating is updated.
/// This is a business logic event that signals the Leaderboard slice to refresh.
/// </summary>
public record TeamRatingUpdatedEvent(Guid TeamId, string TeamSize) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record ApplyTeamRatingChangeEvent(
    Guid TeamId,
    TeamSize TeamSize,
    double RatingChange,
    string Reason
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}