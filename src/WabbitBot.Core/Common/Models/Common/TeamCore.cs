using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using System.Linq;
using WabbitBot.Core.Common.Interfaces;

namespace WabbitBot.Core.Common.Models.Common
{
    public partial class TeamCore : ITeamCore
    {
        /// <inheritdoc />
        public Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task ValidateAsync() => Task.CompletedTask;

        public static class Factory
        {
            public static Team CreateTeam(
                Guid id,
                string name,
                Guid teamCaptainId,
                TeamSize teamSize,
                List<TeamMember> teamMembers,
                DateTime createdAt,
                DateTime lastActive,
                bool isArchived)
            {
                var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(
                    ScrimmageOptions.SectionName);

                return new Team
                {
                    Id = id,
                    Name = name,
                    TeamCaptainId = teamCaptainId,
                    TeamSize = teamSize,
                    MaxRosterSize = scrimmageConfig.MaxRosterSize,
                    TeamMembers = teamMembers,
                };
            }
        }

        public async Task UpdateLastActive(Guid teamId)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Failed to retrieve team {teamId} for last active update: " +
                        $"{teamResult.ErrorMessage}"
                    ),
                    "Team Last Active Warning",
                    nameof(UpdateLastActive)
                );
                return; // Exit if team not found
            }
            var team = teamResult.Data;

            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Team {teamId} not found for last active update after successful retrieval."
                    ),
                    "Team Last Active Warning",
                    nameof(UpdateLastActive)
                );
                return; // Exit if team not found
            }

            team.LastActive = DateTime.UtcNow;
            var updateTeamRepoResult = await CoreService.Teams.UpdateAsync(team,
                DatabaseComponent.Repository);
            var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team,
                DatabaseComponent.Cache);

            if (!updateTeamRepoResult.Success || !updateTeamCacheResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Failed to update last active for team {teamId}: " +
                        $"{updateTeamRepoResult.ErrorMessage} / " +
                        $"{updateTeamCacheResult.ErrorMessage}"
                    ),
                    "Team Last Active Warning",
                    nameof(UpdateLastActive)
                );
            }
        }
        public async Task AddPlayer(Guid teamId, Guid playerId, TeamRole role = TeamRole.Core)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                throw new InvalidOperationException($"Failed to retrieve team {teamId}: {teamResult.ErrorMessage}");
            }
            var team = teamResult.Data;

            if (team is null)
            {
                throw new InvalidOperationException($"Team with ID {teamId} not found.");
            }

            if (team.TeamMembers.Count >= team.MaxRosterSize)
            {
                throw new InvalidOperationException($"Team roster is full (max size: {team.MaxRosterSize})");
            }

            if (team.TeamMembers.Any(m => m.PlayerId == playerId))
            {
                throw new InvalidOperationException("Player is already on the team");
            }

            team.TeamMembers.Add(new TeamMember
            {
                PlayerId = playerId,
                Role = role,
                JoinedAt = DateTime.UtcNow,
                IsActive = true,
                IsTeamManager = role == TeamRole.Captain // Captains are automatically team managers
            });

            var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                DatabaseComponent.Repository);
            var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team,
                DatabaseComponent.Cache);

            if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Failed to add player {playerId} to team {teamId}: " +
                        $"{updateTeamResult.ErrorMessage} / " +
                        $"{updateTeamCacheResult.ErrorMessage}"
                    ),
                    "Team Add Player Warning",
                    nameof(AddPlayer)
                );
            }
        }

        public async Task RemovePlayer(Guid teamId, Guid playerId)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Failed to retrieve team {teamId} for player removal: " +
                        $"{teamResult.ErrorMessage}"
                    ),
                    "Team Remove Player Warning",
                    nameof(RemovePlayer)
                );
                return; // Exit if team not found
            }
            var team = teamResult.Data;

            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Team {teamId} not found for player removal after successful retrieval."
                    ),
                    "Team Remove Player Warning",
                    nameof(RemovePlayer)
                );
                return; // Exit if team not found
            }

            var member = team.TeamMembers.FirstOrDefault(m => m.PlayerId == playerId);
            if (member != null)
            {
                team.TeamMembers.Remove(member);
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(new Exception(
                            $"Failed to remove player {playerId} from team {teamId}: " +
                            $"{updateTeamResult.ErrorMessage} / " +
                            $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Remove Player Warning",
                        nameof(RemovePlayer)
                    );
                }
            }
        }

        public async Task UpdatePlayerRole(Guid teamId, Guid playerId, TeamRole newRole)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Failed to retrieve team {teamId} for player role update: " +
                        $"{teamResult.ErrorMessage}"
                    ),
                    "Team Update Player Role Warning",
                    nameof(UpdatePlayerRole)
                );
                return; // Exit if team not found
            }
            var team = teamResult.Data;

            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Team {teamId} not found for player role update after successful retrieval."
                    ),
                    "Team Update Player Role Warning",
                    nameof(UpdatePlayerRole)
                );
                return; // Exit if team not found
            }

            var member = team.TeamMembers.FirstOrDefault(m => m.PlayerId == playerId);
            if (member != null)
            {
                member.Role = newRole;
                // Captains are automatically team managers
                if (newRole == TeamRole.Captain)
                {
                    member.IsTeamManager = true;
                }
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(new Exception(
                            $"Failed to update player role for team {teamId}: " +
                            $"{updateTeamResult.ErrorMessage} / " +
                            $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Update Player Role Warning",
                        nameof(UpdatePlayerRole)
                    );
                }
            }
        }

        public async Task ChangeCaptain(Guid teamId, Guid newCaptainId)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Failed to retrieve team {teamId} for captain change: " +
                        $"{teamResult.ErrorMessage}"
                    ),
                    "Team Change Captain Warning",
                    nameof(ChangeCaptain)
                );
                return; // Exit if team not found
            }
            var team = teamResult.Data;

            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Team {teamId} not found for captain change after successful retrieval."
                    ),
                    "Team Change Captain Warning",
                    nameof(ChangeCaptain)
                );
                return; // Exit if team not found
            }

            // Find current captain
            var currentCaptain = team.TeamMembers.FirstOrDefault(m => m.Role == TeamRole.Captain);
            if (currentCaptain != null)
            {
                // Demote current captain to Core
                currentCaptain.Role = TeamRole.Core;
                // Remove manager status from outgoing captain
                currentCaptain.IsTeamManager = false;
            }

            // Find new captain
            var newCaptain = team.TeamMembers.FirstOrDefault(m => m.PlayerId == newCaptainId);
            if (newCaptain != null)
            {
                // Promote new captain
                newCaptain.Role = TeamRole.Captain;
                // Ensure new captain has manager status
                newCaptain.IsTeamManager = true;
                // Update team captain ID
                team.TeamCaptainId = newCaptainId;
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(new Exception(
                            $"Failed to change captain for team {teamId}: " +
                            $"{updateTeamResult.ErrorMessage} / " +
                            $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Change Captain Warning",
                        nameof(ChangeCaptain)
                    );
                }
            }
        }

        public async Task SetTeamManagerStatus(Guid teamId, Guid playerId, bool isTeamManager)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Failed to retrieve team {teamId} for team manager status update: " +
                        $"{teamResult.ErrorMessage}"
                    ),
                    "Team Set Manager Status Warning",
                    nameof(SetTeamManagerStatus)
                );
                return; // Exit if team not found
            }
            var team = teamResult.Data;

            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Team {teamId} not found for team manager status update after successful retrieval."
                    ),
                    "Team Set Manager Status Warning",
                    nameof(SetTeamManagerStatus)
                );
                return; // Exit if team not found
            }

            var member = team.TeamMembers.FirstOrDefault(m => m.PlayerId == playerId);
            if (member != null)
            {
                // Captains are always team managers and cannot be demoted
                if (member.Role == TeamRole.Captain && !isTeamManager)
                {
                    throw new InvalidOperationException("Team captains cannot have their manager status removed");
                }

                member.IsTeamManager = isTeamManager;
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(new Exception(
                            $"Failed to set team manager status for team {teamId}: " +
                            $"{updateTeamResult.ErrorMessage} / " +
                            $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Set Manager Status Warning",
                        nameof(SetTeamManagerStatus)
                    );
                }
            }
        }

        public async Task DeactivatePlayer(Guid teamId, Guid playerId)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Failed to retrieve team {teamId} for player deactivation: " +
                        $"{teamResult.ErrorMessage}"
                    ),
                    "Team Deactivate Player Warning",
                    nameof(DeactivatePlayer)
                );
                return; // Exit if team not found
            }
            var team = teamResult.Data;

            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Team {teamId} not found for player deactivation after successful retrieval."
                    ),
                    "Team Deactivate Player Warning",
                    nameof(DeactivatePlayer)
                );
                return; // Exit if team not found
            }

            var member = team.TeamMembers.FirstOrDefault(m => m.PlayerId == playerId);
            if (member != null)
            {
                member.IsActive = false;
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(new Exception(
                            $"Failed to deactivate player {playerId} in team {teamId}: " +
                            $"{updateTeamResult.ErrorMessage} / " +
                            $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Deactivate Player Warning",
                        nameof(DeactivatePlayer)
                    );
                }
            }
        }

        public async Task ReactivatePlayer(Guid teamId, Guid playerId)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                // Log but don't fail if team not found (already reactivated or deleted)
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Failed to retrieve team {teamId} for reactivation: " +
                        $"{teamResult.ErrorMessage}"
                    ),
                    "Team Reactivation Warning",
                    nameof(ReactivatePlayer)
                );
                return; // Exit if team not found
            }
            var team = teamResult.Data;

            if (team is null)
            {
                // Log but don't fail if team not found (already reactivated or deleted)
                await CoreService.ErrorHandler.CaptureAsync(new Exception(
                        $"Team {teamId} not found for reactivation after successful retrieval."
                    ),
                    "Team Reactivation Warning",
                    nameof(ReactivatePlayer)
                );
                return; // Exit if team not found
            }

            var member = team.TeamMembers.FirstOrDefault(m => m.PlayerId == playerId);
            if (member != null)
            {
                member.IsActive = true;
                var updateTeamRepoResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team,
                    DatabaseComponent.Cache);

                if (!updateTeamRepoResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(new Exception(
                            $"Failed to reactivate player {playerId} in team {teamId}: " +
                            $"{updateTeamRepoResult.ErrorMessage} / " +
                            $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Reactivation Warning",
                        nameof(ReactivatePlayer)
                    );
                }
            }
        }

        // Moved from StatsCore
        public async Task<Result> UpdateScrimmageStats(Guid teamId, TeamSize teamSize, bool isWin)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                return Result.Failure(
                    $"Failed to retrieve team {teamId}: {teamResult.ErrorMessage}"
                );
            }
            var team = teamResult.Data;

            if (team is null)
            {
                return Result.Failure($"Team with ID {teamId} not found.");
            }

            if (!team.ScrimmageTeamStats.TryGetValue(teamSize, out var stats))
            {
                stats = new Common.ScrimmageTeamStats { TeamId = teamId, TeamSize = teamSize, };
                team.ScrimmageTeamStats.Add(teamSize, stats);
            }

            if (isWin)
            {
                stats.Wins++;
                stats.CurrentStreak = Math.Max(0, stats.CurrentStreak) + 1;
            }
            else
            {
                stats.Losses++;
                stats.CurrentStreak = Math.Min(0, stats.CurrentStreak) - 1;
            }

            stats.LongestStreak = Math.Max(Math.Abs(stats.CurrentStreak),
                stats.LongestStreak);
            stats.LastMatchAt = DateTime.UtcNow;
            stats.LastUpdated = DateTime.UtcNow;

            var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                DatabaseComponent.Repository);
            var updateStatsResult = await CoreService.ScrimmageTeamStats.UpdateAsync(stats,
                DatabaseComponent.Repository); // Update individual stats object

            if (!updateTeamResult.Success || !updateStatsResult.Success)
            {
                return Result.Failure(
                    $"Failed to update stats for team {teamId}: " +
                    $"{updateTeamResult.ErrorMessage} / " +
                    $"{updateStatsResult.ErrorMessage}"
                );
            }

            return Result.CreateSuccess();
        }

        // Moved from StatsCore
        public async Task<Result> UpdateScrimmageRating(Guid teamId, TeamSize teamSize, double newRating)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId,
                DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                return Result.Failure(
                    $"Failed to retrieve team {teamId}: {teamResult.ErrorMessage}"
                );
            }
            var team = teamResult.Data;

            if (team is null)
            {
                return Result.Failure($"Team with ID {teamId} not found.");
            }

            if (!team.ScrimmageTeamStats.TryGetValue(teamSize, out var stats))
            {
                stats = new Common.ScrimmageTeamStats { TeamId = teamId, TeamSize = teamSize, };
                team.ScrimmageTeamStats.Add(teamSize, stats);
            }

            stats.CurrentRating = newRating;
            stats.HighestRating = Math.Max(stats.HighestRating, newRating);
            stats.LastUpdated = DateTime.UtcNow;

            var updateTeamResult = await CoreService.Teams.UpdateAsync(team,
                DatabaseComponent.Repository);
            var updateStatsResult = await CoreService.ScrimmageTeamStats.UpdateAsync(stats,
                DatabaseComponent.Repository); // Update individual stats object

            if (!updateTeamResult.Success || !updateStatsResult.Success)
            {
                return Result.Failure(
                    $"Failed to update scrimmage rating for team {teamId}: " +
                    $"{updateTeamResult.ErrorMessage} / " +
                    $"{updateStatsResult.ErrorMessage}"
                );
            }

            return Result.CreateSuccess();
        }

        /// <summary>
        /// Updates the Tournament stats for a team
        /// </summary>
        public async Task<Result> UpdateTournamentStats(Guid teamId, TeamSize teamSize, bool isWin)
        {
            // TODO: Implement Tournament stats update
            return Result.CreateSuccess();
        }

        /// <summary>
        /// Updates the Tournament rating for a team
        /// </summary>
        public async Task<Result> UpdateTournamentRating(Guid teamId, TeamSize teamSize, double newRating)
        {
            // TODO: Implement Tournament rating update
            return Result.CreateSuccess();
        }

        public static class Accessors
        {
            public static List<Guid> GetTeamManagerIds(Team team)
            {
                return team.TeamMembers.Where(m => m.IsActive && m.IsTeamManager).Select(m => m.PlayerId).ToList();
            }

            public static List<TeamMember> GetActiveMembers(Team team)
            {
                return team.TeamMembers.Where(m => m.IsActive).ToList();
            }

            public static TeamMember? GetMember(Team team, Guid playerId)
            {
                return team.TeamMembers.FirstOrDefault(m => m.PlayerId == playerId);
            }

            public static bool HasPlayer(Team team, Guid playerId)
            {
                return team.TeamMembers.Any(m => m.PlayerId == playerId);
            }

            public static bool HasActivePlayer(Team team, Guid playerId)
            {
                return team.TeamMembers.Any(m => m.PlayerId == playerId && m.IsActive);
            }

            public static int GetActivePlayerCount(Team team)
            {
                return team.TeamMembers.Count(m => m.IsActive);
            }

            public static List<Guid> GetActivePlayerIds(Team team)
            {
                return team.TeamMembers.Where(m => m.IsActive).Select(m => m.PlayerId).ToList();
            }

            public static List<Guid> GetCorePlayerIds(Team team)
            {
                return team.TeamMembers.Where(m => m.IsActive && (m.Role == TeamRole.Core || m.Role == TeamRole.Captain)).Select(m => m.PlayerId).ToList();
            }

            public static List<Guid> GetCaptainIds(Team team)
            {
                return team.TeamMembers.Where(m => m.IsActive && m.Role == TeamRole.Captain).Select(m => m.PlayerId).ToList();
            }

            public static bool IsCaptain(Team team, Guid playerId)
            {
                return team.TeamMembers.Any(m => m.PlayerId == playerId && m.IsActive && m.Role == TeamRole.Captain);
            }

            public static bool IsTeamManager(Team team, Guid playerId)
            {
                return team.TeamMembers.Any(m => m.PlayerId == playerId && m.IsActive && m.IsTeamManager);
            }

            public static bool IsValidForTeamSize(Team team, TeamSize teamSize)
            {
                if (team is null)
                {
                    return false;
                }
                var requiredPlayers = teamSize switch
                {
                    TeamSize.TwoVTwo => 2,
                    TeamSize.ThreeVThree => 3,
                    TeamSize.FourVFour => 4,
                    _ => 1
                };

                return GetActivePlayerCount(team) >= requiredPlayers;
            }
        }

        public static class Validation
        {
            // ------------------------ Validation Logic -----------------------------------
            // Future: Add business rule validations here
        }

        // Stats moved to TeamCore.Stats partial
    }
}