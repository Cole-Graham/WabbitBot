using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    public class TeamListWrapper : BaseEntity
    {
        public List<Team> Teams { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public bool IncludeInactive { get; set; }
        public GameSize? FilterByGameSize { get; set; }

        public TeamListWrapper()
        {
            LastUpdated = DateTime.UtcNow;
            IncludeInactive = false;
        }

        public IEnumerable<Team> GetFilteredTeams()
        {
            var query = Teams.AsEnumerable();

            if (!IncludeInactive)
            {
                query = query.Where(t => t.LastActive > DateTime.UtcNow.AddDays(-30));
            }

            if (FilterByGameSize.HasValue)
            {
                query = query.Where(t => t.TeamSize == FilterByGameSize.Value);
            }

            return query;
        }
    }
}