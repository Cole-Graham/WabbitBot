using System;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Common.Attributes
{
    /// <summary>
    /// Defines the target event buses for event publishing.
    /// Used with EventTrigger to specify where events should be published.
    /// </summary>
    [Flags]
    public enum EventTargets
    {
        /// <summary>
        /// Publish to the local/default event bus only.
        /// </summary>
        Local = 1,

        /// <summary>
        /// Publish to the Global event bus only.
        /// </summary>
        Global = 2,

        /// <summary>
        /// Publish to both local and Global event buses (dual-publish).
        /// </summary>
        Both = Local | Global
    }

    #region Command
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class WabbitCommandAttribute(string name) : Attribute
    {
        public string Name { get; } = name;
        public string? Group { get; set; }
    }
    #endregion

    #region Cross-Boundary
    /// <summary>
    /// Marks a class for cross-boundary duplication. Classes marked with this attribute 
    /// will have duplicate definitions generated in the target project.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class GenerateCrossBoundaryAttribute(string? targetProjects = null) : Attribute
    {
        public string? TargetProjects { get; } = targetProjects;
    }
    #endregion

    #region Method Implementation
    /// <summary>
    /// Attribute to mark a class for source-generated method implementations.
    /// Only use this when you want the source generator to create partial method implementations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GenerateImplementationAttribute() : Attribute
    {
    }
    #endregion

    #region Event Boundary Generation
    /// <summary>
    /// Marks a class or interface for event boundary generation.
    /// Triggers the source generator to emit event records, publishers, and subscribers
    /// based on methods/properties in the declaration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public class EventBoundaryAttribute(
        bool generateRequestResponse = false,
        string? targetProjects = null) : Attribute  // Comma-separated or array in usage
    {
        public bool GenerateRequestResponse { get; } = generateRequestResponse;
        public string? TargetProjects { get; } = targetProjects;  // e.g., "DiscBot,Analytics"
    }
    #endregion

    #region Event Type Specification
    /// <summary>
    /// Specifies the EventBusType for an event or boundary.
    /// Defaults to inferred from namespace/project if omitted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class EventTypeAttribute(EventBusType busType) : Attribute
    {
        public EventBusType BusType { get; } = busType;
    }
    #endregion

    #region Event Subscriptions and Handlers
    /// <summary>
    /// Marks a handler class for auto-generated event subscriptions.
    /// Emits Subscribe calls and partial handler methods in InitializeAsync.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GenerateEventSubscriptionsAttribute(
        bool enableMetrics = false,
        bool enableErrorHandling = false,
        bool enableLogging = false) : Attribute
    {
        /// <summary>
        /// Enables metrics emission (e.g., counters for publishes).
        /// </summary>
        public bool EnableMetrics { get; } = enableMetrics;

        /// <summary>
        /// Enables error handling wrappers (e.g., try-catch emitting BoundaryErrorEvent).
        /// </summary>
        public bool EnableErrorHandling { get; } = enableErrorHandling;

        /// <summary>
        /// Enables logging wrappers (e.g., ILogger calls).
        /// </summary>
        public bool EnableLogging { get; } = enableLogging;
    }

    /// <summary>
    /// Marks a bus class for auto-generated handler middleware.
    /// Emits metrics, error handling, and logging around Publish/Subscribe.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GenerateEventHandlerAttribute(
        EventBusType busType,
        bool enableMetrics = false,
        bool enableErrorHandling = false,
        bool enableLogging = false) : Attribute
    {
        public EventBusType BusType { get; } = busType;
        public bool EnableMetrics { get; } = enableMetrics;
        public bool EnableErrorHandling { get; } = enableErrorHandling;
        public bool EnableLogging { get; } = enableLogging;
    }
    #endregion

    #region Event Generator and Trigger
    /// <summary>
    /// Marks a class for automatic event publisher and subscriber generation.
    /// Supports opt-in trigger mode where methods marked with [EventTrigger] will have publishers generated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EventGeneratorAttribute : Attribute
    {
        /// <summary>
        /// Default event bus type for generated events when not overridden by EventTrigger.
        /// </summary>
        public EventBusType DefaultBus { get; set; } = EventBusType.Global;

        /// <summary>
        /// Whether to generate publisher methods for events.
        /// </summary>
        public bool GeneratePublishers { get; set; } = false;

        /// <summary>
        /// Whether to generate subscriber registrations for events.
        /// </summary>
        public bool GenerateSubscribers { get; set; } = false;

        /// <summary>
        /// Whether to generate request-response patterns.
        /// </summary>
        public bool GenerateRequestResponse { get; set; } = false;

        /// <summary>
        /// Trigger mode: "OptIn" means only methods with [EventTrigger] will generate events.
        /// Other modes like "Convention" could be added later.
        /// </summary>
        public string TriggerMode { get; set; } = "OptIn";
    }

    /// <summary>
    /// Marks a method for automatic event publisher generation.
    /// Must be used on a class decorated with [EventGenerator(TriggerMode = "OptIn")].
    /// The method signature determines the event payload.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EventTriggerAttribute : Attribute
    {
        /// <summary>
        /// Override the event bus type for this specific trigger.
        /// Uses the DefaultBus from EventGenerator when not explicitly set.
        /// </summary>
        public EventBusType BusType { get; set; } = EventBusType.Global;

        /// <summary>
        /// Target event buses for publishing.
        /// Local = publish to default/local bus only
        /// Global = publish to Global bus only
        /// Both = dual-publish to both local and Global buses
        /// </summary>
        public EventTargets Targets { get; set; } = EventTargets.Global;
    }
    #endregion

    #region Component Factory Generation
    /// <summary>
    /// Marks a component model class for factory generation.
    /// The generator will create factory methods to build Discord components from these POCOs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class GenerateComponentFactoryAttribute : Attribute
    {
        /// <summary>
        /// Optional theme/color for the component (e.g., "Info", "Success", "Warning", "Error").
        /// </summary>
        public string? Theme { get; set; }

        /// <summary>
        /// Whether this component supports attachments.
        /// </summary>
        public bool SupportsAttachments { get; set; } = false;
    }
    #endregion

    #region Suppression
    /// <summary>
    /// Suppresses event generation for a specific declaration (e.g., manual override).
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class SuppressGenerationAttribute(string? reason = null) : Attribute
    {
        public string? Reason { get; } = reason;  // Optional for docs
    }
    #endregion
}