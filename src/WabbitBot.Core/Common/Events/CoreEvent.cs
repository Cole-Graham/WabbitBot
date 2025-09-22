using System;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.BotCore
{
    #region Startup Events
    public class CoreStartupCompletedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public DateTime CompletedAt { get; } = DateTime.UtcNow;
    }

    public class CoreServicesInitializedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string[] InitializedServices { get; }

        public CoreServicesInitializedEvent(string[] initializedServices)
        {
            InitializedServices = initializedServices;
        }
    }

    public class CoreFeatureReadyEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string FeatureName { get; }

        public CoreFeatureReadyEvent(string featureName)
        {
            FeatureName = featureName;
        }
    }

    public class CoreErrorHandlingReadyEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
    #endregion

    #region Error Events
    public class CoreServiceErrorEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string ServiceName { get; }
        public Exception Exception { get; }

        public CoreServiceErrorEvent(string serviceName, Exception exception)
        {
            ServiceName = serviceName;
            Exception = exception;
        }
    }

    public class CoreStartupFailedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public string EventId { get; init; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public Exception Exception { get; }
        public string Component { get; }

        public CoreStartupFailedEvent(Exception exception, string component = "Unknown")
        {
            Exception = exception;
            Component = component;
        }
    }
    #endregion
}