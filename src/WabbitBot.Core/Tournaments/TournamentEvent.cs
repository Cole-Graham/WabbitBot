using System;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Tournaments
{
    public abstract class TournamentEvent
    {
        public required Guid TournamentId { get; set; }
        public required DateTime Timestamp { get; set; }
        public required int Version { get; set; }
        public string EventType => GetType().Name;
        public required string UserId { get; set; }
    }

    public class TournamentCreatedEvent : TournamentEvent
    {
        public required Tournament Tournament { get; set; }
    }

    public class TournamentUpdatedEvent : TournamentEvent
    {
        public required Tournament Before { get; set; }
        public required Tournament After { get; set; }
        public required string[] ChangedProperties { get; set; }
    }

    public class TournamentStatusChangedEvent : TournamentEvent
    {
        public required TournamentStatus OldStatus { get; set; }
        public required TournamentStatus NewStatus { get; set; }
        public string? Reason { get; set; }
    }

    public class TournamentDeletedEvent : TournamentEvent
    {
        public string? Reason { get; set; }
    }
}
