using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;
using WabbitBot.DiscBot.DiscBot.Commands;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.DiscBot.DSharpPlus.Commands;

/// <summary>
/// Discord integration for team commands - handles Discord-specific code and calls business logic
/// </summary>
[Command("Team")]
[Description("Team management commands")]
public partial class TeamCommandsDiscord
{
    private static readonly TeamCommands TeamCommands = new();

    #region Discord Command Handlers

    [Command("create")]
    [Description("Create a new team")]
    public async Task CreateTeamAsync(
        CommandContext ctx,
        [Description("Team size (1v1, 2v2, 3v3, 4v4)")]
        [ChoiceProvider(typeof(GameSizeChoiceProvider))]
        string size,
        [Description("Team name")]
        string teamName)
    {
        await ctx.DeferResponseAsync();

        // Parse game size
        if (!TryParseGameSize(size, out var gameSize))
        {
            await ctx.EditResponseAsync($"Invalid team size: {size}. Valid sizes: 1v1, 2v2, 3v3, 4v4");
            return;
        }

        // Call business logic - the creator becomes the captain
        var result = await TeamCommands.CreateTeamAsync(teamName, gameSize, ctx.User.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to create team");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("🏆 Team Created")
            .WithDescription(result.Message ?? "Team created successfully")
            .WithColor(DiscordColor.Green)
            .AddField("Team Name", teamName, true)
            .AddField("Size", size, true)
            .AddField("Captain (Creator)", ctx.User.Mention, true)
            .AddField("Team ID", result.Team?.Id.ToString() ?? "Unknown", true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("info")]
    [Description("Get information about a team")]
    public async Task GetTeamInfoAsync(
        CommandContext ctx,
        [Description("Team name")]
        string teamName)
    {
        await ctx.DeferResponseAsync();

        // Call business logic
        var result = await TeamCommands.GetTeamInfoAsync(teamName);

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to get team info");
            return;
        }

        var team = result.Team!;
        var embed = new DiscordEmbedBuilder()
            .WithTitle($"🏆 {team.Name}")
            .WithColor(DiscordColor.Blue)
            .AddField("Team Size", team.TeamSize.ToString(), true)
            .AddField("Captain", $"<@{team.TeamCaptainId}>", true)
            .AddField("Members", team.Roster.Count.ToString(), true)
            .AddField("Created", team.CreatedAt.ToString("g"), true)
            .AddField("Last Active", team.LastActive.ToString("g"), true);

        // Add roster information
        if (team.Roster.Any())
        {
            var rosterText = string.Join("\n", team.Roster.Select(m =>
                $"• <@{m.PlayerId}> ({m.Role})"));
            embed.AddField("Roster", rosterText);
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("invite")]
    [Description("Invite a user to your team")]
    public async Task InviteUserAsync(
        CommandContext ctx,
        [Description("User to invite")]
        DiscordUser user)
    {
        await ctx.DeferResponseAsync();

        // TODO: Get user's team from context
        var teamName = "placeholder_team_name";

        // Call business logic
        var result = await TeamCommands.InviteUserAsync(teamName, ctx.User.Id.ToString(), user.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to invite user");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("📨 Team Invitation")
            .WithDescription($"You have been invited to join team '{teamName}'")
            .WithColor(DiscordColor.Gold)
            .AddField("Invited by", ctx.User.Mention, true)
            .AddField("Team", teamName, true);

        // Send DM to the invited user
        try
        {
            await user.SendMessageAsync(new DiscordEmbedBuilder()
                .WithTitle("📨 Team Invitation")
                .WithDescription($"You have been invited to join team '{teamName}'")
                .WithColor(DiscordColor.Gold)
                .AddField("Invited by", ctx.User.Mention, true)
                .AddField("Team", teamName, true)
                .WithFooter("Use /team accept [team name] to accept this invitation"));
        }
        catch
        {
            // User has DMs disabled
        }

        await ctx.EditResponseAsync(result.Message ?? "Invitation sent successfully");
    }

    [Command("kick")]
    [Description("Kick a user from your team (captain only)")]
    public async Task KickUserAsync(
        CommandContext ctx,
        [Description("User to kick")]
        DiscordUser user)
    {
        await ctx.DeferResponseAsync();

        // TODO: Validate user is captain of the team
        var teamName = "placeholder_team_name";

        // Call business logic
        var result = await TeamCommands.KickUserAsync(teamName, ctx.User.Id.ToString(), user.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to kick user");
            return;
        }

        await ctx.EditResponseAsync(result.Message ?? "User kicked successfully");
    }

    [Command("position")]
    [Description("Change a team member's position (captain only)")]
    public async Task ChangePositionAsync(
        CommandContext ctx,
        [Description("Team member")]
        DiscordUser user,
        [Description("New position (Core, Backup)")]
        [ChoiceProvider(typeof(TeamRoleChoiceProvider))]
        string position)
    {
        await ctx.DeferResponseAsync();

        if (!TryParseTeamRole(position, out var teamRole))
        {
            await ctx.EditResponseAsync($"Invalid position: {position}. Valid positions: Core, Backup");
            return;
        }

        // TODO: Validate user is captain of the team
        var teamName = "placeholder_team_name";

        // Call business logic
        var result = await TeamCommands.ChangePositionAsync(teamName, ctx.User.Id.ToString(), user.Id.ToString(), teamRole);

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to change position");
            return;
        }

        await ctx.EditResponseAsync(result.Message ?? "Position changed successfully");
    }

    [Command("captain")]
    [Description("Promote a team member to captain (captain only)")]
    public async Task ChangeCaptainAsync(
        CommandContext ctx,
        [Description("New captain")]
        DiscordUser user)
    {
        await ctx.DeferResponseAsync();

        // TODO: Validate user is captain of the team
        var teamName = "placeholder_team_name";

        // Call business logic
        var result = await TeamCommands.ChangeCaptainAsync(teamName, ctx.User.Id.ToString(), user.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to change captain");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("👑 Captain Changed")
            .WithDescription(result.Message ?? "Captain changed successfully")
            .WithColor(DiscordColor.Gold)
            .AddField("New Captain", user.Mention, true)
            .AddField("Previous Captain", ctx.User.Mention, true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("leave")]
    [Description("Leave a team")]
    public async Task LeaveTeamAsync(
        CommandContext ctx,
        [Description("Team name")]
        string teamName)
    {
        await ctx.DeferResponseAsync();

        // Call business logic
        var result = await TeamCommands.LeaveTeamAsync(teamName, ctx.User.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to leave team");
            return;
        }

        await ctx.EditResponseAsync(result.Message ?? "Successfully left team");
    }

    [Command("rename")]
    [Description("Rename your team (captain only)")]
    public async Task RenameTeamAsync(
        CommandContext ctx,
        [Description("Current team name")]
        string oldTeamName,
        [Description("New team name")]
        string newTeamName)
    {
        await ctx.DeferResponseAsync();

        // TODO: Validate user is captain of the team

        // Call business logic
        var result = await TeamCommands.RenameTeamAsync(oldTeamName, newTeamName, ctx.User.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to rename team");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("✏️ Team Renamed")
            .WithDescription(result.Message ?? "Team renamed successfully")
            .WithColor(DiscordColor.Green)
            .AddField("Old Name", oldTeamName, true)
            .AddField("New Name", newTeamName, true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("myteams")]
    [Description("List all teams you are a member of")]
    public async Task GetUserTeamsAsync(CommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        // Call business logic
        var result = await TeamCommands.GetUserTeamsAsync(ctx.User.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to get user teams");
            return;
        }

        if (!result.Teams.Any())
        {
            await ctx.EditResponseAsync(result.Message ?? "You are not a member of any teams");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("🏆 Your Teams")
            .WithDescription(result.Message ?? $"Found {result.Teams.Count} team(s)")
            .WithColor(DiscordColor.Blue);

        foreach (var team in result.Teams)
        {
            embed.AddField(team.Name,
                $"Size: {team.TeamSize} | Captain: <@{team.TeamCaptainId}> | Members: {team.Roster.Count}",
                true);
        }

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("disband")]
    [Description("Disband your team (captain only)")]
    public async Task DisbandTeamAsync(
        CommandContext ctx,
        [Description("Team name")]
        string teamName)
    {
        await ctx.DeferResponseAsync();

        // TODO: Validate user is captain of the team

        // Call business logic
        var result = await TeamCommands.DisbandTeamAsync(teamName, ctx.User.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to disband team");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("💥 Team Disbanded")
            .WithDescription(result.Message ?? "Team disbanded successfully")
            .WithColor(DiscordColor.Red)
            .AddField("Team", teamName, true)
            .AddField("Disbanded by", ctx.User.Mention, true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("tag")]
    [Description("Set your team's tag (captain only)")]
    public async Task SetTeamTagAsync(
        CommandContext ctx,
        [Description("Team name")]
        string teamName,
        [Description("Team tag")]
        string tag)
    {
        await ctx.DeferResponseAsync();

        // TODO: Validate user is captain of the team

        // Call business logic
        var result = await TeamCommands.SetTeamTagAsync(teamName, ctx.User.Id.ToString(), tag);

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to set team tag");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("🏷️ Team Tag Updated")
            .WithDescription(result.Message ?? "Team tag updated successfully")
            .WithColor(DiscordColor.Green)
            .AddField("Team", teamName, true)
            .AddField("New Tag", tag, true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }



    #endregion

    #region Private Helper Methods

    private static bool TryParseGameSize(string size, out GameSize gameSize)
    {
        gameSize = size.ToLowerInvariant() switch
        {
            "1v1" => GameSize.OneVOne,
            "2v2" => GameSize.TwoVTwo,
            "3v3" => GameSize.ThreeVThree,
            "4v4" => GameSize.FourVFour,
            _ => GameSize.OneVOne
        };

        return size.ToLowerInvariant() is "1v1" or "2v2" or "3v3" or "4v4";
    }

    private static bool TryParseTeamRole(string role, out TeamRole teamRole)
    {
        teamRole = role.ToLowerInvariant() switch
        {
            "core" => TeamRole.Core,
            "backup" => TeamRole.Substitute,
            _ => TeamRole.Core
        };

        return role.ToLowerInvariant() is "core" or "backup";
    }

    #endregion
}

/// <summary>
/// Choice provider for team roles
/// </summary>
public class TeamRoleChoiceProvider : IChoiceProvider
{
    public IEnumerable<CommandChoice> GetChoices()
    {
        return new[]
        {
            new CommandChoice("Core", "Core"),
            new CommandChoice("Backup", "Backup"),
        };
    }
}

/// <summary>
/// Discord integration for team admin commands - handles Discord-specific code and calls business logic
/// </summary>
[Command("team_admin")]
[Description("Team administration commands")]
[RequirePermissions(DiscordPermission.Administrator)]
public partial class TeamAdminCommandsDiscord
{
    private static readonly TeamCommands TeamCommands = new();

    #region Discord Admin Command Handlers

    [Command("archive")]
    [Description("Archive a team (admin only)")]
    public async Task ArchiveTeamAsync(
        CommandContext ctx,
        [Description("Team name")]
        string teamName)
    {
        await ctx.DeferResponseAsync();

        // Call business logic
        var result = await TeamCommands.ArchiveTeamAsync(teamName, ctx.User.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to archive team");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("📦 Team Archived")
            .WithDescription(result.Message ?? "Team archived successfully")
            .WithColor(DiscordColor.Orange)
            .AddField("Team", teamName, true)
            .AddField("Archived by", ctx.User.Mention, true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    [Command("unarchive")]
    [Description("Unarchive a team (admin only)")]
    public async Task UnarchiveTeamAsync(
        CommandContext ctx,
        [Description("Team name")]
        string teamName)
    {
        await ctx.DeferResponseAsync();

        // Call business logic
        var result = await TeamCommands.UnarchiveTeamAsync(teamName, ctx.User.Id.ToString());

        if (!result.Success)
        {
            await ctx.EditResponseAsync(result.ErrorMessage ?? "Failed to unarchive team");
            return;
        }

        var embed = new DiscordEmbedBuilder()
            .WithTitle("📦 Team Unarchived")
            .WithDescription(result.Message ?? "Team unarchived successfully")
            .WithColor(DiscordColor.Green)
            .AddField("Team", teamName, true)
            .AddField("Unarchived by", ctx.User.Mention, true);

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
    }

    #endregion
}