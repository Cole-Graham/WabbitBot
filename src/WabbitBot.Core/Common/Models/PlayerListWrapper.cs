using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    public class PlayerListWrapper : BaseEntity
    {
        public List<Player> Players { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public bool IncludeInactive { get; set; }

        public PlayerListWrapper()
        {
            LastUpdated = DateTime.UtcNow;
            IncludeInactive = false;
        }
    }
}