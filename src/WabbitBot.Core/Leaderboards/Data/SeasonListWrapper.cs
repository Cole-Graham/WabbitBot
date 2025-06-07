using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class SeasonListWrapper : BaseEntity
    {
        public List<Season> Seasons { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public bool IncludeInactive { get; set; }
    }
}