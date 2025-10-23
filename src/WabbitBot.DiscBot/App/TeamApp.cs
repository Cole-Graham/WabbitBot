using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Renderers;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.App.Utilities;

namespace WabbitBot.DiscBot.App
{
    public static class TeamApp
    {
        private static readonly MessageStateManager<TeamRolesState> _rolesStateManager = new();

        public static async Task<Result> ProcessSelectMenuInteractionAsync(
            DiscordClient client,
            ComponentInteractionCreatedEventArgs args
        )
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;
            var selections = interaction.Data.Values?.ToArray() ?? Array.Empty<string>();

            // Acknowledge update inline
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            if (interaction.Message is null)
            {
                return Result.Failure("Message not found");
            }

            var state = _rolesStateManager.GetOrCreateState(interaction.Message.Id);
            if (state.DiscordUserId == 0)
            {
                state.DiscordUserId = interaction.User.Id;
            }

            if (customId.StartsWith("team_roles_team_", StringComparison.Ordinal))
            {
                // Selection is a single teamId
                state.SelectedTeamId =
                    selections.FirstOrDefault() is string s && Guid.TryParse(s, out var teamId) ? teamId : null;
                // Reset dependent selections
                state.SelectedRosterGroup = null;
                state.SelectedMemberPlayerId = null;
            }
            else if (customId.StartsWith("team_roles_roster_", StringComparison.Ordinal))
            {
                // Selection is the roster group enum name
                var groupStr = selections.FirstOrDefault();
                if (
                    !string.IsNullOrEmpty(groupStr) && Enum.TryParse<TeamSizeRosterGroup>(groupStr, true, out var group)
                )
                {
                    state.SelectedRosterGroup = group;
                    state.SelectedMemberPlayerId = null; // reset member selection
                }

                // Extract teamId from customId suffix: team_roles_roster_{teamIdOrNone}
                var teamIdStr = customId.Replace("team_roles_roster_", string.Empty, StringComparison.Ordinal);
                if (state.SelectedTeamId is null && Guid.TryParse(teamIdStr, out var parsedTeamId))
                {
                    state.SelectedTeamId = parsedTeamId;
                }
            }
            else if (customId.StartsWith("team_roles_member_", StringComparison.Ordinal))
            {
                state.SelectedMemberPlayerId =
                    selections.FirstOrDefault() is string s && Guid.TryParse(s, out var pid) ? pid : null;

                // Extract teamId and rosterGroup from customId: team_roles_member_{teamIdOrNone}_{rosterGroupOrNone}
                var parts = customId.Replace("team_roles_member_", string.Empty, StringComparison.Ordinal).Split('_');
                if (parts.Length >= 2)
                {
                    if (state.SelectedTeamId is null && Guid.TryParse(parts[0], out var parsedTeamId))
                    {
                        state.SelectedTeamId = parsedTeamId;
                    }
                    if (
                        state.SelectedRosterGroup is null
                        && !string.Equals(parts[1], "none", StringComparison.OrdinalIgnoreCase)
                        && Enum.TryParse<TeamSizeRosterGroup>(parts[1], true, out var parsedGroup)
                    )
                    {
                        state.SelectedRosterGroup = parsedGroup;
                    }
                }
            }
            else
            {
                return Result.Failure($"Unknown select id: {customId}");
            }

            _rolesStateManager.SetState(interaction.Message.Id, state);

            // Re-render with latest selections
            var container = await TeamRenderer.RenderTeamRoleEditorAsync(
                state.DiscordUserId,
                state.SelectedTeamId,
                state.SelectedRosterGroup,
                state.SelectedMemberPlayerId
            );

            await interaction.EditOriginalResponseAsync(
                new DiscordWebhookBuilder().EnableV2Components().AddContainerComponent(container)
            );
            return Result.CreateSuccess();
        }

        public static async Task<Result> ProcessButtonInteractionAsync(
            DiscordClient client,
            ComponentInteractionCreatedEventArgs args
        )
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            if (interaction.Message is null)
            {
                return Result.Failure("Message not found");
            }

            var state = _rolesStateManager.GetState(interaction.Message.Id);
            if (
                state is null
                || state.SelectedTeamId is null
                || state.SelectedRosterGroup is null
                || state.SelectedMemberPlayerId is null
            )
            {
                return Result.Failure("Please select a team, roster, and member first.");
            }

            // Authorization: Requestor must be roster captain or manager on the selected roster
            var authorized = await CoreService.WithDbContext(async db =>
            {
                var player = await db.Players.FirstOrDefaultAsync(p =>
                    p.MashinaUser.DiscordUserId == interaction.User.Id
                );
                if (player is null)
                    return false;
                return await db.TeamMembers.AnyAsync(tm =>
                    tm.TeamRoster.TeamId == state.SelectedTeamId.Value
                    && tm.TeamRoster.RosterGroup == state.SelectedRosterGroup.Value
                    && tm.PlayerId == player.Id
                    && tm.ValidTo == null
                    && (tm.Role == RosterRole.Captain || tm.IsRosterManager)
                );
            });

            if (!authorized)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("You must be a roster captain or manager to edit roles.")
                        .AsEphemeral()
                );
                return Result.Failure("Unauthorized");
            }

            await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

            try
            {
                if (customId.Equals("team_roles_set_captain", StringComparison.Ordinal))
                {
                    await new TeamCore().ChangeCaptain(
                        state.SelectedTeamId.Value,
                        state.SelectedRosterGroup.Value,
                        state.SelectedMemberPlayerId.Value
                    );
                }
                else if (customId.Equals("team_roles_set_core", StringComparison.Ordinal))
                {
                    await new TeamCore().UpdatePlayerRole(
                        state.SelectedTeamId.Value,
                        state.SelectedMemberPlayerId.Value,
                        RosterRole.Core
                    );
                }
                else if (customId.Equals("team_roles_toggle_manager", StringComparison.Ordinal))
                {
                    // Load current manager flag
                    var isManager = await CoreService.WithDbContext(async db =>
                    {
                        var tm = await db.TeamMembers.FirstOrDefaultAsync(tm =>
                            tm.TeamRoster.TeamId == state.SelectedTeamId.Value
                            && tm.TeamRoster.RosterGroup == state.SelectedRosterGroup.Value
                            && tm.PlayerId == state.SelectedMemberPlayerId.Value
                        );
                        return tm?.IsRosterManager ?? false;
                    });
                    await new TeamCore().SetTeamManagerStatus(
                        state.SelectedTeamId.Value,
                        state.SelectedMemberPlayerId.Value,
                        !isManager
                    );
                }
                else if (customId.Equals("team_roles_clear", StringComparison.Ordinal))
                {
                    await new TeamCore().ClearPlayerRole(
                        state.SelectedTeamId.Value,
                        state.SelectedMemberPlayerId.Value,
                        clearManager: false
                    );
                }
                else
                {
                    return Result.Failure($"Unknown button id: {customId}");
                }

                // Refresh UI
                var container = await TeamRenderer.RenderTeamRoleEditorAsync(
                    state.DiscordUserId,
                    state.SelectedTeamId,
                    state.SelectedRosterGroup,
                    state.SelectedMemberPlayerId
                );
                await interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().EnableV2Components().AddContainerComponent(container)
                );
                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    "Failed to process team role action",
                    nameof(TeamApp)
                );
                await interaction.CreateFollowupMessageAsync(
                    new DiscordFollowupMessageBuilder().WithContent($"Failed: {ex.Message}").AsEphemeral()
                );
                return Result.Failure(ex.Message);
            }
        }
    }

    internal class TeamRolesState
    {
        public ulong DiscordUserId { get; set; }
        public Guid? SelectedTeamId { get; set; }
        public TeamSizeRosterGroup? SelectedRosterGroup { get; set; }
        public Guid? SelectedMemberPlayerId { get; set; }
    }
}
