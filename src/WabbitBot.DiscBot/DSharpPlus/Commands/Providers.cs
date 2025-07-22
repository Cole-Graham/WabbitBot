using DSharpPlus.Entities;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Handlers;
using System.Linq;
using WabbitBot.DiscBot.DSharpPlus;

namespace WabbitBot.DiscBot.DSharpPlus.Commands;

/// <summary>
/// Choice provider for game sizes - automatically generated from GameSize enum
/// </summary>
public class GameSizeChoiceProvider : IChoiceProvider
{
    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        return await Task.FromResult(Enum.GetValues<GameSize>()
            .Select(size => new DiscordApplicationCommandOptionChoice(
                GetDisplayName(size),
                GetDisplayName(size)
            )));
    }

    private static string GetDisplayName(GameSize gameSize)
    {
        return gameSize switch
        {
            GameSize.OneVOne => "1v1",
            GameSize.TwoVTwo => "2v2",
            GameSize.ThreeVThree => "3v3",
            GameSize.FourVFour => "4v4",
            _ => gameSize.ToString()
        };
    }
}

/// <summary>
/// Choice provider for team game sizes - excludes 1v1 since teams don't make sense for 1v1
/// </summary>
public class TeamGameSizeChoiceProvider : IChoiceProvider
{
    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        return await Task.FromResult(Enum.GetValues<GameSize>()
            .Where(size => size != GameSize.OneVOne) // Exclude 1v1 for team commands
            .Select(size => new DiscordApplicationCommandOptionChoice(
                GetDisplayName(size),
                GetDisplayName(size)
            )));
    }

    private static string GetDisplayName(GameSize gameSize)
    {
        return gameSize switch
        {
            GameSize.TwoVTwo => "2v2",
            GameSize.ThreeVThree => "3v3",
            GameSize.FourVFour => "4v4",
            _ => gameSize.ToString()
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
    private static readonly MapHandler MapHandler = MapHandler.Instance;

    public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter)
    {
        var sizes = MapHandler.GetAvailableSizes().Append("All");
        return await Task.FromResult(sizes
            .Select(s => new DiscordApplicationCommandOptionChoice(s, s)));
    }
}

/// <summary>
/// Auto-complete provider for map names
/// </summary>
public class MapNameAutoCompleteProvider : IAutoCompleteProvider
{
    private static readonly MapHandler MapHandler = MapHandler.Instance;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var userInput = ctx.UserInput ?? "";
        return await Task.FromResult(MapHandler.GetMaps()
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

        // Find the game size parameter from the arguments
        var gameSizeParameter = ctx.Arguments.Keys.FirstOrDefault(p => p.Name == "size");
        if (gameSizeParameter != null &&
            ctx.Arguments.TryGetValue(gameSizeParameter, out var gameSizeObj) &&
            gameSizeObj != null)
        {
            var gameSizeString = gameSizeObj?.ToString();
            if (!string.IsNullOrEmpty(gameSizeString) && Helpers.TryParseGameSize(gameSizeString, out var gameSize))
            {
                // Filter teams by the selected game size, excluding 1v1 teams
                var teams = await TeamLookupService.SearchTeamsByGameSizeAsync(
                    userInput, gameSize, 25);

                return teams.Where(team => team.TeamSize != GameSize.OneVOne)
                    .Select(team => new DiscordAutoCompleteChoice(
                        GetDisplayName(team),
                        team.Name
                    ));
            }
        }

        // Fallback to all teams if no game size found, excluding 1v1 teams
        var allTeams = await TeamLookupService.SearchTeamsAsync(userInput, 25);
        return allTeams.Where(team => team.TeamSize != GameSize.OneVOne)
            .Select(team => new DiscordAutoCompleteChoice(
                GetDisplayName(team),
                team.Name
            ));
    }

    private static string GetDisplayName(Team team)
    {
        var baseName = !string.IsNullOrEmpty(team.Tag) ? $"{team.Name} [{team.Tag}]" : team.Name;
        var gameSizeDisplay = Helpers.GetGameSizeDisplay(team.TeamSize);
        return $"{baseName} ({gameSizeDisplay})";
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
                    var team = await TeamLookupService.GetByNameAsync(teamName);
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
            var userTeams = await TeamLookupService.GetUserTeamsAsync(userId);

            // Filter to teams where user is captain, and exclude 1v1 teams
            var captainTeams = userTeams.Where(team =>
                team.TeamSize != GameSize.OneVOne &&
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
        var gameSizeDisplay = Helpers.GetGameSizeDisplay(team.TeamSize);
        return $"{baseName} ({gameSizeDisplay})";
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
            var userTeams = await TeamLookupService.GetUserTeamsAsync(userId);

            // Filter to teams where user is a team manager, and exclude 1v1 teams
            var managedTeams = userTeams.Where(team =>
                team.TeamSize != GameSize.OneVOne &&
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
        var gameSizeDisplay = Helpers.GetGameSizeDisplay(team.TeamSize);
        return $"{baseName} ({gameSizeDisplay})";
    }
}