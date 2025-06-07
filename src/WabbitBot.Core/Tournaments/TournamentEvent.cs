using System;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Tournaments
{
    public abstract class TournamentEvent
    {
        public Guid TournamentId { get; set; }
        public DateTime Timestamp { get; set; }
        public int Version { get; set; }
        public string EventType { get; set; }
        public string UserId { get; set; }
    }

    public class TournamentCreatedEvent : TournamentEvent
    {
        public Tournament Tournament { get; set; }
    }

    public class TournamentUpdatedEvent : TournamentEvent
    {
        public Tournament Before { get; set; }
        public Tournament After { get; set; }
        public string[] ChangedProperties { get; set; }
    }

    public class TournamentStatusChangedEvent : TournamentEvent
    {
        public TournamentStatus OldStatus { get; set; }
        public TournamentStatus NewStatus { get; set; }
        public string Reason { get; set; }
    }

    public class TournamentDeletedEvent : TournamentEvent
    {
        public string Reason { get; set; }
    }
}
