namespace WabbitBot.Common.Events.Interfaces
{
    /// <summary>
    /// Defines the execution phase for event handlers to prevent race conditions.
    /// Write handlers execute first (mutate state), Read handlers execute second (read state).
    /// </summary>
    public enum HandlerType
    {
        /// <summary>
        /// Write handlers execute first and mutate database state.
        /// Typically used by Core handlers that update GameStateSnapshots, create entities, etc.
        /// </summary>
        Write = 0,

        /// <summary>
        /// Read handlers execute after Write handlers complete and read database state.
        /// Typically used by DiscBot handlers that update Discord UI based on current state.
        /// </summary>
        Read = 1,
    }
}
