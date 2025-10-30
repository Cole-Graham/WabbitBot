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

            // Mark activity for thread cleanup tracking
            DiscBotService.ThreadContainers.MarkActivity(interaction.Message.Id);

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
            var isModerator =
                interaction.User is DiscordMember modMember
                && modMember.Permissions.HasFlag(DiscordPermission.ModerateMembers);
            var container = await TeamRenderer.RenderTeamEditorAsync(
                state.DiscordUserId,
                state.SelectedTeamId,
                state.SelectedRosterGroup,
                state.SelectedMemberPlayerId,
                adminMode: isModerator,
                awaitingAddInput: state.AwaitingAddInput
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
            // Authorization: determine moderator status once
            var isModerator =
                interaction.User is DiscordMember modMember
                && modMember.Permissions.HasFlag(DiscordPermission.ModerateMembers);

            // Action classification for selection requirements
            bool isAdminAdd = customId.Equals("team_roles_admin_add_player", StringComparison.Ordinal);
            bool isAdminRemove = customId.Equals("team_roles_admin_remove_player", StringComparison.Ordinal);
            bool isDeleteRoster =
                customId.Equals("team_roles_admin_delete_roster", StringComparison.Ordinal)
                || customId.StartsWith("confirm_delete_roster_", StringComparison.Ordinal);
            bool isDeleteTeam =
                customId.Equals("team_roles_admin_delete_team", StringComparison.Ordinal)
                || customId.StartsWith("confirm_delete_team_", StringComparison.Ordinal);
            bool isCancelDestructive = customId.Equals("cancel_destructive", StringComparison.Ordinal);
            bool isStrictMemberAction =
                customId.Equals("team_roles_set_major", StringComparison.Ordinal)
                || customId.Equals("team_roles_set_captain", StringComparison.Ordinal)
                || customId.Equals("team_roles_set_core", StringComparison.Ordinal)
                || customId.Equals("team_roles_toggle_manager", StringComparison.Ordinal)
                || customId.Equals("team_roles_clear", StringComparison.Ordinal);

            if (state is null)
            {
                return Result.Failure("Please select a team and roster (and member if applicable).");
            }
            if (isStrictMemberAction)
            {
                if (
                    state.SelectedTeamId is null
                    || state.SelectedRosterGroup is null
                    || state.SelectedMemberPlayerId is null
                )
                {
                    return Result.Failure("Please select a team and roster (and member if applicable).");
                }
            }

            // Mark activity for thread cleanup tracking
            DiscBotService.ThreadContainers.MarkActivity(interaction.Message!.Id);

            // Authorization: Requestor must be roster captain/manager on the selected roster OR team major OR a Discord moderator
            // isModerator computed above

            var authorized =
                isModerator ? true
                : state.SelectedTeamId.HasValue && state.SelectedRosterGroup.HasValue
                    ? await CoreService.WithDbContext(async db =>
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
                            )
                            || await db.Teams.AnyAsync(t =>
                                t.Id == state.SelectedTeamId.Value && t.TeamMajorId == player.Id
                            );
                    })
                : false;

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

            try
            {
                // For adding a player, user replies with Discord mention
                if (customId.Equals("team_roles_admin_add_player", StringComparison.Ordinal))
                {
                    state.AwaitingAddInput = true;
                    _rolesStateManager.SetState(interaction.Message.Id, state);
                    await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                    var container2 = await TeamRenderer.RenderTeamEditorAsync(
                        state.DiscordUserId,
                        state.SelectedTeamId,
                        state.SelectedRosterGroup,
                        state.SelectedMemberPlayerId,
                        adminMode: isModerator,
                        awaitingAddInput: state.AwaitingAddInput
                    );
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().EnableV2Components().AddContainerComponent(container2)
                    );
                    return Result.CreateSuccess();
                }

                await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                if (customId.Equals("team_roles_set_major", StringComparison.Ordinal))
                {
                    if (state.SelectedTeamId is not Guid teamId || state.SelectedMemberPlayerId is not Guid memberId)
                    {
                        return Result.Failure("Please select a team first.");
                    }
                    await new TeamCore().SetTeamMajor(teamId, memberId);
                }
                else if (customId.Equals("team_roles_set_captain", StringComparison.Ordinal))
                {
                    if (
                        state.SelectedTeamId is not Guid teamId
                        || state.SelectedRosterGroup is not TeamSizeRosterGroup group
                        || state.SelectedMemberPlayerId is not Guid memberId
                    )
                    {
                        return Result.Failure("Please select a team, roster, and member first.");
                    }
                    await new TeamCore().ChangeCaptain(teamId, group, memberId, isMod: isModerator);
                }
                else if (customId.Equals("team_roles_set_core", StringComparison.Ordinal))
                {
                    if (state.SelectedTeamId is not Guid teamId || state.SelectedMemberPlayerId is not Guid memberId)
                    {
                        return Result.Failure("Please select a team and member first.");
                    }
                    await new TeamCore().UpdatePlayerRole(teamId, memberId, RosterRole.Core, isMod: isModerator);
                }
                else if (customId.Equals("team_roles_toggle_manager", StringComparison.Ordinal))
                {
                    // Load current manager flag
                    if (
                        state.SelectedTeamId is not Guid teamId
                        || state.SelectedRosterGroup is not TeamSizeRosterGroup group
                        || state.SelectedMemberPlayerId is not Guid memberId
                    )
                    {
                        return Result.Failure("Please select a team, roster, and member first.");
                    }
                    var isManager = await CoreService.WithDbContext(async db =>
                    {
                        var tm = await db.TeamMembers.FirstOrDefaultAsync(tm =>
                            tm.TeamRoster.TeamId == teamId
                            && tm.TeamRoster.RosterGroup == group
                            && tm.PlayerId == memberId
                        );
                        return tm?.IsRosterManager ?? false;
                    });
                    await new TeamCore().SetTeamManagerStatus(teamId, memberId, !isManager);
                }
                else if (customId.Equals("team_roles_clear", StringComparison.Ordinal))
                {
                    if (state.SelectedTeamId is not Guid teamId || state.SelectedMemberPlayerId is not Guid memberId)
                    {
                        return Result.Failure("Please select a team and member first.");
                    }
                    await new TeamCore().ClearPlayerRole(teamId, memberId, clearManager: false);
                }
                else if (customId.Equals("team_roles_admin_remove_player", StringComparison.Ordinal))
                {
                    if (state.SelectedMemberPlayerId is null)
                    {
                        await interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder().WithContent("Select a member to remove.").AsEphemeral()
                        );
                        return Result.Failure("Member required for removal");
                    }
                    // Prevent removing the last active member from the roster
                    var activeCount = 0;
                    if (state.SelectedTeamId.HasValue && state.SelectedRosterGroup.HasValue)
                    {
                        activeCount = await CoreService.WithDbContext(async db =>
                            await db.TeamMembers.CountAsync(tm =>
                                tm.TeamRoster.TeamId == state.SelectedTeamId.Value
                                && tm.TeamRoster.RosterGroup == state.SelectedRosterGroup.Value
                                && tm.ValidTo == null
                            )
                        );
                    }
                    if (activeCount <= 1)
                    {
                        await interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder()
                                .WithContent(
                                    "You cannot remove the last member of a roster. Use Delete Roster or Delete Team instead."
                                )
                                .AsEphemeral()
                        );
                        return Result.Failure("Cannot remove last member");
                    }
                    if (state.SelectedTeamId is not Guid teamId || state.SelectedMemberPlayerId is not Guid memberId)
                    {
                        return Result.Failure("Please select a team and member first.");
                    }
                    await new TeamCore().DeactivatePlayer(teamId, memberId);
                }
                else if (customId.Equals("team_roles_admin_delete_roster", StringComparison.Ordinal))
                {
                    if (state.SelectedTeamId is null || state.SelectedRosterGroup is null)
                    {
                        await interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder()
                                .WithContent("Select a team and roster first.")
                                .AsEphemeral()
                        );
                        return Result.Failure("Missing selection");
                    }
                    var confirm = new DiscordButtonComponent(
                        DiscordButtonStyle.Danger,
                        $"confirm_delete_roster_{state.SelectedTeamId.Value}_{state.SelectedRosterGroup.Value}",
                        "Confirm Delete Roster"
                    );
                    var cancel = new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        "cancel_destructive",
                        "Cancel"
                    );
                    var row = new DiscordActionRowComponent([confirm, cancel]);
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder()
                            .EnableV2Components()
                            .AddContainerComponent(new DiscordContainerComponent(new List<DiscordComponent> { row }))
                    );
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent("⚠️ Are you sure you want to delete this roster? This cannot be undone.")
                            .AsEphemeral()
                    );
                    return Result.CreateSuccess();
                }
                else if (customId.Equals("team_roles_admin_delete_team", StringComparison.Ordinal))
                {
                    if (state.SelectedTeamId is null)
                    {
                        await interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder().WithContent("Select a team first.").AsEphemeral()
                        );
                        return Result.Failure("Missing team");
                    }
                    var confirm = new DiscordButtonComponent(
                        DiscordButtonStyle.Danger,
                        $"confirm_delete_team_{state.SelectedTeamId.Value}",
                        "Confirm Delete Team"
                    );
                    var cancel = new DiscordButtonComponent(
                        DiscordButtonStyle.Secondary,
                        "cancel_destructive",
                        "Cancel"
                    );
                    var row = new DiscordActionRowComponent([confirm, cancel]);
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder()
                            .EnableV2Components()
                            .AddContainerComponent(new DiscordContainerComponent(new List<DiscordComponent> { row }))
                    );
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder()
                            .WithContent("⚠️ Are you sure you want to delete this team? This cannot be undone.")
                            .AsEphemeral()
                    );
                    return Result.CreateSuccess();
                }
                else if (customId.StartsWith("confirm_delete_roster_", StringComparison.Ordinal))
                {
                    var rest = customId.Replace("confirm_delete_roster_", string.Empty, StringComparison.Ordinal);
                    var parts = rest.Split('_');
                    if (parts.Length != 2 || !Guid.TryParse(parts[0], out var teamId))
                    {
                        return Result.Failure("Invalid confirm id");
                    }
                    if (!Enum.TryParse<TeamSizeRosterGroup>(parts[1], true, out var group))
                    {
                        return Result.Failure("Invalid roster group");
                    }
                    try
                    {
                        await new TeamCore().DeleteRoster(teamId, group);
                        state.SelectedMemberPlayerId = null;
                        state.SelectedRosterGroup = null;
                        _rolesStateManager.SetState(interaction.Message.Id, state);
                    }
                    catch (Exception ex)
                    {
                        await interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder()
                                .WithContent($"Failed to delete roster: {ex.Message}")
                                .AsEphemeral()
                        );
                        return Result.Failure(ex.Message);
                    }
                }
                else if (customId.StartsWith("confirm_delete_team_", StringComparison.Ordinal))
                {
                    var idStr = customId.Replace("confirm_delete_team_", string.Empty, StringComparison.Ordinal);
                    if (!Guid.TryParse(idStr, out var teamId))
                    {
                        return Result.Failure("Invalid confirm id");
                    }
                    try
                    {
                        await new TeamCore().DeleteTeam(teamId);
                        state.SelectedTeamId = null;
                        state.SelectedRosterGroup = null;
                        state.SelectedMemberPlayerId = null;
                        _rolesStateManager.SetState(interaction.Message.Id, state);
                    }
                    catch (Exception ex)
                    {
                        await interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder()
                                .WithContent($"Failed to delete team: {ex.Message}")
                                .AsEphemeral()
                        );
                        return Result.Failure(ex.Message);
                    }
                }
                else if (customId.Equals("cancel_destructive", StringComparison.Ordinal))
                {
                    // Re-render original container to clear any pending confirmation UI
                    var updatedContainer = await TeamRenderer.RenderTeamEditorAsync(
                        state.DiscordUserId,
                        state.SelectedTeamId,
                        state.SelectedRosterGroup,
                        state.SelectedMemberPlayerId,
                        adminMode: isModerator,
                        awaitingAddInput: state.AwaitingAddInput
                    );
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().EnableV2Components().AddContainerComponent(updatedContainer)
                    );
                    // Optional feedback
                    await interaction.CreateFollowupMessageAsync(
                        new DiscordFollowupMessageBuilder().WithContent("Cancelled.").AsEphemeral()
                    );
                    return Result.CreateSuccess();
                }
                else
                {
                    return Result.Failure($"Unknown button id: {customId}");
                }

                // Refresh UI
                var container = await TeamRenderer.RenderTeamEditorAsync(
                    state.DiscordUserId,
                    state.SelectedTeamId,
                    state.SelectedRosterGroup,
                    state.SelectedMemberPlayerId,
                    adminMode: isModerator,
                    awaitingAddInput: state.AwaitingAddInput
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

        public static async Task<Result> ProcessModalSubmitAsync(DiscordClient client, ModalSubmittedEventArgs args)
        {
            var interaction = args.Interaction;
            var customId = interaction.Data.CustomId;

            if (!customId.Equals("team_roles_admin_add_modal", StringComparison.Ordinal))
            {
                return Result.CreateSuccess();
            }

            // Recover state for message/thread
            if (interaction.Message is null)
            {
                return Result.Failure("Message not found");
            }

            var state = _rolesStateManager.GetState(interaction.Message.Id);
            if (state is null || state.SelectedTeamId is null || state.SelectedRosterGroup is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Select team and roster first.").AsEphemeral()
                );
                return Result.Failure("Missing team/roster selection");
            }

            // Extract input
            var input =
                (
                    interaction.Data.Components?.FirstOrDefault(c => c.CustomId == "discord_user")
                    as DiscordTextInputComponent
                )?.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Discord user is required.").AsEphemeral()
                );
                return Result.Failure("No user provided");
            }

            // Resolve Discord user ID from mention or raw ID
            static bool TryParseDiscordId(string s, out ulong id)
            {
                s = s.Trim();
                if (s.StartsWith("<@") && s.EndsWith(">", StringComparison.Ordinal))
                {
                    var inner = s.Trim('<', '>', '!', '@');
                    return ulong.TryParse(inner, out id);
                }
                if (s.StartsWith("@", StringComparison.Ordinal))
                {
                    // Username mention format: not resolvable without guild lookup
                    id = 0;
                    return false;
                }
                return ulong.TryParse(s, out id);
            }

            ulong discordUserId;
            if (!TryParseDiscordId(input, out discordUserId))
            {
                // Try to resolve by username within the guild
                if (interaction.Guild is null)
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("Provide a valid Discord mention or ID.")
                            .AsEphemeral()
                    );
                    return Result.Failure("Unresolvable user");
                }
                var name = input.TrimStart('@');
                var member = interaction.Guild.Members.Values.FirstOrDefault(m =>
                    string.Equals(m.GlobalName, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(m.Username, name, StringComparison.OrdinalIgnoreCase)
                );
                if (member is null)
                {
                    await interaction.CreateResponseAsync(
                        DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("User not found in guild.").AsEphemeral()
                    );
                    return Result.Failure("User not found");
                }
                discordUserId = member.Id;
            }

            // Lookup PlayerId via MashinaUser
            var player = await CoreService.WithDbContext(async db =>
                await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == discordUserId)
            );
            if (player is null)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("That Discord user is not registered.")
                        .AsEphemeral()
                );
                return Result.Failure("Player not found");
            }

            // Membership limit validation
            var limit = await TeamCore.Validation.ValidateMembershipLimit(player.Id, state.SelectedRosterGroup.Value);
            if (!limit.Success)
            {
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(limit.ErrorMessage ?? "Membership limit reached.")
                        .AsEphemeral()
                );
                return Result.Failure(limit.ErrorMessage ?? "Membership limit reached");
            }

            // Add player
            try
            {
                await new TeamCore().AddPlayerBypassingCooldown(
                    state.SelectedTeamId.Value,
                    player.Id,
                    state.SelectedRosterGroup.Value,
                    RosterRole.Core
                );

                // Re-render
                var container = await TeamRenderer.RenderTeamEditorAsync(
                    state.DiscordUserId,
                    state.SelectedTeamId,
                    state.SelectedRosterGroup,
                    state.SelectedMemberPlayerId,
                    adminMode: true
                );

                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("✅ Player added.").AsEphemeral()
                );

                // Also update original container message if accessible
                try
                {
                    await interaction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder().EnableV2Components().AddContainerComponent(container)
                    );
                }
                catch { }

                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(ex, "Failed to add player via modal", nameof(TeamApp));
                await interaction.CreateResponseAsync(
                    DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Failed: {ex.Message}").AsEphemeral()
                );
                return Result.Failure(ex.Message);
            }
        }

        internal static TeamRolesState? TeamApp_GetStateSafe(ulong messageId)
        {
            return _rolesStateManager.GetState(messageId);
        }

        internal static bool TeamApp_TryParseDiscordId(string input, out ulong id)
        {
            input = input?.Trim() ?? string.Empty;
            if (input.StartsWith("<@") && input.EndsWith(">", StringComparison.Ordinal))
            {
                var inner = input.Trim('<', '>', '!', '@');
                return ulong.TryParse(inner, out id);
            }
            return ulong.TryParse(input, out id);
        }

        internal static async Task<Result> TeamApp_TryAddPlayerFromDiscordIdAsync(
            DiscordMessage originalContainerMessage,
            TeamRolesState state,
            ulong discordUserId
        )
        {
            try
            {
                var player = await CoreService.WithDbContext(async db =>
                    await db.Players.FirstOrDefaultAsync(p => p.MashinaUser.DiscordUserId == discordUserId)
                );
                if (player is null)
                {
                    return Result.Failure("That Discord user is not registered.");
                }
                var limit = await TeamCore.Validation.ValidateMembershipLimit(
                    player.Id,
                    state.SelectedRosterGroup!.Value
                );
                if (!limit.Success)
                {
                    return Result.Failure(limit.ErrorMessage ?? "Membership limit reached.");
                }
                await new TeamCore().AddPlayer(
                    state.SelectedTeamId!.Value,
                    player.Id,
                    state.SelectedRosterGroup!.Value,
                    RosterRole.Core
                );

                state.AwaitingAddInput = false;
                _rolesStateManager.SetState(originalContainerMessage.Id, state);

                var container = await TeamRenderer.RenderTeamEditorAsync(
                    state.DiscordUserId,
                    state.SelectedTeamId,
                    state.SelectedRosterGroup,
                    state.SelectedMemberPlayerId,
                    adminMode: true,
                    awaitingAddInput: state.AwaitingAddInput
                );
                await originalContainerMessage.ModifyAsync(
                    new DiscordMessageBuilder().EnableV2Components().AddContainerComponent(container)
                );
                return Result.CreateSuccess();
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(ex, "Failed to add player via reply", nameof(TeamApp));
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
        public bool AwaitingAddInput { get; set; }
    }
}
