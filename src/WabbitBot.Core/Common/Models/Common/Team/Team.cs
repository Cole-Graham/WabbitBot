using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Service;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Data.Interfaces;
using System.Threading.Tasks;

namespace WabbitBot.Core.Common.Models
{
    [EntityMetadata(
        tableName: "teams",
        archiveTableName: "team_archive",
        maxCacheSize: 200,
        cacheExpiryMinutes: 20,
        servicePropertyName: "Teams"
    )]
    public class Team : Entity, ITeamEntity
    {
        // Core team data
        public string Name { get; set; } = string.Empty;
        public Guid TeamCaptainId { get; set; }
        public TeamSize TeamSize { get; set; }
        public int MaxRosterSize { get; set; }
        public DateTime LastActive { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string? Tag { get; set; }
        public virtual ICollection<TeamMember> Roster { get; set; } = new List<TeamMember>();

        // Navigation properties
        public Dictionary<TeamSize, Stats> Stats { get; set; } = new();
        public Dictionary<TeamSize, TeamVarietyStats> VarietyStats { get; set; } = new();
        public virtual ICollection<TeamOpponentEncounter> RecentOpponents { get; set; } = new List<TeamOpponentEncounter>(); // Top 10 most recent
        public virtual ICollection<MatchParticipant> RecentParticipations { get; set; } = new List<MatchParticipant>(); // Recent matches

        public override Domain Domain => Domain.Common;
    }

    #region TeamRole
    public enum TeamRole
    {
        Captain,
        Core,
        Substitute
    }
    #endregion

    #region TeamMember
    [EntityMetadata(
        tableName: "team_members",
        archiveTableName: "team_member_archive",
        maxCacheSize: 500,
        cacheExpiryMinutes: 30,
        servicePropertyName: "TeamMembers"
    )]
    public class TeamMember : Entity, ITeamEntity
    {
        public Guid PlayerId { get; set; }
        public TeamRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsTeamManager { get; set; }

        public override Domain Domain => Domain.Common;
    }
    #endregion




    #region Stats
    [EntityMetadata(
        tableName: "team_stats",
        archiveTableName: "team_stats_archive",
        maxCacheSize: 1000,
        cacheExpiryMinutes: 60,
        servicePropertyName: "Stats"
    )]
    public class Stats : Entity, ITeamEntity
    {
        // Team identification (for team stats)
        public Guid TeamId { get; set; }
        public TeamSize TeamSize { get; set; }

        // Basic stats
        public int Wins { get; set; }
        public int Losses { get; set; }

        // Rating system (using double for precision as per user preference)
        public double InitialRating { get; set; } = 1000.0;
        public double CurrentRating { get; set; } = 1000.0;
        public double HighestRating { get; set; } = 1000.0;

        // Streak tracking
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }

        // Timing
        public DateTime LastMatchAt { get; set; }
        public DateTime LastUpdated { get; set; }

        // Variety statistics (replaces OpponentDistributionScore)
        public double VarietyEntropy { get; set; } = 0.0;
        public double VarietyBonus { get; set; } = 0.0;
        public int UniqueOpponents { get; set; } = 0;
        public int TotalOpponentEncounters { get; set; } = 0;

        // Navigation property
        public Team Team { get; set; } = null!;

        public override Domain Domain => Domain.Common;
    }
    #endregion

    // StatsBusinessLogic code moved to TeamCore

    #region TeamVarietyStats
    [EntityMetadata(
        tableName: "team_variety_stats",
        archiveTableName: "team_variety_stats_archive",
        maxCacheSize: 500,
        cacheExpiryMinutes: 60,
        servicePropertyName: "TeamVarietyStats"
    )]
    public class TeamVarietyStats : Entity, ITeamEntity
    {
        public Guid TeamId { get; set; }
        public TeamSize TeamSize { get; set; }
        public double VarietyEntropy { get; set; } = 0.0;
        public double VarietyBonus { get; set; } = 0.0;
        public int TotalOpponents { get; set; } = 0;
        public int UniqueOpponents { get; set; } = 0;
        public DateTime LastCalculated { get; set; }
        public DateTime LastUpdated { get; set; }

        // Navigation property
        public Team Team { get; set; } = null!;

        public override Domain Domain => Domain.Common;
    }
    #endregion
}

