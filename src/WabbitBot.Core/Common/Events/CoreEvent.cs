using System;
using WabbitBot.Common.Events.Interfaces;

namespace WabbitBot.Core.Common.BotCore
{
    #region Startup Events
    public record CoreStartupCompletedEvent(
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default,
        DateTime CompletedAt = default
    ) : IEvent;

    public record CoreServicesInitializedEvent(
        string[] InitializedServices,
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default
    ) : IEvent;

    public record CoreFeatureReadyEvent(
        string FeatureName,
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default
    ) : IEvent;

    public record CoreErrorHandlingReadyEvent(
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default
    ) : IEvent;
    #endregion

    #region Error Events
    public record CoreServiceErrorEvent(
        string ServiceName,
        Exception Exception,
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default
    ) : IEvent;

    public record CoreStartupFailedEvent(
        Exception Exception,
        string Component = "Unknown",
        EventBusType EventBusType = EventBusType.Core,
        Guid EventId = default,
        DateTime Timestamp = default
    ) : IEvent;
    #endregion
}
