using WabbitBot.Common.Configuration;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Events
{
    /// <summary>
    /// Events for the global startup sequence. These events coordinate startup across all projects.
    /// They are part of the infrastructure layer and therefore live in the Common project.
    /// </summary>

    #region Startup Sequence Events
    public class StartupInitiatedEvent
    {
        public BotConfiguration Configuration { get; }
        public IBotConfigurationReader ConfigurationReader { get; }

        public StartupInitiatedEvent(BotConfiguration configuration, IBotConfigurationReader configReader)
        {
            Configuration = configuration;
            ConfigurationReader = configReader;
        }
    }

    public class SystemReadyEvent
    {
        public DateTime StartupTime { get; } = DateTime.UtcNow;
    }

    public class ApplicationReadyEvent
    {
        public TimeSpan StartupDuration { get; }

        public ApplicationReadyEvent(TimeSpan startupDuration)
        {
            StartupDuration = startupDuration;
        }
    }
    #endregion

    #region Shutdown Events
    public class ApplicationShuttingDownEvent
    {
        public string Reason { get; }
        public bool IsGraceful { get; }

        public ApplicationShuttingDownEvent(string reason, bool isGraceful = true)
        {
            Reason = reason;
            IsGraceful = isGraceful;
        }
    }
    #endregion

    #region Error Events
    public class GlobalErrorHandlingReadyEvent { }

    public class CriticalStartupErrorEvent
    {
        public Exception Exception { get; }
        public string Component { get; }

        public CriticalStartupErrorEvent(Exception exception, string component = "Unknown")
        {
            Exception = exception;
            Component = component;
        }
    }
    #endregion
}