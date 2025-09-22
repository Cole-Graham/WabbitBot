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
        public BotOptions Configuration { get; }
        public IBotConfigurationService ConfigurationService { get; }

        public StartupInitiatedEvent(BotOptions configuration, IBotConfigurationService configService)
        {
            Configuration = configuration;
            ConfigurationService = configService;
        }
    }

    public class SystemReadyEvent
    {
        public DateTime StartupTime { get; } = DateTime.UtcNow;
    }

    public class ApplicationReadyEvent
    {
        public TimeSpan StartupDuration { get; }
        public IBotConfigurationService ConfigurationService { get; init; }

        public ApplicationReadyEvent(TimeSpan startupDuration, IBotConfigurationService configService)
        {
            StartupDuration = startupDuration;
            ConfigurationService = configService;
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