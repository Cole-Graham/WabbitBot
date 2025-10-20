using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Models.Common
{
    [EntityMetadata(
        tableName: "teams",
        archiveTableName: "team_archive",
        maxCacheSize: 200,
        cacheExpiryMinutes: 20,
        servicePropertyName: "Teams",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class Team : Entity, ITeamEntity
    {
        // Navigation properties
        public Dictionary<TeamSize, ScrimmageTeamStats> ScrimmageTeamStats { get; set; } = [];
        public Dictionary<TeamSize, TournamentTeamStats> TournamentTeamStats { get; set; } = [];

        // Foreign key collections
        public ICollection<Guid> MatchIds { get; set; } = [];
        public ICollection<Guid> RosterIds { get; set; } = [];
        public ICollection<Guid> VarietyStatsIds { get; set; } = [];

        // Navigation properties
        public virtual ICollection<Match> Matches { get; set; } = [];

        // Core team data (no roster-specific properties here)
        public string Name { get; set; } = string.Empty;
        public Guid TeamCaptainId { get; set; }
        public DateTime LastActive { get; set; }
        public string? Tag { get; set; }

        // Team type determines roster constraints
        public TeamType TeamType { get; set; }

        // Navigation to rosters - each team can have multiple rosters for different game sizes
        public virtual ICollection<TeamRoster> Rosters { get; set; } = [];

        // VarietyStats are for the scrimmage rating system
        public virtual ICollection<TeamVarietyStats> VarietyStats { get; set; } = [];

        public override Domain Domain => Domain.Common;
    }

    #region TeamRole
    public enum TeamRole
    {
        Captain,
        Core,
        Substitute,
    }
    #endregion

    #region TeamType
    public enum TeamType
    {
        Solo = 0, // 1v1 teams - can only have Solo roster
        Team = 1, // Team games - can have Duo and/or Squad rosters (but not Solo)
    }
    #endregion

    #region TeamRoster
    [EntityMetadata(
        tableName: "team_rosters",
        archiveTableName: "team_roster_archive",
        maxCacheSize: 1000,
        cacheExpiryMinutes: 30,
        servicePropertyName: "TeamRosters",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class TeamRoster : Entity, ITeamEntity
    {
        // Navigation properties
        public Guid TeamId { get; set; }
        public virtual Team Team { get; set; } = null!;

        // Roster-specific properties
        public TeamSizeRosterGroup RosterGroup { get; set; }
        public int MaxRosterSize { get; set; }
        public DateTime LastActive { get; set; }
        public Guid? RosterCaptainId { get; set; } // Roster-specific captain (optional)

        // Foreign key collection for members
        public ICollection<Guid> RosterMemberIds { get; set; } = [];

        // Navigation to members
        public virtual ICollection<TeamMember> RosterMembers { get; set; } = [];

        public override Domain Domain => Domain.Common;
    }
    #endregion

    #region TeamMember
    [EntityMetadata(
        tableName: "team_members",
        archiveTableName: "team_member_archive",
        maxCacheSize: 500,
        cacheExpiryMinutes: 30,
        servicePropertyName: "TeamMembers",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class TeamMember : Entity, ITeamEntity
    {
        // Navigation properties
        public Guid TeamRosterId { get; set; }
        public virtual TeamRoster? TeamRoster { get; set; } = null;
        public Guid MashinaUserId { get; set; }
        public virtual MashinaUser? MashinaUser { get; set; }
        public Guid PlayerId { get; set; }
        public string DiscordUserId { get; set; } = string.Empty;

        // Member-specific properties
        public TeamRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsTeamManager { get; set; }
        public bool ReceiveScrimmagePings { get; set; } = false;

        public override Domain Domain => Domain.Common;
    }
    #endregion


    #region ScrimmageTeamStats
    [EntityMetadata(
        tableName: "scrimmage_team_stats",
        archiveTableName: "scrimmage_team_stats_archive",
        maxCacheSize: 1000,
        cacheExpiryMinutes: 60,
        servicePropertyName: "ScrimmageTeamStats",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class ScrimmageTeamStats : Entity, ITeamEntity
    {
        // Team identification (for team stats)
        public Guid TeamId { get; set; }
        public TeamSize TeamSize { get; set; }

        // Foreign key collection for opponent encounters
        public ICollection<Guid> OpponentEncounterIds { get; set; } = [];

        // Navigation property for opponent encounters
        public virtual ICollection<TeamOpponentEncounter> OpponentEncounters { get; set; } = [];

        // Basic stats
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }

        // Rating system (using double for precision as per user preference)
        public double InitialRating { get; set; }
        public double CurrentRating { get; set; }
        public double HighestRating { get; set; }
        public double RecentRatingChange { get; set; }
        public double Confidence { get; set; }

        // Streak tracking
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }

        // Timing
        public DateTime LastMatchAt { get; set; }
        public DateTime LastUpdated { get; set; }

        public override Domain Domain => Domain.Common;
    }
    #endregion

    #region TournamentTeamStats
    [EntityMetadata(
        tableName: "tournament_team_stats",
        archiveTableName: "tournament_team_stats_archive",
        maxCacheSize: 1000,
        cacheExpiryMinutes: 60,
        servicePropertyName: "TournamentTeamStats",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class TournamentTeamStats : Entity, ITeamEntity
    {
        // Navigation properties
        public Guid TeamId { get; set; }
        public virtual Team? Team { get; set; } = null;
        public List<Guid> TournamentIds { get; set; } = [];

        // Navigation property for tournaments
        public virtual ICollection<Tournament.Tournament> Tournaments { get; set; } = [];

        // Data properties
        public TeamSize TeamSize { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public int GamesDrawn { get; set; }
        public int MatchesWon { get; set; }
        public int MatchesLost { get; set; }
        public double InitialRating { get; set; }
        public double CurrentRating { get; set; }
        public double HighestRating { get; set; }
        public double RecentRatingChange { get; set; }
        public double Confidence { get; set; }
        public DateTime LastMatchAt { get; set; }
        public DateTime LastUpdated { get; set; }

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
        servicePropertyName: "TeamVarietyStats",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class TeamVarietyStats : Entity, ITeamEntity
    {
        // Navigation
        public virtual Team? Team { get; set; } = null;
        public Guid TeamId { get; set; }

        // Data
        public TeamSize TeamSize { get; set; }
        public double VarietyEntropy { get; set; }
        public double VarietyBonus { get; set; }
        public int TotalOpponents { get; set; }
        public int UniqueOpponents { get; set; }
        public DateTime LastCalculated { get; set; }
        public DateTime LastUpdated { get; set; }

        // Calculation context
        public double AverageVarietyEntropyAtCalc { get; set; }
        public double MedianGamesAtCalc { get; set; }
        public double RatingRangeAtCalc { get; set; }
        public double NeighborRangeAtCalc { get; set; }
        public int PlayerNeighborsAtCalc { get; set; }
        public int MaxNeighborsObservedAtCalc { get; set; }
        public double AvailabilityFactorUsed { get; set; }

        public override Domain Domain => Domain.Common;
    }
    #endregion

    #region TeamOpponentEncounter
    [EntityMetadata(
        tableName: "team_opponent_encounters",
        archiveTableName: "team_opponent_encounter_archive",
        maxCacheSize: 2000,
        cacheExpiryMinutes: 60,
        servicePropertyName: "TeamOpponentEncounters",
        emitCacheRegistration: true,
        emitArchiveRegistration: true
    )]
    public class TeamOpponentEncounter : Entity, IMatchEntity
    {
        // Navigation properties
        public Guid MatchId { get; set; }
        public virtual Match Match { get; set; } = null!;

        public Guid TeamId { get; set; }
        public virtual Team Team { get; set; } = null!;

        public Guid OpponentId { get; set; }
        public virtual Team Opponent { get; set; } = null!;

        // State properties
        public TeamSize TeamSize { get; set; }
        public DateTime EncounteredAt { get; set; }
        public bool Won { get; set; }

        public override Domain Domain => Domain.Common;
    }
    #endregion
}
