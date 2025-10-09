using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.DiscBot.App.Providers
{
    /// <summary>
    /// Autocomplete provider for user's teams in ChallengeAsync
    /// </summary>
    /// <remarks>
    /// This provider filters teams based on whether the user is a member of any roster
    /// that matches the selected game size's roster group.
    /// </remarks>
    public sealed class UserTeamAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;
            var teamSize = context.Options.FirstOrDefault(o => o.Name == "teamSize")?.Value?.ToString();
            var teamSizeEnum = teamSize != null ? Enum.Parse<TeamSize>(teamSize, true) : TeamSize.ThreeVThree;

            var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(teamSizeEnum);

            var teams = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Teams.Where(t =>
                        t.Rosters.Any(r =>
                            r.RosterGroup == rosterGroup
                            && r.RosterMembers.Any(m => m.DiscordUserId == context.User.Id.ToString())
                        )
                    )
                    .Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.Name)
                    .Select(t => new { t.Name, t.Id })
                    .Take(25)
                    .ToListAsync();
            });

            return teams.Select(t => new DiscordAutoCompleteChoice(t.Name, t.Name));
        }
    }

    /// <summary>
    /// Autocomplete provider for opponent teams in ChallengeAsync
    /// </summary>
    /// <remarks>
    /// This provider filters out all user's teams and teams in active matches,
    /// and only shows teams that have rosters for the selected game size group.
    /// </remarks>
    public sealed class OpponentTeamAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;
            var teamSize = context.Options.FirstOrDefault(o => o.Name == "teamSize")?.Value?.ToString();

            var teamSizeEnum = teamSize != null ? Enum.Parse<TeamSize>(teamSize, true) : TeamSize.ThreeVThree;

            var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(teamSizeEnum);

            var teams = await CoreService.WithDbContext(async db =>
            {
                // Get user's team IDs efficiently via Player entity
                var player = await db
                    .MashinaUsers.Where(mu => mu.DiscordUserId == context.User.Id)
                    .Include(mu => mu.Player)
                    .Select(mu => mu.Player)
                    .FirstOrDefaultAsync();

                var userTeamIds = player?.TeamIds ?? [];

                return await db
                    .Teams.Where(t => t.Rosters.Any(r => r.RosterGroup == rosterGroup)) // Must have roster for this game size
                    .Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Where(t => !userTeamIds.Contains(t.Id)) // Exclude all user's teams
                    .Where(t => !t.Matches.Any(m => m.CompletedAt == null)) // Exclude teams in active matches
                    .OrderBy(t => t.Name)
                    .Select(t => new { t.Name, t.Id })
                    .Take(25)
                    .ToListAsync();
            });

            return teams.Select(t => new DiscordAutoCompleteChoice(t.Name, t.Name));
        }
    }

    /// <summary>
    /// Autocomplete provider for roster players in ChallengeAsync
    /// </summary>
    /// <remarks>
    /// This provider shows active players from the selected challenger team's roster
    /// that matches the selected game size's roster group.
    /// </remarks>
    public sealed class RosterPlayerAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;
            var challengerTeamName = context
                .Options.FirstOrDefault(o => o.Name == "challengerTeamName")
                ?.Value?.ToString();
            var teamSize = context.Options.FirstOrDefault(o => o.Name == "teamSize")?.Value?.ToString();

            if (string.IsNullOrEmpty(challengerTeamName))
            {
                return [];
            }

            // Get the roster group for the selected team size
            var teamSizeEnum = teamSize != null ? Enum.Parse<TeamSize>(teamSize, true) : TeamSize.ThreeVThree;
            var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(teamSizeEnum);

            var players = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Teams.Where(t => t.Name.Equals(challengerTeamName, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(t => t.Rosters)
                    .Where(r => r.RosterGroup == rosterGroup) // Filter to the right roster group
                    .SelectMany(r => r.RosterMembers) // Get members from that roster
                    .Where(tm => tm.IsActive)
                    .Include(tm => tm.MashinaUser)
                    .Where(tm =>
                        tm.MashinaUser != null
                        && (
                            tm.MashinaUser.DiscordUsername.Contains(term, StringComparison.OrdinalIgnoreCase)
                            || tm.MashinaUser.DiscordGlobalname.Contains(term, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    .OrderBy(tm => tm.MashinaUser!.DiscordUsername)
                    .Select(tm => new
                    {
                        DisplayName = string.IsNullOrEmpty(tm.MashinaUser!.DiscordGlobalname)
                            ? tm.MashinaUser.DiscordUsername
                            : tm.MashinaUser.DiscordGlobalname,
                        PlayerId = tm.PlayerId,
                    })
                    .Take(25)
                    .ToListAsync();
            });

            return players.Select(p => new DiscordAutoCompleteChoice(p.DisplayName, p.PlayerId.ToString()));
        }
    }

    /// <summary>
    /// Autocomplete provider for active scrimmage challenges
    /// </summary>
    /// <remarks>
    /// This provider shows pending challenges in a "Challenger vs Opponent" format
    /// for easy identification by admins, while returning the challenge ID.
    /// </remarks>
    public sealed class ActiveChallengeAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;

            var challenges = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .ScrimmageChallenges.Where(c => c.ChallengeStatus == ScrimmageChallengeStatus.Pending)
                    .Include(c => c.ChallengerTeam)
                    .Include(c => c.OpponentTeam)
                    .Where(c => c.ChallengerTeam != null && c.OpponentTeam != null)
                    .Where(c =>
                        c.ChallengerTeam!.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || c.OpponentTeam!.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                    )
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new { DisplayName = $"{c.ChallengerTeam!.Name} vs {c.OpponentTeam!.Name}", c.Id })
                    .Take(25)
                    .ToListAsync();
            });

            return challenges.Select(c => new DiscordAutoCompleteChoice(c.DisplayName, c.Id.ToString()));
        }
    }
}
