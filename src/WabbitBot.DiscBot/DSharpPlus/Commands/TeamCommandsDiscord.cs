// using DSharpPlus;
// using DSharpPlus.Commands;
// using DSharpPlus.Commands.ContextChecks;
// using DSharpPlus.Entities;
// using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
// using DSharpPlus.Commands.Trees;
// using System.ComponentModel;
// using WabbitBot.Core.Common.Models;
// using WabbitBot.Core.Common.Commands;
// using WabbitBot.Core.Common.Services;
// using WabbitBot.DiscBot.DSharpPlus.Attributes;

// namespace WabbitBot.DiscBot.DSharpPlus.Commands;

// /// <summary>
// /// Discord integration for team commands - handles Discord-specific code and calls business logic
// /// </summary>
// [Command("Team")]
// [Description("Team management commands")]
// public partial class TeamCommandsDiscord
// {
//     private static readonly TeamCommands TeamCommands = new();

//     #region Discord Command Handlers

//     [Command("create")]
//     [Description("Create a new team")]
//     public async Task CreateTeamAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         string teamName)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic - the creator becomes the captain
//         var result = await TeamCommands.CreateTeamAsync(teamName, TeamSize, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to create team");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("🏆 Team Created")
//             .WithDescription(result.Message ?? "Team created successfully")
//             .WithColor(DiscordColor.Green)
//             .AddField("Team Name", teamName, true)
//             .AddField("Size", size, true)
//             .AddField("Captain (Creator)", ctx.User.Mention, true)
//             .AddField("Team ID", result.Team?.Id.ToString() ?? "Unknown", true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("info")]
//     [Description("Get information about a team")]
//     public async Task GetTeamInfoAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.GetTeamInfoAsync(teamName, TeamSize);

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to get team info");
//             return;
//         }

//         if (result.Team is null)
//         {
//             await ctx.EditResponseAsync("Team information is not available");
//             return;
//         }

//         var team = result.Team;
//         var embed = new DiscordEmbedBuilder()
//             .WithTitle($"🏆 {team.Name}")
//             .WithColor(DiscordColor.Blue)
//             .AddField("Team Size", team.TeamSize.ToString(), true)
//             .AddField("Captain", $"<@{team.TeamCaptainId}>", true)
//             .AddField("Members", team.Roster.Count.ToString(), true)
//             .AddField("Created", team.CreatedAt.ToString("g"), true)
//             .AddField("Last Active", team.LastActive.ToString("g"), true);

//         // Add roster information
//         if (team.Roster.Any())
//         {
//             var rosterText = string.Join("\n", team.Roster.Select(m =>
//                 $"• <@{m.PlayerId}> ({m.Role})"));
//             embed.AddField("Roster", rosterText);
//         }

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("invite")]
//     [Description("Invite a user to your team")]
//     [RequireTeamManager("teamName")]
//     public async Task InviteUserAsync(
//         CommandContext ctx,
//         [Description("Your team")]
//         [SlashAutoCompleteProvider(typeof(UserManagedTeamsAutoCompleteProvider))]
//         string teamName,
//         [Description("User to invite")]
//         DiscordUser user)
//     {
//         await ctx.DeferResponseAsync();

//         // Call business logic
//         var result = await TeamCommands.InviteUserAsync(teamName, ctx.User.Id.ToString(), user.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to invite user");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("📨 Team Invitation")
//             .WithDescription($"You have been invited to join team '{teamName}'")
//             .WithColor(DiscordColor.Gold)
//             .AddField("Invited by", ctx.User.Mention, true)
//             .AddField("Team", teamName, true);

//         // Send DM to the invited user
//         try
//         {
//             await user.SendMessageAsync(new DiscordEmbedBuilder()
//                 .WithTitle("📨 Team Invitation")
//                 .WithDescription($"You have been invited to join team '{teamName}'")
//                 .WithColor(DiscordColor.Gold)
//                 .AddField("Invited by", ctx.User.Mention, true)
//                 .AddField("Team", teamName, true)
//                 .WithFooter("Use /team accept [team name] to accept this invitation"));
//         }
//         catch
//         {
//             // User has DMs disabled
//         }

//         await ctx.EditResponseAsync(result.Message ?? "Invitation sent successfully");
//     }

//     [Command("kick")]
//     [Description("Kick a user from your team")]
//     [RequireTeamManager("teamName")]
//     public async Task KickUserAsync(
//         CommandContext ctx,
//         [Description("Your team")]
//         [SlashAutoCompleteProvider(typeof(UserManagedTeamsAutoCompleteProvider))]
//         string teamName,
//         [Description("User to kick")]
//         DiscordUser user)
//     {
//         await ctx.DeferResponseAsync();

//         // Call business logic
//         var result = await TeamCommands.KickUserAsync(teamName, ctx.User.Id.ToString(), user.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to kick user");
//             return;
//         }

//         await ctx.EditResponseAsync(result.Message ?? "User kicked successfully");
//     }

//     [Command("position")]
//     [Description("Change a team member's position")]
//     [RequireTeamManager("teamName")]
//     public async Task ChangePositionAsync(
//         CommandContext ctx,
//         [Description("Your team")]
//         [SlashAutoCompleteProvider(typeof(UserManagedTeamsAutoCompleteProvider))]
//         string teamName,
//         [Description("Team member")]
//         DiscordUser user,
//         [Description("New position (Core, Backup)")]
//         [SlashChoiceProvider(typeof(TeamRoleChoiceProvider))]
//         string position)
//     {
//         await ctx.DeferResponseAsync();

//         if (!Team.Validation.TryParseTeamRole(position, out var teamRole))
//         {
//             await ctx.EditResponseAsync($"Invalid position: {position}. Valid positions: Core, Backup");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.ChangePositionAsync(teamName, ctx.User.Id.ToString(), user.Id.ToString(), teamRole);

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to change position");
//             return;
//         }

//         await ctx.EditResponseAsync(result.Message ?? "Position changed successfully");
//     }

//     [Command("captain")]
//     [Description("Promote a team member to captain (captain only)")]
//     public async Task ChangeCaptainAsync(
//         CommandContext ctx,
//         [Description("Your team")]
//         [SlashAutoCompleteProvider(typeof(UserCaptainTeamsAutoCompleteProvider))]
//         string teamName,
//         [Description("New captain")]
//         [SlashAutoCompleteProvider(typeof(TeamMemberAutoCompleteProvider))]
//         string newCaptainId)
//     {
//         await ctx.DeferResponseAsync();

//         // Call business logic
//         var result = await TeamCommands.ChangeCaptainAsync(teamName, ctx.User.Id.ToString(), newCaptainId);

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to change captain");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("👑 Captain Changed")
//             .WithDescription(result.Message ?? "Captain changed successfully")
//             .WithColor(DiscordColor.Gold)
//             .AddField("New Captain", $"<@{newCaptainId}>", true)
//             .AddField("Previous Captain", ctx.User.Mention, true)
//             .AddField("Team", teamName, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("leave")]
//     [Description("Leave a team")]
//     public async Task LeaveTeamAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.LeaveTeamAsync(teamName, TeamSize, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to leave team");
//             return;
//         }

//         await ctx.EditResponseAsync(result.Message ?? "Successfully left team");
//     }

//     [Command("rename")]
//     [Description("Rename your team (captain only)")]
//     public async Task RenameTeamAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Current team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string oldTeamName,
//         [Description("New team name")]
//         string newTeamName)
//     {
//         await ctx.DeferResponseAsync();

//         // TODO: Validate user is captain of the team

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.RenameTeamAsync(oldTeamName, TeamSize, newTeamName, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to rename team");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("✏️ Team Renamed")
//             .WithDescription(result.Message ?? "Team renamed successfully")
//             .WithColor(DiscordColor.Green)
//             .AddField("Old Name", oldTeamName, true)
//             .AddField("New Name", newTeamName, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("myteams")]
//     [Description("List all teams you are a member of")]
//     public async Task GetUserTeamsAsync(CommandContext ctx)
//     {
//         await ctx.DeferResponseAsync();

//         // Call business logic
//         var result = await TeamCommands.GetUserTeamsAsync(ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to get user teams");
//             return;
//         }

//         if (!result.Teams.Any())
//         {
//             await ctx.EditResponseAsync(result.Message ?? "You are not a member of any teams");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("🏆 Your Teams")
//             .WithDescription(result.Message ?? $"Found {result.Teams.Count} team(s)")
//             .WithColor(DiscordColor.Blue);

//         foreach (var team in result.Teams)
//         {
//             embed.AddField(team.Name,
//                 $"Size: {team.TeamSize} | Captain: <@{team.TeamCaptainId}> | Members: {team.Roster.Count}",
//                 true);
//         }

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("disband")]
//     [Description("Disband your team (captain only)")]
//     public async Task DisbandTeamAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName)
//     {
//         await ctx.DeferResponseAsync();

//         // TODO: Validate user is captain of the team

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.DisbandTeamAsync(teamName, TeamSize, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to disband team");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("💥 Team Disbanded")
//             .WithDescription(result.Message ?? "Team disbanded successfully")
//             .WithColor(DiscordColor.Red)
//             .AddField("Team", teamName, true)
//             .AddField("Disbanded by", ctx.User.Mention, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("tag")]
//     [Description("Set your team's tag (captain only)")]
//     public async Task SetTeamTagAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName,
//         [Description("Team tag")]
//         string tag)
//     {
//         await ctx.DeferResponseAsync();

//         // TODO: Validate user is captain of the team

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.SetTeamTagAsync(teamName, TeamSize, ctx.User.Id.ToString(), tag);

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to set team tag");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("🏷️ Team Tag Updated")
//             .WithDescription(result.Message ?? "Team tag updated successfully")
//             .WithColor(DiscordColor.Green)
//             .AddField("Team", teamName, true)
//             .AddField("New Tag", tag, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("manager")]
//     [Description("Manage team manager permissions")]
//     [RequireTeamManager("teamName")]
//     public async Task SetTeamManagerAsync(
//         CommandContext ctx,
//         [Description("Your team")]
//         [SlashAutoCompleteProvider(typeof(UserManagedTeamsAutoCompleteProvider))]
//         string teamName,
//         [Description("Team member to manage")]
//         [SlashAutoCompleteProvider(typeof(TeamMemberAutoCompleteProvider))]
//         string memberId,
//         [Description("Grant manager permissions")]
//         bool isManager)
//     {
//         await ctx.DeferResponseAsync();

//         // Call business logic
//         var result = await TeamCommands.SetTeamManagerAsync(teamName, TeamSize.TwoVTwo, ctx.User.Id.ToString(), memberId, isManager);

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to set team manager status");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("👥 Team Manager Updated")
//             .WithDescription(result.Message ?? "Team manager status updated successfully")
//             .WithColor(isManager ? DiscordColor.Green : DiscordColor.Orange)
//             .AddField("Team", teamName, true)
//             .AddField("User", $"<@{memberId}>", true)
//             .AddField("Status", isManager ? "Manager" : "Member", true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("managers")]
//     [Description("List team managers")]
//     public async Task ListTeamManagersAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.ListTeamManagersAsync(teamName, TeamSize);

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to list team managers");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("👥 Team Managers")
//             .WithDescription(result.Message ?? "Team managers listed successfully")
//             .WithColor(DiscordColor.Blue)
//             .AddField("Team", teamName, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }



//     #endregion
// }

// /// <summary>
// /// Discord integration for team admin commands - handles Discord-specific code and calls business logic
// /// </summary>
// [Command("team_admin")]
// [Description("Team administration commands")]
// [RequirePermissions(DiscordPermission.Administrator)]
// public partial class TeamAdminCommandsDiscord
// {
//     private static readonly TeamCommands TeamCommands = new();

//     #region Discord Admin Command Handlers

//     [Command("archive")]
//     [Description("Archive a team (admin only)")]
//     public async Task ArchiveTeamAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.ArchiveTeamAsync(teamName, TeamSize, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to archive team");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("📦 Team Archived")
//             .WithDescription(result.Message ?? "Team archived successfully")
//             .WithColor(DiscordColor.Orange)
//             .AddField("Team", teamName, true)
//             .AddField("Archived by", ctx.User.Mention, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("unarchive")]
//     [Description("Unarchive a team (admin only)")]
//     public async Task UnarchiveTeamAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.UnarchiveTeamAsync(teamName, TeamSize, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to unarchive team");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("📦 Team Unarchived")
//             .WithDescription(result.Message ?? "Team unarchived successfully")
//             .WithColor(DiscordColor.Green)
//             .AddField("Team", teamName, true)
//             .AddField("Unarchived by", ctx.User.Mention, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("create")]
//     [Description("Create a team (admin only)")]
//     public async Task AdminCreateTeamAsync(
//         CommandContext ctx,
//         [Description("Team name")]
//         string teamName,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Captain username")]
//         string captainUsername)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.AdminCreateTeamAsync(teamName, TeamSize, captainUsername, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to create team");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("🏆 Team Created (Admin)")
//             .WithDescription(result.Message ?? "Team created successfully")
//             .WithColor(DiscordColor.Green)
//             .AddField("Team Name", teamName, true)
//             .AddField("Size", size, true)
//             .AddField("Captain", captainUsername, true)
//             .AddField("Created by", ctx.User.Mention, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("delete")]
//     [Description("Delete a team (admin only)")]
//     public async Task AdminDeleteTeamAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.AdminDeleteTeamAsync(teamName, TeamSize, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to delete team");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("🗑️ Team Deleted (Admin)")
//             .WithDescription(result.Message ?? "Team deleted successfully")
//             .WithColor(DiscordColor.Red)
//             .AddField("Team Name", teamName, true)
//             .AddField("Size", size, true)
//             .AddField("Deleted by", ctx.User.Mention, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("add_player")]
//     [Description("Add a player to a team (admin only)")]
//     public async Task AdminAddPlayerAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName,
//         [Description("Player username")]
//         string username,
//         [Description("Player role (Core, Substitute)")]
//         [SlashChoiceProvider(typeof(TeamRoleChoiceProvider))]
//         string role)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Parse team role
//         if (!Team.Validation.TryParseTeamRole(role, out var teamRole))
//         {
//             await ctx.EditResponseAsync($"Invalid role: {role}. Valid roles: Core, Substitute");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.AdminAddPlayerAsync(teamName, TeamSize, username, teamRole, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to add player");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("➕ Player Added (Admin)")
//             .WithDescription(result.Message ?? "Player added successfully")
//             .WithColor(DiscordColor.Green)
//             .AddField("Team", teamName, true)
//             .AddField("Player", username, true)
//             .AddField("Role", role, true)
//             .AddField("Added by", ctx.User.Mention, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("remove_player")]
//     [Description("Remove a player from a team (admin only)")]
//     public async Task AdminRemovePlayerAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName,
//         [Description("Player username")]
//         string username)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.AdminRemovePlayerAsync(teamName, TeamSize, username, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to remove player");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("➖ Player Removed (Admin)")
//             .WithDescription(result.Message ?? "Player removed successfully")
//             .WithColor(DiscordColor.Orange)
//             .AddField("Team", teamName, true)
//             .AddField("Player", username, true)
//             .AddField("Removed by", ctx.User.Mention, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("reset_rating")]
//     [Description("Reset a team's rating (admin only)")]
//     public async Task AdminResetRatingAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.AdminResetRatingAsync(teamName, TeamSize, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to reset team rating");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("🔄 Rating Reset (Admin)")
//             .WithDescription(result.Message ?? "Team rating reset successfully")
//             .WithColor(DiscordColor.Yellow)
//             .AddField("Team", teamName, true)
//             .AddField("Size", size, true)
//             .AddField("Reset by", ctx.User.Mention, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     [Command("change_role")]
//     [Description("Change a player's role in a team (admin only)")]
//     public async Task AdminChangeRoleAsync(
//         CommandContext ctx,
//         [Description("Team size")]
//         [SlashChoiceProvider(typeof(TeamTeamSizeChoiceProvider))]
//         string size,
//         [Description("Team name")]
//         [SlashAutoCompleteProvider(typeof(DynamicTeamAutoCompleteProvider))]
//         string teamName,
//         [Description("Player username")]
//         string username,
//         [Description("New role (Core, Substitute)")]
//         [SlashChoiceProvider(typeof(TeamRoleChoiceProvider))]
//         string newRole)
//     {
//         await ctx.DeferResponseAsync();

//         // Parse game size
//         if (!Game.Validation.TryParseTeamSize(size, out var TeamSize))
//         {
//             await ctx.EditResponseAsync($"Invalid team size: {size}");
//             return;
//         }

//         // Parse team role
//         if (!Team.Validation.TryParseTeamRole(newRole, out var teamRole))
//         {
//             await ctx.EditResponseAsync($"Invalid role: {newRole}. Valid roles: Core, Substitute");
//             return;
//         }

//         // Call business logic
//         var result = await TeamCommands.AdminChangeRoleAsync(teamName, TeamSize, username, teamRole, ctx.User.Id.ToString());

//         if (!result.Success)
//         {
//             await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to change player role");
//             return;
//         }

//         var embed = new DiscordEmbedBuilder()
//             .WithTitle("🔄 Role Changed (Admin)")
//             .WithDescription(result.Message ?? "Player role changed successfully")
//             .WithColor(DiscordColor.Blue)
//             .AddField("Team", teamName, true)
//             .AddField("Player", username, true)
//             .AddField("New Role", newRole, true)
//             .AddField("Changed by", ctx.User.Mention, true);

//         await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
//     }

//     #endregion
// }