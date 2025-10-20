namespace WabbitBot.Common.Events.Interfaces
{
    /// <summary>
    /// Metadata wrapper for event handlers that includes execution phase information.
    /// Used by event buses to implement two-phase execution (Write then Read).
    /// </summary>
    public class EventHandlerMetadata
    {
        /// <summary>
        /// The handler delegate to invoke when the event is published.
        /// </summary>
        public required Delegate Handler { get; init; }

        /// <summary>
        /// The execution phase for this handler (Write or Read).
        /// </summary>
        public required HandlerType Type { get; init; }
    }
}
