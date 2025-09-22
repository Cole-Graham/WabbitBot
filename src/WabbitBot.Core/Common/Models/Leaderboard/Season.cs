using System;
using System.Collections.Generic;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    /// <summary>
    /// Represents a season for a specific game size.
    /// Each season belongs to a SeasonGroup for coordination.
    /// </summary>
    public class Season : Entity
    {
        public Guid SeasonGroupId { get; set; }
        public EvenTeamFormat EvenTeamFormat { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, string> ParticipatingTeams { get; set; } = new();
        public Guid SeasonConfigId { get; set; }
        public Dictionary<string, object> ConfigData { get; set; } = new();

        public Season()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }


    public class SeasonConfig : Entity
    {
        public bool RatingDecayEnabled { get; set; }
        public double DecayRatePerWeek { get; set; }
        public double MinimumRating { get; set; }
    }

    /// <summary>
    /// Represents a group of seasons that are coordinated together.
    /// All seasons in a group typically start and end at the same time,
    /// but each season is for a different game size.
    /// </summary>
    public class SeasonGroup : Entity
    {
        public string Name { get; set; } = string.Empty;

        public SeasonGroup()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }
    }
}
