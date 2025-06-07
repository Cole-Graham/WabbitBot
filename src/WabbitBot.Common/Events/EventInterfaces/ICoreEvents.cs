namespace WabbitBot.Common.Events.EventInterfaces
{
    /// <summary>
    /// Marker interface for core events. Only needed if Common needs to recognize
    /// core events without knowing their concrete implementations.
    /// </summary>
    public interface ICoreEvent
    {
        // No members needed - this is just a marker interface
        // Concrete event classes in Core will implement this if needed
    }
}