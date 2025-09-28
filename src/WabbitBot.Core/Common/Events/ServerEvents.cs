using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.Events
{
    /// <summary>
    /// Generic thumbnail events that can be used across different features (maps, teams, etc.)
    /// </summary>
    public partial class ThumbnailUploadedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event published when a thumbnail is successfully deleted
    /// </summary>
    public partial class ThumbnailDeletedEvent : IEvent
    {
        public EventBusType EventBusType { get; init; } = EventBusType.Core;
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
        public string FileName { get; set; } = string.Empty;
    }
}
