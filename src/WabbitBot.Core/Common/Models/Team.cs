using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Common.Models;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Common.Models
{
    public enum TeamFormat
    {
        OneVOne,
        TwoVTwo,
        ThreeVThree,
        FourVFour
    }
    public class Team : Entity
    {
        public string Name { get; set; } = string.Empty;
        public Guid TeamCaptainId { get; set; }
        public EvenTeamFormat TeamSize { get; set; }
        public int MaxRosterSize { get; set; }
        public DateTime LastActive { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string? Tag { get; set; }
        public Dictionary<EvenTeamFormat, Stats> Stats { get; set; } = new();
        public List<TeamMember> Roster { get; set; } = new();

        public Team()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            LastActive = DateTime.UtcNow;
            IsArchived = false;
            SetMaxRosterSize();
        }

        /// <summary>
        /// Shared helper methods for Discord commands
        /// </summary>
        /// <summary>
        /// Helper methods for Team model
        /// </summary>
        public static class Validation
        {
            /// <summary>
            /// Attempts to parse a string into a TeamRole enum value
            /// </summary>
            public static bool TryParseTeamRole(string role, out TeamRole teamRole)
            {
                teamRole = role.ToLowerInvariant() switch
                {
                    "core" => TeamRole.Core,
                    "backup" => TeamRole.Substitute,
                    _ => TeamRole.Core
                };

                return role.ToLowerInvariant() is "core" or "backup";
            }

            /// <summary>
            /// Validates if a team has a valid captain
            /// </summary>
            public static bool HasValidCaptain(Team team)
            {
                return team.TeamCaptainId != Guid.Empty &&
                       team.GetActiveMembers().Any(m => m.PlayerId == team.TeamCaptainId);
            }
        }

        private void SetMaxRosterSize()
        {
            // Allow one extra player for substitutes
            MaxRosterSize = TeamSize switch
            {
                EvenTeamFormat.TwoVTwo => 3,
                EvenTeamFormat.ThreeVThree => 4,
                EvenTeamFormat.FourVFour => 5,
                _ => 1
            };
        }

        public void UpdateLastActive()
        {
            LastActive = DateTime.UtcNow;
        }


        public void Archive()
        {
            IsArchived = true;
            ArchivedAt = DateTime.UtcNow;
        }

        public void Unarchive()
        {
            IsArchived = false;
            ArchivedAt = null;
        }

        public void AddPlayer(Guid playerId, TeamRole role = TeamRole.Core)
        {
            if (Roster.Count >= MaxRosterSize)
            {
                throw new InvalidOperationException($"Team roster is full (max size: {MaxRosterSize})");
            }

            if (Roster.Exists(m => m.PlayerId == playerId))
            {
                throw new InvalidOperationException("Player is already on the team");
            }

            Roster.Add(new TeamMember
            {
                PlayerId = playerId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                IsTeamManager = role == TeamRole.Captain // Captains are automatically team managers
            });
        }

        public void RemovePlayer(Guid playerId)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                Roster.Remove(member);
            }
        }

        public void UpdatePlayerRole(Guid playerId, TeamRole newRole)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                member.Role = newRole;
                // Captains are automatically team managers
                if (newRole == TeamRole.Captain)
                {
                    member.IsTeamManager = true;
                }
            }
        }

        public void ChangeCaptain(Guid newCaptainId)
        {
            // Find current captain
            var currentCaptain = Roster.Find(m => m.Role == TeamRole.Captain);
            if (currentCaptain != null)
            {
                // Demote current captain to Core
                currentCaptain.Role = TeamRole.Core;
                // Remove manager status from outgoing captain
                currentCaptain.IsTeamManager = false;
            }

            // Find new captain
            var newCaptain = Roster.Find(m => m.PlayerId == newCaptainId);
            if (newCaptain != null)
            {
                // Promote new captain
                newCaptain.Role = TeamRole.Captain;
                // Ensure new captain has manager status
                newCaptain.IsTeamManager = true;
                // Update team captain ID
                TeamCaptainId = newCaptainId;
            }
        }

        public void SetTeamManagerStatus(Guid playerId, bool isTeamManager)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                // Captains are always team managers and cannot be demoted
                if (member.Role == TeamRole.Captain && !isTeamManager)
                {
                    throw new InvalidOperationException("Team captains cannot have their manager status removed");
                }

                member.IsTeamManager = isTeamManager;
            }
        }

        public List<Guid> GetTeamManagerIds()
        {
            return Roster.Where(m => m.IsActive && m.IsTeamManager).Select(m => m.PlayerId).ToList();
        }

        public List<TeamMember> GetActiveMembers()
        {
            return Roster.Where(m => m.IsActive).ToList();
        }

        public TeamMember? GetMember(Guid playerId)
        {
            return Roster.FirstOrDefault(m => m.PlayerId == playerId);
        }

        public void DeactivatePlayer(Guid playerId)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                member.IsActive = false;
            }
        }

        public void ReactivatePlayer(Guid playerId)
        {
            var member = Roster.Find(m => m.PlayerId == playerId);
            if (member != null)
            {
                member.IsActive = true;
            }
        }

        public bool HasPlayer(Guid playerId)
        {
            return Roster.Exists(m => m.PlayerId == playerId);
        }

        public bool HasActivePlayer(Guid playerId)
        {
            return Roster.Exists(m => m.PlayerId == playerId && m.IsActive);
        }

        public int GetActivePlayerCount()
        {
            return Roster.Count(m => m.IsActive);
        }

        public List<Guid> GetActivePlayerIds()
        {
            return Roster.Where(m => m.IsActive).Select(m => m.PlayerId).ToList();
        }

        public List<Guid> GetCorePlayerIds()
        {
            return Roster.Where(m => m.IsActive && (m.Role == TeamRole.Core || m.Role == TeamRole.Captain)).Select(m => m.PlayerId).ToList();
        }

        public List<Guid> GetCaptainIds()
        {
            return Roster.Where(m => m.IsActive && m.Role == TeamRole.Captain).Select(m => m.PlayerId).ToList();
        }

        public bool IsCaptain(Guid playerId)
        {
            return Roster.Exists(m => m.PlayerId == playerId && m.IsActive && m.Role == TeamRole.Captain);
        }

        public bool IsTeamManager(Guid playerId)
        {
            return Roster.Exists(m => m.PlayerId == playerId && m.IsActive && m.IsTeamManager);
        }

        public bool IsValidForEvenTeamFormat(EvenTeamFormat evenTeamFormat)
        {
            var requiredPlayers = evenTeamFormat switch
            {
                EvenTeamFormat.TwoVTwo => 2,
                EvenTeamFormat.ThreeVThree => 3,
                EvenTeamFormat.FourVFour => 4,
                _ => 1
            };

            return GetActivePlayerCount() >= requiredPlayers;
        }
    }

    public enum TeamRole
    {
        Captain,
        Core,
        Substitute
    }

    public class TeamMember : Entity
    {
        public Guid PlayerId { get; set; }
        public TeamRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsTeamManager { get; set; }
    }

    #region Stats

    public class Stats : Entity
    {
        // Team identification (for team stats)
        public Guid TeamId { get; set; }
        public EvenTeamFormat EvenTeamFormat { get; set; }

        // Basic stats
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int MatchesCount => Wins + Losses;

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

        // Advanced stats
        public Dictionary<string, double> OpponentDistribution { get; set; } = new();
        public int RecentMatchesCount { get; set; }  // Number of games played within the variety window

        // Computed properties
        public double WinRate => MatchesCount == 0 ? 0 : (double)Wins / MatchesCount;

        public virtual void UpdateStats(bool isWin)
        {
            if (isWin)
            {
                Wins++;
                CurrentStreak = Math.Max(0, CurrentStreak) + 1;
            }
            else
            {
                Losses++;
                CurrentStreak = Math.Min(0, CurrentStreak) - 1;
            }

            LongestStreak = Math.Max(Math.Abs(CurrentStreak), LongestStreak);
            LastMatchAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }

        public virtual void UpdateRating(double newRating)
        {
            CurrentRating = newRating;
            HighestRating = Math.Max(HighestRating, newRating);
            LastUpdated = DateTime.UtcNow;
        }
    }
    #endregion
}
