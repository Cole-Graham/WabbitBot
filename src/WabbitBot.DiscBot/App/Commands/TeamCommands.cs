using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Providers;
using WabbitBot.DiscBot.App.Renderers;

namespace WabbitBot.DiscBot.App.Commands
{
    [Command("team")]
    [Description("Team management commands")]
    [RequireGuild]
    public sealed class TeamCommands
    {
        private static bool IsCaptainOrManager(Team team, Guid playerId)
        {
            return team
                .Rosters.SelectMany(r => r.RosterMembers)
                .Any(m =>
                    m.PlayerId == playerId && m.ValidTo == null && (m.Role == RosterRole.Captain || m.IsRosterManager)
                );
        }

        private static async Task<(bool ok, string? message)> CheckMembershipLimitAsync(
            Guid playerId,
            TeamSizeRosterGroup group
        )
        {
            var result = await TeamCore.Validation.ValidateMembershipLimit(playerId, group);
            return (result.Success, result.ErrorMessage);
        }

        [Command("create")]
        [Description("Create a new team")]
        public async Task CreateAsync(
            CommandContext ctx,
            [Description("Team name")] string name,
            [Description("Initial roster")]
            [SlashAutoCompleteProvider(typeof(TeamRosterGroupCreateAutoComplete))]
                TeamSizeRosterGroup initialGroup,
            [Description("Optional tag")] string? tag = null
        )
        {
            await ctx.DeferResponseAsync();

            // Resolve issuer player
            var issuer = await CoreService.WithDbContext(async db =>
                await db
                    .Players.Include(p => p.MashinaUser)
                    .FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == ctx.User.Id)
            );
            if (issuer is null)
            {
                await ctx.EditResponseAsync("You must be registered to create a team.");
                return;
            }

            // Solo management is automatic and not user-managed
            if (initialGroup == TeamSizeRosterGroup.Solo)
            {
                await ctx.EditResponseAsync("Solo rosters are handled automatically and cannot be created manually.");
                return;
            }
            var limitCheck = await CheckMembershipLimitAsync(issuer.Id, initialGroup);
            if (!limitCheck.ok)
            {
                await ctx.EditResponseAsync(
                    limitCheck.message ?? "You have reached the maximum number of teams for this roster group."
                );
                return;
            }

            var team = TeamCore.Factory.CreateTeam(
                Guid.NewGuid(),
                name,
                issuer.Id,
                TeamType.Team,
                DateTime.UtcNow,
                DateTime.UtcNow
            );
            team.Tag = tag;
            var createRes = await CoreService.Teams.CreateAsync(team, DatabaseComponent.Repository);
            if (!createRes.Success)
            {
                await ctx.EditResponseAsync(createRes.ErrorMessage ?? "Failed to create team.");
                return;
            }
            // Get MashinaUser for issuer
            var mashinaUser = await CoreService.WithDbContext(async db =>
                await db.MashinaUsers.FirstOrDefaultAsync(m => m.PlayerId == issuer.Id)
            );
            if (mashinaUser is null)
            {
                await ctx.EditResponseAsync("Failed to retrieve MashinaUser for issuer.");
                return;
            }
            // Create initial roster and add issuer as captain
            var roster = TeamCore.Factory.CreateRoster(
                team.Id,
                initialGroup,
                issuer.Id,
                mashinaUser.Id,
                ctx.User.Id.ToString()
            );
            await CoreService.TeamRosters.CreateAsync(roster, DatabaseComponent.Repository);
            await new TeamCore().AddPlayer(team.Id, issuer.Id, initialGroup, RosterRole.Captain);

            var container = await TeamRenderer.RenderRosterAsync(team.Id, initialGroup);
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().EnableV2Components().AddContainerComponent(container)
            );
        }

        [Command("add-player")]
        [Description("Add a player to a team's roster")]
        public async Task AddPlayerAsync(
            CommandContext ctx,
            [SlashAutoCompleteProvider(typeof(TeamNameAutoComplete))] string team,
            [Description("Player")] string playerId,
            [Description("Roster group")] TeamSizeRosterGroup rosterGroup,
            [Description("Role")] RosterRole role = RosterRole.Core
        )
        {
            await ctx.DeferResponseAsync();
            // Prevent managing Solo via commands
            if (rosterGroup == TeamSizeRosterGroup.Solo)
            {
                await ctx.EditResponseAsync(
                    "Solo rosters are handled automatically and cannot be managed via commands."
                );
                return;
            }

            if (!Guid.TryParse(team, out var teamId) || !Guid.TryParse(playerId, out var playerGuid))
            {
                await ctx.EditResponseAsync("Invalid team or player id.");
                return;
            }

            var teamEntity = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );
            if (teamEntity is null)
            {
                await ctx.EditResponseAsync("Team not found.");
                return;
            }

            // Permissions: issuer must be captain/manager
            var issuer = await CoreService.WithDbContext(async db =>
                await db
                    .Players.Include(p => p.MashinaUser)
                    .FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == ctx.User.Id)
            );
            if (issuer is null || !IsCaptainOrManager(teamEntity, issuer.Id))
            {
                await ctx.EditResponseAsync("You must be a captain or team manager to add players.");
                return;
            }

            // Membership limit for target player
            var playerLimit = await CheckMembershipLimitAsync(playerGuid, rosterGroup);
            if (!playerLimit.ok)
            {
                await ctx.EditResponseAsync(
                    playerLimit.message ?? "That player has reached the membership limit for this roster group."
                );
                return;
            }

            try
            {
                await new TeamCore().AddPlayer(teamEntity.Id, playerGuid, rosterGroup, role);
                await ctx.EditResponseAsync($"Player added to {teamEntity.Name} ({rosterGroup}).");
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync($"Failed to add player: {ex.Message}");
            }
        }

        [Command("remove-player")]
        [Description("Remove a player from the team")]
        public async Task RemovePlayerAsync(
            CommandContext ctx,
            [SlashAutoCompleteProvider(typeof(TeamNameAutoComplete))] string team,
            [SlashAutoCompleteProvider(typeof(TeamMemberAutoComplete))] string player
        )
        {
            await ctx.DeferResponseAsync();

            if (!Guid.TryParse(team, out var teamId) || !Guid.TryParse(player, out var playerId))
            {
                await ctx.EditResponseAsync("Invalid team or player id.");
                return;
            }

            var teamEntity = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );
            if (teamEntity is null)
            {
                await ctx.EditResponseAsync("Team not found.");
                return;
            }

            var issuer = await CoreService.WithDbContext(async db =>
                await db
                    .Players.Include(p => p.MashinaUser)
                    .FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == ctx.User.Id)
            );
            if (issuer is null || !IsCaptainOrManager(teamEntity, issuer.Id))
            {
                await ctx.EditResponseAsync("You must be a captain or team manager to remove players.");
                return;
            }

            await new TeamCore().DeactivatePlayer(teamEntity.Id, playerId);
            await ctx.EditResponseAsync("Player removed.");
        }

        [Command("set-role")]
        [Description("Set a player's role on the team")]
        public async Task SetRoleAsync(
            CommandContext ctx,
            [SlashAutoCompleteProvider(typeof(TeamNameAutoComplete))] string team,
            [SlashAutoCompleteProvider(typeof(TeamMemberAutoComplete))] string player,
            RosterRole role
        )
        {
            await ctx.DeferResponseAsync();

            if (!Guid.TryParse(team, out var teamId) || !Guid.TryParse(player, out var playerId))
            {
                await ctx.EditResponseAsync("Invalid team or player id.");
                return;
            }

            var teamEntity = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );
            if (teamEntity is null)
            {
                await ctx.EditResponseAsync("Team not found.");
                return;
            }

            var issuer = await CoreService.WithDbContext(async db =>
                await db
                    .Players.Include(p => p.MashinaUser)
                    .FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == ctx.User.Id)
            );
            if (issuer is null || !IsCaptainOrManager(teamEntity, issuer.Id))
            {
                await ctx.EditResponseAsync("You must be a captain or team manager to set roles.");
                return;
            }

            try
            {
                await new TeamCore().UpdatePlayerRole(teamEntity.Id, playerId, role);
                await ctx.EditResponseAsync("Role updated.");
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync($"Failed to update role: {ex.Message}");
            }
        }

        [Command("edit-roles")]
        [Description("Open the team role editor for a team you manage")]
        public async Task EditRolesAsync(
            CommandContext ctx,
            [SlashAutoCompleteProvider(typeof(WabbitBot.DiscBot.App.Providers.TeamRolesTeamAutoComplete))] string team
        )
        {
            await ctx.DeferResponseAsync();

            if (!Guid.TryParse(team, out var teamId))
            {
                await ctx.EditResponseAsync("Invalid team selection.");
                return;
            }

            var container = await TeamRenderer.RenderTeamRoleEditorAsync(ctx.User.Id, teamId);
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().EnableV2Components().AddContainerComponent(container)
            );
        }

        [Command("leave")]
        [Description("Leave a team you are in")]
        public async Task LeaveAsync(
            CommandContext ctx,
            [SlashAutoCompleteProvider(typeof(TeamNameAutoComplete))] string team
        )
        {
            await ctx.DeferResponseAsync();

            if (!Guid.TryParse(team, out var teamId))
            {
                await ctx.EditResponseAsync("Invalid team.");
                return;
            }

            var teamEntity = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );
            if (teamEntity is null)
            {
                await ctx.EditResponseAsync("Team not found.");
                return;
            }

            var issuer = await CoreService.WithDbContext(async db =>
                await db
                    .Players.Include(p => p.MashinaUser)
                    .FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == ctx.User.Id)
            );
            if (issuer is null)
            {
                await ctx.EditResponseAsync("You are not registered.");
                return;
            }

            await new TeamCore().DeactivatePlayer(teamEntity.Id, issuer.Id);
            await ctx.EditResponseAsync("You have left the team.");
        }

        [Command("info")]
        [Description("Show team info")]
        public async Task InfoAsync(
            CommandContext ctx,
            [SlashAutoCompleteProvider(typeof(TeamRosterAutoComplete))] string teamRoster
        )
        {
            await ctx.DeferResponseAsync();

            // Expecting value format: "{teamId}:{rosterGroup}"
            var parts = (teamRoster ?? string.Empty).Split(':');
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out var teamId))
            {
                await ctx.EditResponseAsync("Invalid team/roster selection.");
                return;
            }
            if (!Enum.TryParse<TeamSizeRosterGroup>(parts[1], true, out var rosterGroup))
            {
                await ctx.EditResponseAsync("Invalid roster group selection.");
                return;
            }

            var teamEntity = await CoreService.WithDbContext(async db =>
                await db.Teams.FirstOrDefaultAsync(t => t.Id == teamId)
            );
            if (teamEntity is null)
            {
                await ctx.EditResponseAsync("Team not found.");
                return;
            }
            var container = await TeamRenderer.RenderRosterAsync(teamEntity.Id, rosterGroup);
            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().EnableV2Components().AddContainerComponent(container)
            );
        }
    }
}
