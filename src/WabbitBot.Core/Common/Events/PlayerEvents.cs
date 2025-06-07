using System;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.Events
{
    public class PlayerArchiveCheckEvent : ICoreEvent
    {
        public Guid PlayerId { get; }
        public bool HasActiveUsers { get; set; }
        public bool HasActiveMatches { get; set; }

        public PlayerArchiveCheckEvent(Guid playerId)
        {
            PlayerId = playerId;
            HasActiveUsers = false;
            HasActiveMatches = false;
        }
    }

    public class PlayerArchivedEvent : ICoreEvent
    {
        public Guid PlayerId { get; }
        public DateTime ArchivedAt { get; }

        public PlayerArchivedEvent(Guid playerId)
        {
            PlayerId = playerId;
            ArchivedAt = DateTime.UtcNow;
        }
    }

    public class PlayerUnarchivedEvent : ICoreEvent
    {
        public Guid PlayerId { get; }
        public DateTime UnarchivedAt { get; }

        public PlayerUnarchivedEvent(Guid playerId)
        {
            PlayerId = playerId;
            UnarchivedAt = DateTime.UtcNow;
        }
    }
}