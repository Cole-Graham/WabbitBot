using System;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.BotCore
{
    #region Startup Events
    public class CoreStartupCompletedEvent : ICoreEvent
    {
        public DateTime CompletedAt { get; } = DateTime.UtcNow;
    }

    public class CoreServicesInitializedEvent : ICoreEvent
    {
        public string[] InitializedServices { get; }

        public CoreServicesInitializedEvent(string[] initializedServices)
        {
            InitializedServices = initializedServices;
        }
    }

    public class CoreFeatureReadyEvent : ICoreEvent
    {
        public string FeatureName { get; }

        public CoreFeatureReadyEvent(string featureName)
        {
            FeatureName = featureName;
        }
    }

    public class CoreErrorHandlingReadyEvent : ICoreEvent { }
    #endregion

    #region Error Events
    public class CoreServiceErrorEvent : ICoreEvent
    {
        public string ServiceName { get; }
        public Exception Exception { get; }

        public CoreServiceErrorEvent(string serviceName, Exception exception)
        {
            ServiceName = serviceName;
            Exception = exception;
        }
    }

    public class CoreStartupFailedEvent : ICoreEvent
    {
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