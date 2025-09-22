using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Leaderboards;

/// <summary>
/// Event published when a season is created
/// </summary>
public partial record SeasonCreatedEvent(
    string SeasonId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a season is updated
/// </summary>
public partial record SeasonUpdatedEvent(
    string SeasonId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a season is deleted
/// </summary>
public partial record SeasonDeletedEvent(
    string SeasonId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a season is archived
/// </summary>
public partial record SeasonArchivedEvent(
    string SeasonId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a season ends
/// </summary>
public partial record SeasonEndedEvent(
    string SeasonId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when rating decay is applied to a season
/// </summary>
public partial record SeasonRatingDecayAppliedEvent(
    string SeasonId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a team is registered for a season
/// </summary>
public partial record TeamRegisteredForSeasonEvent(
    string SeasonId,
    string TeamId
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Event published when a team result is added to a season
/// </summary>
public partial record TeamResultAddedEvent(
    string SeasonId,
    string TeamId,
    string EvenTeamFormat,
    double RatingChange,
    bool IsWin
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Business logic event for season validation - used as a mutable data container
/// where handlers can set validation properties
/// </summary>
public partial class SeasonValidationEvent : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string SeasonId { get; }
    public bool IsValid { get; set; }
    public string ValidationMessage { get; set; }

    public SeasonValidationEvent(string seasonId)
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
    string SeasonId,
    List<string> ParticipatingTeamIds
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

#region SeasonRatingService Events

/// <summary>
/// Event published when a team's rating is updated.
/// Handlers should look up the team and rating details from cache/repository.
/// </summary>
public partial record TeamRatingUpdatedEvent(
    string TeamId,
    string EvenTeamFormat
) : IEvent
{
    public EventBusType EventBusType { get; init; } = EventBusType.Core;
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

#endregion
