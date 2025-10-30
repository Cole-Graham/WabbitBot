using System.Linq;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Interfaces;
using WabbitBot.Core.Common.Services;

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
                Guid TeamMajorId,
                TeamType teamType,
                DateTime createdAt,
                DateTime lastActive
            )
            {
                return new Team
                {
                    Id = id,
                    Name = name,
                    TeamMajorId = TeamMajorId,
                    TeamType = teamType,
                    LastActive = lastActive,
                    CreatedAt = createdAt,
                };
            }

            public static TeamRoster CreateRoster(
                Guid teamId,
                TeamSizeRosterGroup rosterGroup,
                Guid rosterCaptainId,
                Guid mashinaUserId,
                string discordUserId
            )
            {
                var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
                var maxSlots = GetMaxRosterSlots(rosterGroup, scrimmageConfig.TeamRules);

                var roster = new TeamRoster
                {
                    Id = Guid.NewGuid(),
                    TeamId = teamId,
                    RosterGroup = rosterGroup,
                    MaxRosterSize = maxSlots,
                    LastActive = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                };

                roster.RosterMembers.Add(
                    new TeamMember
                    {
                        TeamRosterId = roster.Id,
                        MashinaUserId = mashinaUserId,
                        PlayerId = rosterCaptainId,
                        DiscordUserId = discordUserId,
                        Role = RosterRole.Captain,
                        ValidFrom = DateTime.UtcNow,
                        IsRosterManager = true,
                    }
                );

                return roster;
            }

            private static int GetMaxRosterSlots(TeamSizeRosterGroup group, TeamRules rules)
            {
                return group switch
                {
                    TeamSizeRosterGroup.Solo => rules.Solo.MaxRosterSlots,
                    TeamSizeRosterGroup.Duo => rules.Duo.MaxRosterSlots,
                    TeamSizeRosterGroup.Squad => rules.Squad.MaxRosterSlots,
                    _ => rules.Solo.MaxRosterSlots,
                };
            }

            // role caps helper moved to TeamSizeRosterGrouping
        }

        public static class TeamSizeRosterGrouping
        {
            public static TeamSizeRosterGroup GetRosterGroup(TeamSize teamSize)
            {
                return teamSize switch
                {
                    TeamSize.OneVOne => TeamSizeRosterGroup.Solo,
                    TeamSize.TwoVTwo => TeamSizeRosterGroup.Duo,
                    TeamSize.ThreeVThree => TeamSizeRosterGroup.Squad,
                    TeamSize.FourVFour => TeamSizeRosterGroup.Squad,
                    _ => TeamSizeRosterGroup.Solo,
                };
            }

            public static int GetMaxRosterSlots(TeamSizeRosterGroup group)
            {
                var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
                return group switch
                {
                    TeamSizeRosterGroup.Solo => scrimmageConfig.TeamRules.Solo.MaxRosterSlots,
                    TeamSizeRosterGroup.Duo => scrimmageConfig.TeamRules.Duo.MaxRosterSlots,
                    TeamSizeRosterGroup.Squad => scrimmageConfig.TeamRules.Squad.MaxRosterSlots,
                    _ => scrimmageConfig.TeamRules.Solo.MaxRosterSlots,
                };
            }

            public static (int CaptainSlots, int CoreSlots) GetRosterRoleCaps(TeamSizeRosterGroup group)
            {
                var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
                var rules = scrimmageConfig.TeamRules;
                return group switch
                {
                    TeamSizeRosterGroup.Solo => (rules.Solo.CaptainRosterSlots, rules.Solo.CoreRosterSlots),
                    TeamSizeRosterGroup.Duo => (rules.Duo.CaptainRosterSlots, rules.Duo.CoreRosterSlots),
                    TeamSizeRosterGroup.Squad => (rules.Squad.CaptainRosterSlots, rules.Squad.CoreRosterSlots),
                    _ => (rules.Solo.CaptainRosterSlots, rules.Solo.CoreRosterSlots),
                };
            }
        }

        private static bool IsConsideredCaptain(Guid playerId, Guid teamMajorId, RosterRole? role)
        {
            return playerId == teamMajorId || role == RosterRole.Captain;
        }

        public async Task UpdateLastActive(Guid teamId)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId, DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    new Exception(
                        $"Failed to retrieve team {teamId} for last active update: " + $"{teamResult.ErrorMessage}"
                    ),
                    "Team Last Active Warning",
                    nameof(UpdateLastActive)
                );
                return; // Exit if team not found
            }
            var team = teamResult.Data;

            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    new Exception($"Team {teamId} not found for last active update after successful retrieval."),
                    "Team Last Active Warning",
                    nameof(UpdateLastActive)
                );
                return; // Exit if team not found
            }

            team.LastActive = DateTime.UtcNow;
            var updateTeamRepoResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Repository);
            var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Cache);

            if (!updateTeamRepoResult.Success || !updateTeamCacheResult.Success)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    new Exception(
                        $"Failed to update last active for team {teamId}: "
                            + $"{updateTeamRepoResult.ErrorMessage} / "
                            + $"{updateTeamCacheResult.ErrorMessage}"
                    ),
                    "Team Last Active Warning",
                    nameof(UpdateLastActive)
                );
            }
        }

        public async Task AddPlayer(
            Guid teamId,
            Guid playerId,
            TeamSizeRosterGroup rosterGroup,
            RosterRole role = RosterRole.Core
        )
        {
            await AddPlayerInternal(teamId, playerId, rosterGroup, role, bypassCooldown: false);
        }

        public async Task AddPlayerBypassingCooldown(
            Guid teamId,
            Guid playerId,
            TeamSizeRosterGroup rosterGroup,
            RosterRole role = RosterRole.Core
        )
        {
            await AddPlayerInternal(teamId, playerId, rosterGroup, role, bypassCooldown: true);
        }

        private static async Task AddPlayerInternal(
            Guid teamId,
            Guid playerId,
            TeamSizeRosterGroup rosterGroup,
            RosterRole role,
            bool bypassCooldown
        )
        {
            // Enforce player membership limit for the roster group
            var membershipLimitResult = await Validation.ValidateMembershipLimit(playerId, rosterGroup);
            if (!membershipLimitResult.Success)
            {
                throw new InvalidOperationException(membershipLimitResult.ErrorMessage);
            }

            // Use a single EF DbContext to load, modify, and persist
            await CoreService.WithDbContext(async db =>
            {
                var team = await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId);
                if (team is null)
                {
                    throw new InvalidOperationException($"Team with ID {teamId} not found.");
                }

                // Enforce rejoin cooldown for this team
                var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
                var player = await db.Players.Include(p => p.MashinaUser).FirstOrDefaultAsync(p => p.Id == playerId);
                if (player is null)
                {
                    throw new InvalidOperationException($"Player {playerId} not found.");
                }
                if (
                    !bypassCooldown
                    && player.TeamJoinCooldowns.TryGetValue(teamId, out var unlockAt)
                    && unlockAt > DateTime.UtcNow
                )
                {
                    var remaining = unlockAt - DateTime.UtcNow;
                    throw new InvalidOperationException(
                        $"Player cannot rejoin this team yet. Try again in {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m."
                    );
                }

                // Validate roster group is allowed for this team type
                var validationResult = Validation.ValidateRosterGroupForTeamType(team.TeamType, rosterGroup);
                if (!validationResult.Success)
                {
                    throw new InvalidOperationException(validationResult.ErrorMessage);
                }

                var roster = Accessors.GetRosterForGroup(team, rosterGroup);
                if (roster is null)
                {
                    throw new InvalidOperationException($"Roster for group {rosterGroup} not found for team {teamId}");
                }
                if (roster.RosterMembers.Count(m => m.ValidTo == null) >= roster.MaxRosterSize)
                {
                    throw new InvalidOperationException($"Roster is full (max size: {roster.MaxRosterSize})");
                }
                if (roster.RosterMembers.Any(m => m.PlayerId == playerId && m.ValidTo == null))
                {
                    throw new InvalidOperationException("Player is already on this roster");
                }

                // Enforce role caps from TeamRules (Major is counted as a captain when active on the roster)
                var (captainCap, coreCap) = TeamSizeRosterGrouping.GetRosterRoleCaps(rosterGroup);
                var consideredCaptainCount = roster.RosterMembers.Count(m =>
                    m.ValidTo == null && IsConsideredCaptain(m.PlayerId, team.TeamMajorId, m.Role)
                );

                bool newMemberIsConsideredCaptain = role == RosterRole.Captain || playerId == team.TeamMajorId;

                if (newMemberIsConsideredCaptain && consideredCaptainCount >= captainCap)
                {
                    throw new InvalidOperationException(
                        $"This roster already has the maximum of {captainCap} captains"
                    );
                }

                if (role == RosterRole.Core)
                {
                    var currentCores = roster.RosterMembers.Count(m => m.ValidTo == null && m.Role == RosterRole.Core);
                    if (currentCores >= coreCap)
                    {
                        throw new InvalidOperationException(
                            $"This roster already has the maximum of {coreCap} core players"
                        );
                    }
                }

                // Add member with required identity fields (direct DbSet add to avoid accidental updates)
                var discordUserId = player.MashinaUser?.DiscordUserId.ToString() ?? string.Empty;
                var newMember = new TeamMember
                {
                    TeamRosterId = roster.Id,
                    MashinaUserId = player.MashinaUserId,
                    PlayerId = playerId,
                    DiscordUserId = discordUserId,
                    Role = role,
                    ValidFrom = DateTime.UtcNow,
                    IsRosterManager = role == RosterRole.Captain,
                };
                await db.TeamMembers.AddAsync(newMember);

                // If adding a considered captain (Captain role or Team Major), update CaptainChangedAt
                if (newMemberIsConsideredCaptain)
                {
                    roster.CaptainChangedAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
            });
        }

        // Removing players is handled via deactivation

        public async Task UpdatePlayerRole(Guid teamId, Guid playerId, RosterRole newRole, bool isMod = false)
        {
            var team = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );

            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    new Exception($"Team {teamId} not found for player role update."),
                    "Team Update Player Role Warning",
                    nameof(UpdatePlayerRole)
                );
                return; // Exit if team not found
            }

            var member = team.Rosters.SelectMany(r => r.RosterMembers).FirstOrDefault(m => m.PlayerId == playerId);
            if (member != null)
            {
                // Enforce caps when changing roles
                var roster = team.Rosters.First(r => r.RosterMembers.Contains(member));
                var (captainCap, coreCap) = TeamSizeRosterGrouping.GetRosterRoleCaps(roster.RosterGroup);

                // Enforce core role change cooldown with override per group
                var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
                int effectiveCoreCooldownDays = scrimmageConfig.TeamConfig.BaseConfig.ChangeCoreCooldownDays;
                var overrideDays = roster.RosterGroup switch
                {
                    TeamSizeRosterGroup.Solo => scrimmageConfig.TeamConfig.Solo.ChangeCoreCooldownDays,
                    TeamSizeRosterGroup.Duo => scrimmageConfig.TeamConfig.Duo.ChangeCoreCooldownDays,
                    TeamSizeRosterGroup.Squad => scrimmageConfig.TeamConfig.Squad.ChangeCoreCooldownDays,
                    _ => null,
                };
                if (overrideDays.HasValue)
                {
                    effectiveCoreCooldownDays = overrideDays.Value;
                }
                bool touchesCore =
                    member.Role != newRole && (newRole == RosterRole.Core || member.Role == RosterRole.Core);
                if (touchesCore && roster.CoreRoleChangedAt.HasValue)
                {
                    var unlock = roster.CoreRoleChangedAt.Value.AddDays(effectiveCoreCooldownDays);
                    if (unlock > DateTime.UtcNow && !isMod)
                    {
                        var remaining = unlock - DateTime.UtcNow;
                        throw new InvalidOperationException(
                            $"Role change cooldown active for this roster. Try again in {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m."
                        );
                    }
                }

                // Count considered captains including Team Major
                var consideredCaptainCount = roster.RosterMembers.Count(m =>
                    m.ValidTo == null && IsConsideredCaptain(m.PlayerId, team.TeamMajorId, m.Role)
                );

                bool wasConsideredCaptain = IsConsideredCaptain(member.PlayerId, team.TeamMajorId, member.Role);
                bool becomesConsideredCaptain = IsConsideredCaptain(member.PlayerId, team.TeamMajorId, newRole);

                if (newRole == RosterRole.Captain)
                {
                    // Only enforce if this change increases the number of considered captains
                    if (!wasConsideredCaptain && becomesConsideredCaptain && consideredCaptainCount >= captainCap)
                    {
                        throw new InvalidOperationException(
                            $"This roster already has the maximum of {captainCap} captains"
                        );
                    }
                }
                else if (newRole == RosterRole.Core)
                {
                    var currentCores = roster.RosterMembers.Count(m => m.ValidTo == null && m.Role == RosterRole.Core);
                    if (member.Role != RosterRole.Core && currentCores >= coreCap)
                    {
                        throw new InvalidOperationException(
                            $"This roster already has the maximum of {coreCap} core players"
                        );
                    }
                }

                member.Role = newRole;
                // Captains are automatically team managers
                if (newRole == RosterRole.Captain)
                {
                    member.IsRosterManager = true;
                }
                if (touchesCore)
                {
                    roster.CoreRoleChangedAt = DateTime.UtcNow;
                }

                // Update CaptainChangedAt if considered captain boundary changed for this member
                if (wasConsideredCaptain != becomesConsideredCaptain)
                {
                    roster.CaptainChangedAt = DateTime.UtcNow;
                }
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new Exception(
                            $"Failed to update player role for team {teamId}: "
                                + $"{updateTeamResult.ErrorMessage} / "
                                + $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Update Player Role Warning",
                        nameof(UpdatePlayerRole)
                    );
                }
            }
        }

        public async Task ClearPlayerRole(Guid teamId, Guid playerId, bool clearManager)
        {
            // Load necessary navigation properties within the same DbContext to avoid lazy-load after disposal
            var team = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );

            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    new Exception($"Team {teamId} not found for clearing player role."),
                    "Team Clear Player Role Warning",
                    nameof(ClearPlayerRole)
                );
                return;
            }

            var member = team.Rosters.SelectMany(r => r.RosterMembers).FirstOrDefault(m => m.PlayerId == playerId);
            if (member != null)
            {
                var roster = team.Rosters.First(r => r.RosterMembers.Contains(member));
                bool wasConsideredCaptain = IsConsideredCaptain(member.PlayerId, team.TeamMajorId, member.Role);
                member.Role = null;
                if (clearManager)
                {
                    member.IsRosterManager = false;
                }
                // If this demotion removed a considered captain (non-Major captain), mark timestamp
                bool isConsideredAfter = IsConsideredCaptain(member.PlayerId, team.TeamMajorId, member.Role);
                if (wasConsideredCaptain && !isConsideredAfter)
                {
                    roster.CaptainChangedAt = DateTime.UtcNow;
                }
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new Exception(
                            $"Failed to clear player role for team {teamId}: "
                                + $"{updateTeamResult.ErrorMessage} / {updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Clear Player Role Warning",
                        nameof(ClearPlayerRole)
                    );
                }
            }
        }

        public async Task ChangeCaptain(
            Guid teamId,
            TeamSizeRosterGroup rosterGroup,
            Guid newCaptainId,
            bool isMod = false
        )
        {
            // Load team with required navigations inside a single DbContext to avoid lazy-loading after dispose
            var team = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );
            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    new Exception($"Team {teamId} not found for captain change."),
                    "Team Change Captain Warning",
                    nameof(ChangeCaptain)
                );
                return; // Exit if team not found
            }

            // Target roster
            var roster = team.Rosters.FirstOrDefault(r => r.RosterGroup == rosterGroup);
            if (roster is null)
            {
                throw new InvalidOperationException($"Roster for group {rosterGroup} not found for team {teamId}");
            }

            // Enforce captain change cooldown per roster
            var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
            var captainCooldownDays = scrimmageConfig.TeamConfig.BaseConfig.ChangeCaptainCooldownDays;
            if (roster.CaptainChangedAt.HasValue)
            {
                var unlock = roster.CaptainChangedAt.Value.AddDays(captainCooldownDays);
                if (unlock > DateTime.UtcNow && !isMod)
                {
                    var remaining = unlock - DateTime.UtcNow;
                    throw new InvalidOperationException(
                        $"Roster captain change cooldown active. Try again in {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m."
                    );
                }
            }

            // Find current captain for this roster
            var currentCaptain = roster.RosterMembers.FirstOrDefault(m => m.Role == RosterRole.Captain);
            if (currentCaptain != null)
            {
                // Remove captain role from current captain
                currentCaptain.Role = null;
            }

            // Find new captain within this roster
            var newCaptain = roster.RosterMembers.FirstOrDefault(m => m.PlayerId == newCaptainId);
            if (newCaptain != null)
            {
                // Promote new captain
                newCaptain.Role = RosterRole.Captain;
                // Ensure new captain has manager status
                newCaptain.IsRosterManager = true;
                roster.CaptainChangedAt = DateTime.UtcNow;
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new Exception(
                            $"Failed to change captain for team {teamId}: "
                                + $"{updateTeamResult.ErrorMessage} / "
                                + $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Change Captain Warning",
                        nameof(ChangeCaptain)
                    );
                }
            }
        }

        public async Task SetTeamManagerStatus(Guid teamId, Guid playerId, bool IsRosterManager)
        {
            // Load team and members to avoid deferred nav access
            var team = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );
            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    new Exception($"Team {teamId} not found for team manager status update."),
                    "Team Set Manager Status Warning",
                    nameof(SetTeamManagerStatus)
                );
                return; // Exit if team not found
            }

            var member = team.Rosters.SelectMany(r => r.RosterMembers).FirstOrDefault(m => m.PlayerId == playerId);
            if (member != null)
            {
                // Captains are always team managers and cannot be demoted
                if (member.Role == RosterRole.Captain && !IsRosterManager)
                {
                    throw new InvalidOperationException("Team captains cannot have their manager status removed");
                }

                member.IsRosterManager = IsRosterManager;
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new Exception(
                            $"Failed to set team manager status for team {teamId}: "
                                + $"{updateTeamResult.ErrorMessage} / "
                                + $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Set Manager Status Warning",
                        nameof(SetTeamManagerStatus)
                    );
                }
            }
        }

        public async Task DeactivatePlayer(Guid teamId, Guid playerId)
        {
            // Load team with roster members to deactivate within same DbContext scope
            var team = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );
            if (team is null)
            {
                await CoreService.ErrorHandler.CaptureAsync(
                    new Exception($"Team {teamId} not found for player deactivation."),
                    "Team Deactivate Player Warning",
                    nameof(DeactivatePlayer)
                );
                return; // Exit if team not found
            }

            var member = team.Rosters.SelectMany(r => r.RosterMembers).FirstOrDefault(m => m.PlayerId == playerId);
            if (member != null)
            {
                var roster = team.Rosters.First(r => r.RosterMembers.Contains(member));
                bool wasConsideredCaptain =
                    member.ValidTo == null && IsConsideredCaptain(member.PlayerId, team.TeamMajorId, member.Role);
                member.ValidTo = DateTime.UtcNow;
                if (wasConsideredCaptain)
                {
                    roster.CaptainChangedAt = DateTime.UtcNow;
                }
                var updateTeamResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Repository);
                var updateTeamCacheResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Cache);

                if (!updateTeamResult.Success || !updateTeamCacheResult.Success)
                {
                    await CoreService.ErrorHandler.CaptureAsync(
                        new Exception(
                            $"Failed to deactivate player {playerId} in team {teamId}: "
                                + $"{updateTeamResult.ErrorMessage} / "
                                + $"{updateTeamCacheResult.ErrorMessage}"
                        ),
                        "Team Deactivate Player Warning",
                        nameof(DeactivatePlayer)
                    );
                }

                // Write rejoin cooldown for this player-team
                var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
                var days = scrimmageConfig.TeamConfig.BaseConfig.RejoinTeamCooldownDays;
                var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                if (playerResult.Success && playerResult.Data is not null)
                {
                    var p = playerResult.Data;
                    p.TeamJoinCooldowns[teamId] = DateTime.UtcNow.AddDays(days);
                    var upRepo = await CoreService.Players.UpdateAsync(p, DatabaseComponent.Repository);
                    var upCache = await CoreService.Players.UpdateAsync(p, DatabaseComponent.Cache);
                    if (!upRepo.Success || !upCache.Success)
                    {
                        await CoreService.ErrorHandler.CaptureAsync(
                            new Exception(
                                $"Failed to set rejoin cooldown for player {playerId} and team {teamId}: "
                                    + $"{upRepo.ErrorMessage} / {upCache.ErrorMessage}"
                            ),
                            "Team Deactivate Player Warning",
                            nameof(DeactivatePlayer)
                        );
                    }
                }
            }
        }

        public async Task DeleteRoster(Guid teamId, TeamSizeRosterGroup rosterGroup)
        {
            await CoreService.WithDbContext(async db =>
            {
                var team = await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId);
                if (team is null)
                {
                    throw new InvalidOperationException($"Team {teamId} not found.");
                }

                var roster = team.Rosters.FirstOrDefault(r => r.RosterGroup == rosterGroup);
                if (roster is null)
                {
                    throw new InvalidOperationException($"Roster {rosterGroup} not found for team {teamId}.");
                }

                // Deleting roster will cascade delete TeamMembers
                db.TeamRosters.Remove(roster);
                await db.SaveChangesAsync();
            });
        }

        public async Task DeleteTeam(Guid teamId)
        {
            await CoreService.WithDbContext(async db =>
            {
                // Prevent deletion if referenced by matches (restrict FKs)
                var hasMatches = await db.Matches.AnyAsync(m => m.Team1Id == teamId || m.Team2Id == teamId);
                if (hasMatches)
                {
                    throw new InvalidOperationException("Cannot delete a team that has matches.");
                }

                var team = await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId);
                if (team is null)
                {
                    throw new InvalidOperationException($"Team {teamId} not found.");
                }

                // Remove rosters (cascades to members), then team
                if (team.Rosters.Any())
                {
                    db.TeamRosters.RemoveRange(team.Rosters);
                }
                db.Teams.Remove(team);
                await db.SaveChangesAsync();
            });
        }

        // Moved from StatsCore
        public async Task<Result> UpdateScrimmageStats(Guid teamId, TeamSize teamSize, bool isWin)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId, DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                return Result.Failure($"Failed to retrieve team {teamId}: {teamResult.ErrorMessage}");
            }
            var team = teamResult.Data;

            if (team is null)
            {
                return Result.Failure($"Team with ID {teamId} not found.");
            }

            if (!team.ScrimmageTeamStats.TryGetValue(teamSize, out var stats))
            {
                stats = new Common.ScrimmageTeamStats { TeamId = teamId, TeamSize = teamSize };
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

            stats.LongestStreak = Math.Max(Math.Abs(stats.CurrentStreak), stats.LongestStreak);
            stats.LastMatchAt = DateTime.UtcNow;
            stats.LastUpdated = DateTime.UtcNow;

            var updateTeamResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Repository);
            var updateStatsResult = await CoreService.ScrimmageTeamStats.UpdateAsync(
                stats,
                DatabaseComponent.Repository
            ); // Update individual stats object

            if (!updateTeamResult.Success || !updateStatsResult.Success)
            {
                return Result.Failure(
                    $"Failed to update stats for team {teamId}: "
                        + $"{updateTeamResult.ErrorMessage} / "
                        + $"{updateStatsResult.ErrorMessage}"
                );
            }

            return Result.CreateSuccess();
        }

        // Moved from StatsCore
        public async Task<Result> UpdateScrimmageRating(Guid teamId, TeamSize teamSize, double newRating)
        {
            var teamResult = await CoreService.Teams.GetByIdAsync(teamId, DatabaseComponent.Repository);
            if (!teamResult.Success)
            {
                return Result.Failure($"Failed to retrieve team {teamId}: {teamResult.ErrorMessage}");
            }
            var team = teamResult.Data;

            if (team is null)
            {
                return Result.Failure($"Team with ID {teamId} not found.");
            }

            if (!team.ScrimmageTeamStats.TryGetValue(teamSize, out var stats))
            {
                stats = new Common.ScrimmageTeamStats { TeamId = teamId, TeamSize = teamSize };
                team.ScrimmageTeamStats.Add(teamSize, stats);
            }

            stats.CurrentRating = newRating;
            stats.HighestRating = Math.Max(stats.HighestRating, newRating);
            stats.LastUpdated = DateTime.UtcNow;

            var updateTeamResult = await CoreService.Teams.UpdateAsync(team, DatabaseComponent.Repository);
            var updateStatsResult = await CoreService.ScrimmageTeamStats.UpdateAsync(
                stats,
                DatabaseComponent.Repository
            ); // Update individual stats object

            if (!updateTeamResult.Success || !updateStatsResult.Success)
            {
                return Result.Failure(
                    $"Failed to update scrimmage rating for team {teamId}: "
                        + $"{updateTeamResult.ErrorMessage} / "
                        + $"{updateStatsResult.ErrorMessage}"
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
            await Task.CompletedTask;
            return Result.CreateSuccess();
        }

        /// <summary>
        /// Updates the Tournament rating for a team
        /// </summary>
        public async Task<Result> UpdateTournamentRating(Guid teamId, TeamSize teamSize, double newRating)
        {
            // TODO: Implement Tournament rating update
            await Task.CompletedTask;
            return Result.CreateSuccess();
        }

        public static class Accessors
        {
            public static List<Guid> GetTeamManagerIds(Team team)
            {
                return team
                    .Rosters.SelectMany(r => r.RosterMembers)
                    .Where(m => m.ValidTo == null && m.IsRosterManager)
                    .Select(m => m.PlayerId)
                    .Distinct()
                    .ToList();
            }

            public static List<TeamMember> GetActiveMembers(Team team)
            {
                return team.Rosters.SelectMany(r => r.RosterMembers).Where(m => m.ValidTo == null).ToList();
            }

            public static List<TeamMember> GetActiveMembersForRosterGroup(Team team, TeamSizeRosterGroup rosterGroup)
            {
                return team
                    .Rosters.Where(r => r.RosterGroup == rosterGroup)
                    .SelectMany(r => r.RosterMembers)
                    .Where(m => m.ValidTo == null)
                    .ToList();
            }

            public static TeamMember? GetMember(Team team, Guid playerId)
            {
                return team.Rosters.SelectMany(r => r.RosterMembers).FirstOrDefault(m => m.PlayerId == playerId);
            }

            public static bool HasPlayer(Team team, Guid playerId)
            {
                return team.Rosters.SelectMany(r => r.RosterMembers).Any(m => m.PlayerId == playerId);
            }

            public static bool HasActivePlayer(Team team, Guid playerId)
            {
                return team
                    .Rosters.SelectMany(r => r.RosterMembers)
                    .Any(m => m.PlayerId == playerId && m.ValidTo == null);
            }

            public static bool HasActivePlayerInRosterGroup(Team team, Guid playerId, TeamSizeRosterGroup rosterGroup)
            {
                return team
                    .Rosters.Where(r => r.RosterGroup == rosterGroup)
                    .SelectMany(r => r.RosterMembers)
                    .Any(m => m.PlayerId == playerId && m.ValidTo == null);
            }

            public static int GetActivePlayerCount(Team team)
            {
                return team.Rosters.SelectMany(r => r.RosterMembers).Count(m => m.ValidTo == null);
            }

            public static int GetActivePlayerCountForRosterGroup(Team team, TeamSizeRosterGroup rosterGroup)
            {
                return team
                    .Rosters.Where(r => r.RosterGroup == rosterGroup)
                    .SelectMany(r => r.RosterMembers)
                    .Count(m => m.ValidTo == null);
            }

            public static List<Guid> GetActivePlayerIds(Team team)
            {
                return team
                    .Rosters.SelectMany(r => r.RosterMembers)
                    .Where(m => m.ValidTo == null)
                    .Select(m => m.PlayerId)
                    .Distinct()
                    .ToList();
            }

            public static List<Guid> GetActivePlayerIdsForRosterGroup(Team team, TeamSizeRosterGroup rosterGroup)
            {
                return team
                    .Rosters.Where(r => r.RosterGroup == rosterGroup)
                    .SelectMany(r => r.RosterMembers)
                    .Where(m => m.ValidTo == null)
                    .Select(m => m.PlayerId)
                    .ToList();
            }

            public static List<Guid> GetCorePlayerIds(Team team)
            {
                return team
                    .Rosters.SelectMany(r => r.RosterMembers)
                    .Where(m => m.ValidTo == null && (m.Role == RosterRole.Core || m.Role == RosterRole.Captain))
                    .Select(m => m.PlayerId)
                    .Distinct()
                    .ToList();
            }

            public static List<Guid> GetCaptainIds(Team team)
            {
                var ids = team
                    .Rosters.SelectMany(r => r.RosterMembers)
                    .Where(m => m.ValidTo == null && m.Role == RosterRole.Captain)
                    .Select(m => m.PlayerId)
                    .ToHashSet();

                // Include Team Major if they are on any active roster
                var majorActive = team
                    .Rosters.SelectMany(r => r.RosterMembers)
                    .Any(m => m.ValidTo == null && m.PlayerId == team.TeamMajorId);
                if (majorActive)
                {
                    ids.Add(team.TeamMajorId);
                }

                return ids.ToList();
            }

            public static bool IsCaptain(Team team, Guid playerId)
            {
                return team
                    .Rosters.SelectMany(r => r.RosterMembers)
                    .Any(m =>
                        m.PlayerId == playerId
                        && m.ValidTo == null
                        && IsConsideredCaptain(m.PlayerId, team.TeamMajorId, m.Role)
                    );
            }

            public static bool IsRosterManager(Team team, Guid playerId)
            {
                return team
                    .Rosters.SelectMany(r => r.RosterMembers)
                    .Any(m => m.PlayerId == playerId && m.ValidTo == null && m.IsRosterManager);
            }

            public static bool IsValidForTeamSize(Team team, TeamSize teamSize)
            {
                if (team is null)
                {
                    return false;
                }

                var rosterGroup = TeamSizeRosterGrouping.GetRosterGroup(teamSize);
                var requiredPlayers = teamSize switch
                {
                    TeamSize.TwoVTwo => 2,
                    TeamSize.ThreeVThree => 3,
                    TeamSize.FourVFour => 4,
                    _ => 1,
                };

                return GetActivePlayerCountForRosterGroup(team, rosterGroup) >= requiredPlayers;
            }

            public static TeamRoster? GetRosterForGroup(Team team, TeamSizeRosterGroup rosterGroup)
            {
                return team.Rosters.FirstOrDefault(r => r.RosterGroup == rosterGroup);
            }

            public static bool HasRosterForGroup(Team team, TeamSizeRosterGroup rosterGroup)
            {
                return team.Rosters.Any(r => r.RosterGroup == rosterGroup);
            }
        }

        public static class Validation
        {
            // ------------------------ Team Type and Roster Validation -----------------------------------

            /// <summary>
            /// Validates that a team's roster configuration matches its team type.
            /// Solo teams can only have Solo rosters.
            /// Team-type teams can only have Duo and/or Squad rosters (no Solo).
            /// </summary>
            public static Result ValidateTeamRosterConfiguration(Team team)
            {
                if (team.Rosters?.Any() != true)
                {
                    return Result.CreateSuccess(); // Empty rosters are valid during creation
                }

                var rosterGroups = team.Rosters.Select(r => r.RosterGroup).Distinct().ToList();

                return team.TeamType switch
                {
                    TeamType.Solo => ValidateSoloTeamRosters(rosterGroups),
                    TeamType.Team => ValidateTeamTypeRosters(rosterGroups),
                    _ => Result.Failure($"Invalid team type: {team.TeamType}"),
                };
            }

            private static Result ValidateSoloTeamRosters(List<TeamSizeRosterGroup> rosterGroups)
            {
                if (rosterGroups.Count != 1)
                {
                    return Result.Failure("Solo teams must have exactly one roster");
                }

                if (rosterGroups[0] != TeamSizeRosterGroup.Solo)
                {
                    return Result.Failure("Solo teams can only have Solo rosters");
                }

                return Result.CreateSuccess();
            }

            private static Result ValidateTeamTypeRosters(List<TeamSizeRosterGroup> rosterGroups)
            {
                if (rosterGroups.Contains(TeamSizeRosterGroup.Solo))
                {
                    return Result.Failure("Team-type teams cannot have Solo rosters");
                }

                var validGroups = new[] { TeamSizeRosterGroup.Duo, TeamSizeRosterGroup.Squad };
                if (rosterGroups.Any(g => !validGroups.Contains(g)))
                {
                    return Result.Failure("Team-type teams can only have Duo and/or Squad rosters");
                }

                return Result.CreateSuccess();
            }

            /// <summary>
            /// Validates that a roster group can be added to a team based on its team type.
            /// </summary>
            public static Result ValidateRosterGroupForTeamType(TeamType teamType, TeamSizeRosterGroup rosterGroup)
            {
                return teamType switch
                {
                    TeamType.Solo when rosterGroup != TeamSizeRosterGroup.Solo => Result.Failure(
                        "Solo teams can only have Solo rosters"
                    ),
                    TeamType.Team when rosterGroup == TeamSizeRosterGroup.Solo => Result.Failure(
                        "Team-type teams cannot have Solo rosters"
                    ),
                    _ => Result.CreateSuccess(),
                };
            }

            /// <summary>
            /// Determines the appropriate team type based on the intended roster groups.
            /// </summary>
            public static TeamType GetTeamTypeForRosterGroups(IEnumerable<TeamSizeRosterGroup> intendedRosterGroups)
            {
                var groups = intendedRosterGroups.ToList();

                if (groups.Contains(TeamSizeRosterGroup.Solo))
                {
                    return TeamType.Solo;
                }

                return TeamType.Team;
            }

            /// <summary>
            /// Validates that the specified player has remaining membership slots for the given roster group.
            /// </summary>
            public static async Task<Result> ValidateMembershipLimit(Guid playerId, TeamSizeRosterGroup rosterGroup)
            {
                var cfg = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName).TeamConfig;
                var limit = rosterGroup switch
                {
                    TeamSizeRosterGroup.Solo => cfg.Solo.UserTeamMembershipLimitCount,
                    TeamSizeRosterGroup.Duo => cfg.Duo.UserTeamMembershipLimitCount,
                    TeamSizeRosterGroup.Squad => cfg.Squad.UserTeamMembershipLimitCount,
                    _ => cfg.Duo.UserTeamMembershipLimitCount,
                };

                var count = await CoreService.WithDbContext(async db =>
                    await db.TeamMembers.CountAsync(tm =>
                        tm.PlayerId == playerId && tm.ValidTo == null && tm.TeamRoster.RosterGroup == rosterGroup
                    )
                );

                return count < limit
                    ? Result.CreateSuccess()
                    : Result.Failure("Player has reached the membership limit for this roster group.");
            }

            /// <summary>
            /// Validates a proposed lineup against TeamRules for the given team size.
            /// A lineup is valid if either the number of captains equals the effective required captain count,
            /// or the number of core-role players equals the configured core equivalence count.
            /// </summary>
            public static async Task<Result> ValidateLineupForMatch(
                Guid teamId,
                TeamSize teamSize,
                IEnumerable<Guid> lineupPlayerIds
            )
            {
                var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
                var rules = scrimmageConfig.TeamRules;
                var rosterGroup = TeamSizeRosterGrouping.GetRosterGroup(teamSize);

                TeamMatchRules? matchRules = teamSize switch
                {
                    TeamSize.OneVOne => rules.Solo.OneVOne,
                    TeamSize.TwoVTwo => rules.Duo.TwoVTwo,
                    TeamSize.ThreeVThree => rules.Squad.ThreeVThree,
                    TeamSize.FourVFour => rules.Squad.FourVFour,
                    _ => rules.Duo.TwoVTwo,
                };

                if (matchRules is null)
                {
                    return Result.Failure("Match rules not configured for this team size.");
                }

                var effectiveCaptainsRequired = (
                    matchRules.MatchCaptainsRequiredCount ?? rules.BaseRules.MatchCaptainsRequiredCount
                );

                var lineupIds = lineupPlayerIds.ToHashSet();

                // Load lineup members and team major to compute considered captains
                var counts = await CoreService.WithDbContext(async db =>
                {
                    var teamMajorId = await db
                        .Teams.Where(t => t.Id == teamId)
                        .Select(t => t.TeamMajorId)
                        .FirstOrDefaultAsync();

                    var members = await db
                        .TeamMembers.Where(tm =>
                            tm.TeamRoster.TeamId == teamId
                            && tm.TeamRoster.RosterGroup == rosterGroup
                            && tm.ValidTo == null
                            && lineupIds.Contains(tm.PlayerId)
                        )
                        .Select(tm => new { tm.PlayerId, tm.Role })
                        .ToListAsync();

                    var captainCount = members.Count(m => IsConsideredCaptain(m.PlayerId, teamMajorId, m.Role));
                    var coreCount = members.Count(m => m.Role == RosterRole.Core);
                    return (captainCount, coreCount);
                });

                var validByCaptain = counts.captainCount == effectiveCaptainsRequired;
                var validByCore =
                    matchRules.MatchCorePlayersEqualToCaptainCount > 0
                    && counts.coreCount == matchRules.MatchCorePlayersEqualToCaptainCount;

                if (validByCaptain || validByCore)
                {
                    return Result.CreateSuccess();
                }

                var expectedCore = matchRules.MatchCorePlayersEqualToCaptainCount;
                var msg =
                    $"Lineup invalid: needs either {effectiveCaptainsRequired} captain(s) or"
                    + $" at least {expectedCore} core player(s).";
                return Result.Failure(msg);
            }

            /// <summary>
            /// Validates that a team Name and optional Tag are unique (case-insensitive, trimmed).
            /// </summary>
            public static async Task<Result> ValidateTeamUniqueness(string name, string? tag)
            {
                var normName = (name ?? string.Empty).Trim();
                var normTag = (tag ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(normName))
                {
                    return Result.Failure("Team name is required.");
                }

                var exists = await CoreService.WithDbContext(async db =>
                {
                    var nameExists = await db.Teams.AnyAsync(t => EF.Functions.ILike(t.Name, normName));
                    if (nameExists)
                    {
                        return true;
                    }
                    if (!string.IsNullOrWhiteSpace(normTag))
                    {
                        var tagExists = await db.Teams.AnyAsync(t =>
                            t.Tag != null && EF.Functions.ILike(t.Tag!, normTag)
                        );
                        if (tagExists)
                        {
                            return true;
                        }
                    }
                    return false;
                });

                return exists ? Result.Failure("A team with that name or tag already exists.") : Result.CreateSuccess();
            }
        }

        public async Task SetTeamMajor(Guid teamId, Guid memberId)
        {
            await CoreService.WithDbContext(async db =>
            {
                var team = await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId);
                if (team is null)
                {
                    return Result.Failure($"Team {teamId} not found.");
                }

                var previousMajorId = team.TeamMajorId;
                if (previousMajorId == memberId)
                {
                    return Result.CreateSuccess();
                }

                // Update timestamps on rosters where the Major presence as a considered captain changes
                foreach (var roster in team.Rosters)
                {
                    bool prevMajorActive = roster.RosterMembers.Any(m =>
                        m.ValidTo == null && m.PlayerId == previousMajorId
                    );
                    bool newMajorActive = roster.RosterMembers.Any(m => m.ValidTo == null && m.PlayerId == memberId);
                    if (prevMajorActive || newMajorActive)
                    {
                        roster.CaptainChangedAt = DateTime.UtcNow;
                    }
                }

                team.TeamMajorId = memberId;
                await db.SaveChangesAsync();
                return Result.CreateSuccess();
            });
        }

        // Stats moved to TeamCore.Stats partial
    }
}
