using DSharpPlus.Entities;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Handlers;
using WabbitBot.Core.Common.Services;
using System.Linq;

namespace WabbitBot.DiscBot.DSharpPlus.Commands;

/// <summary>
/// Choice provider for game sizes - automatically generated from EvenTeamFormat enum
/// </summary>
public class EvenTeamFormatChoiceProvider : IChoiceProvider
{
    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        return await Task.FromResult(Enum.GetValues<EvenTeamFormat>()
            .Select(size => new DiscordApplicationCommandOptionChoice(
                GetDisplayName(size),
                GetDisplayName(size)
            )));
    }

    private static string GetDisplayName(EvenTeamFormat evenTeamFormat)
    {
        return evenTeamFormat switch
        {
            EvenTeamFormat.OneVOne => "1v1",
            EvenTeamFormat.TwoVTwo => "2v2",
            EvenTeamFormat.ThreeVThree => "3v3",
            EvenTeamFormat.FourVFour => "4v4",
            _ => evenTeamFormat.ToString()
        };
    }
}

/// <summary>
/// Choice provider for team game sizes - excludes 1v1 since teams don't make sense for 1v1
/// </summary>
public class TeamEvenTeamFormatChoiceProvider : IChoiceProvider
{
    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        return await Task.FromResult(Enum.GetValues<EvenTeamFormat>()
            .Where(size => size != EvenTeamFormat.OneVOne) // Exclude 1v1 for team commands
            .Select(size => new DiscordApplicationCommandOptionChoice(
                GetDisplayName(size),
                GetDisplayName(size)
            )));
    }

    private static string GetDisplayName(EvenTeamFormat evenTeamFormat)
    {
        return evenTeamFormat switch
        {
            EvenTeamFormat.TwoVTwo => "2v2",
            EvenTeamFormat.ThreeVThree => "3v3",
            EvenTeamFormat.FourVFour => "4v4",
            _ => evenTeamFormat.ToString()
        };
    }
}

/// <summary>
/// Choice provider for team roles
/// </summary>
public class TeamRoleChoiceProvider : IChoiceProvider
{
    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        return await Task.FromResult(new[]
        {
            new DiscordApplicationCommandOptionChoice("Core", "Core"),
            new DiscordApplicationCommandOptionChoice("Backup", "Backup"),
        });
    }
}



/// <summary>
/// Choice provider for map sizes
/// </summary>
public class MapSizeChoiceProvider : IChoiceProvider
{
    private static readonly MapService MapService = new MapService();

    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        var sizes = MapService.GetAvailableSizes().Append("All");
        return await Task.FromResult(sizes
            .Select(s => new DiscordApplicationCommandOptionChoice(s, s)));
    }
}

/// <summary>
/// Auto-complete provider for map names
/// </summary>
public class MapNameAutoCompleteProvider : IAutoCompleteProvider
{
    private static readonly MapService MapService = new MapService();

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var userInput = ctx.UserInput ?? "";
        return await Task.FromResult(MapService.GetMaps()
            .Where(m => m.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(m => new DiscordAutoCompleteChoice(m.Name, m.Name)));
    }
}

/// <summary>
/// Dynamic auto-complete provider for team names that filters by game size
/// </summary>
public class DynamicTeamAutoCompleteProvider : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var userInput = ctx.UserInput ?? "";
        var teamService = new TeamService();

        // Find the game size parameter from the arguments
        var evenTeamFormatParameter = ctx.Arguments.Keys.FirstOrDefault(p => p.Name == "size");
        if (evenTeamFormatParameter != null &&
            ctx.Arguments.TryGetValue(evenTeamFormatParameter, out var evenTeamFormatObj) &&
            evenTeamFormatObj != null)
        {
            var evenTeamFormatString = evenTeamFormatObj?.ToString();
            if (!string.IsNullOrEmpty(evenTeamFormatString) && Game.Validation.TryParseEvenTeamFormat(evenTeamFormatString, out var evenTeamFormat))
            {
                // Filter teams by the selected game size, excluding 1v1 teams
                var teams = await teamService.SearchTeamsByEvenTeamFormatAsync(
                    userInput, evenTeamFormat, 25);

                return teams.Where(team => team.TeamSize != EvenTeamFormat.OneVOne)
                    .Select(team => new DiscordAutoCompleteChoice(
                        GetDisplayName(team),
                        team.Name
                    ));
            }
        }

        // Fallback to all teams if no game size found, excluding 1v1 teams
        var allTeams = await teamService.SearchTeamsAsync(userInput, 25);
        return allTeams.Where(team => team.TeamSize != EvenTeamFormat.OneVOne)
            .Select(team => new DiscordAutoCompleteChoice(
                GetDisplayName(team),
                team.Name
            ));
    }

    private static string GetDisplayName(Team team)
    {
        var baseName = !string.IsNullOrEmpty(team.Tag) ? $"{team.Name} [{team.Tag}]" : team.Name;
        var evenTeamFormatDisplay = Game.Helpers.GetEvenTeamFormatDisplay(team.TeamSize);
        return $"{baseName} ({evenTeamFormatDisplay})";
    }
}

/// <summary>
/// Choice provider for team members
/// </summary>
public class TeamMemberChoiceProvider : IChoiceProvider
{
    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        // This is a placeholder - in a real implementation, this would need to be dynamic
        // based on the selected team, but choice providers don't have access to context
        // For now, we'll return a generic message
        return await Task.FromResult(new[]
        {
            new DiscordApplicationCommandOptionChoice("Select a team member", "placeholder")
        });
    }
}

/// <summary>
/// Auto-complete provider for team members of a specific team
/// </summary>
public class TeamMemberAutoCompleteProvider : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var userInput = ctx.UserInput ?? "";

        // Find the team parameter from the arguments
        var teamParameter = ctx.Arguments.Keys.FirstOrDefault(p => p.Name == "teamName");
        if (teamParameter != null &&
            ctx.Arguments.TryGetValue(teamParameter, out var teamNameObj) &&
            teamNameObj != null)
        {
            var teamName = teamNameObj.ToString();
            if (!string.IsNullOrEmpty(teamName))
            {
                try
                {
                    // Get the team
                    var teamService = new TeamService();
                    var team = await teamService.GetByNameAsync(teamName);
                    if (team != null)
                    {
                        // Get active team members and filter by user input
                        var members = team.GetActiveMembers()
                            .Where(member =>
                                member.PlayerId.Contains(userInput, StringComparison.OrdinalIgnoreCase))
                            .Take(25);

                        return members.Select(member => new DiscordAutoCompleteChoice(
                            $"<@{member.PlayerId}> ({member.Role})",
                            member.PlayerId
                        ));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting team members: {ex.Message}");
                }
            }
        }

        return Enumerable.Empty<DiscordAutoCompleteChoice>();
    }
}

/// <summary>
/// Auto-complete provider for teams where the user is a captain
/// </summary>
public class UserCaptainTeamsAutoCompleteProvider : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var userInput = ctx.UserInput ?? "";
        var userId = ctx.User.Id.ToString();

        try
        {
            // Get all teams the user is a member of
            var teamService = new TeamService();
            var userTeams = await teamService.GetUserTeamsAsync(userId);

            // Filter to teams where user is captain, and exclude 1v1 teams
            var captainTeams = userTeams.Where(team =>
                team.TeamSize != EvenTeamFormat.OneVOne &&
                team.IsCaptain(userId));

            // Filter by user input
            var filteredTeams = captainTeams
                .Where(team => team.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase) ||
                              (team.Tag != null && team.Tag.Contains(userInput, StringComparison.OrdinalIgnoreCase)))
                .Take(25);

            return filteredTeams.Select(team => new DiscordAutoCompleteChoice(
                GetDisplayName(team),
                team.Name
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user captain teams: {ex.Message}");
            return Enumerable.Empty<DiscordAutoCompleteChoice>();
        }
    }

    private static string GetDisplayName(Team team)
    {
        var baseName = !string.IsNullOrEmpty(team.Tag) ? $"{team.Name} [{team.Tag}]" : team.Name;
        var evenTeamFormatDisplay = Game.Helpers.GetEvenTeamFormatDisplay(team.TeamSize);
        return $"{baseName} ({evenTeamFormatDisplay})";
    }
}

/// <summary>
/// Auto-complete provider for teams where the user is a manager (captain or core player)
/// </summary>
public class UserManagedTeamsAutoCompleteProvider : IAutoCompleteProvider
{
    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var userInput = ctx.UserInput ?? "";
        var userId = ctx.User.Id.ToString();

        try
        {
            // Get all teams the user is a member of
            var teamService = new TeamService();
            var userTeams = await teamService.GetUserTeamsAsync(userId);

            // Filter to teams where user is a team manager, and exclude 1v1 teams
            var managedTeams = userTeams.Where(team =>
                team.TeamSize != EvenTeamFormat.OneVOne &&
                team.IsTeamManager(userId));

            // Filter by user input
            var filteredTeams = managedTeams
                .Where(team => team.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase) ||
                              (team.Tag != null && team.Tag.Contains(userInput, StringComparison.OrdinalIgnoreCase)))
                .Take(25);

            return filteredTeams.Select(team => new DiscordAutoCompleteChoice(
                GetDisplayName(team),
                team.Name
            ));
        }
        catch (Exception ex)
        {
            // Log error and return empty list
            Console.WriteLine($"Error getting user managed teams: {ex.Message}");
            return Enumerable.Empty<DiscordAutoCompleteChoice>();
        }
    }

    private static string GetDisplayName(Team team)
    {
        var baseName = !string.IsNullOrEmpty(team.Tag) ? $"{team.Name} [{team.Tag}]" : team.Name;
        var evenTeamFormatDisplay = Game.Helpers.GetEvenTeamFormatDisplay(team.TeamSize);
        return $"{baseName} ({evenTeamFormatDisplay})";
    }
}