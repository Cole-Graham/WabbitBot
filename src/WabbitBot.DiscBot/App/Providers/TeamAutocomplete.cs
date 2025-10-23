using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.DiscBot.App.Providers
{
    public sealed class TeamNameAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;
            var userId = context.User.Id.ToString();

            var teams = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Teams.Where(t =>
                        t.Rosters.Any(r => r.RosterMembers.Any(m => m.DiscordUserId == userId && m.IsRosterManager))
                    )
                    .Where(t => t.TeamType == TeamType.Team) // exclude Solo teams from management UI
                    .Where(t => EF.Functions.ILike(t.Name, $"%{term}%"))
                    .OrderBy(t => t.Name)
                    .Select(t => new { t.Id, t.Name })
                    .Take(25)
                    .ToListAsync();
            });

            return teams.Select(t => new DiscordAutoCompleteChoice(t.Name, t.Id.ToString()));
        }
    }

    public sealed class TeamMemberAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;
            var teamIdStr = context.Options.FirstOrDefault(o => o.Name == "team")?.Value?.ToString();
            if (!Guid.TryParse(teamIdStr, out var teamId))
            {
                return Array.Empty<DiscordAutoCompleteChoice>();
            }

            var members = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .TeamMembers.Where(tm => tm.TeamRoster.TeamId == teamId && tm.ValidTo == null)
                    .OrderBy(tm => tm.DiscordUserId)
                    .Select(tm => new { tm.PlayerId, tm.DiscordUserId })
                    .Take(25)
                    .ToListAsync();
            });

            return members
                .Where(m => m.DiscordUserId.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(m => new DiscordAutoCompleteChoice(m.DiscordUserId, m.PlayerId.ToString()));
        }
    }

    public sealed class TeamRosterAutoComplete : IAutoCompleteProvider
    {
        // Returns choices in the format: label = "{TeamName} - {RosterLabel}", value = "{teamId}:{RosterGroup}"
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;
            var userId = context.User.Id.ToString();

            var teamRosters = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Teams.Where(t =>
                        t.TeamType == TeamType.Team // exclude Solo teams from selection
                        && t.Rosters.Any(r => r.RosterMembers.Any(m => m.DiscordUserId == userId && m.IsRosterManager))
                    )
                    .Include(t => t.Rosters)
                    .OrderBy(t => t.Name)
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        Rosters = t.Rosters,
                    })
                    .Take(50)
                    .ToListAsync();
            });

            static string RosterLabel(TeamSizeRosterGroup g) =>
                g switch
                {
                    TeamSizeRosterGroup.Duo => "Duo",
                    TeamSizeRosterGroup.Squad => "Squad",
                    _ => "",
                };

            var choices = new List<DiscordAutoCompleteChoice>();
            foreach (var t in teamRosters)
            {
                foreach (var r in t.Rosters.Where(r => r.RosterGroup != TeamSizeRosterGroup.Solo))
                {
                    var label = $"{t.Name} - {RosterLabel(r.RosterGroup)}";
                    if (string.IsNullOrEmpty(term) || label.Contains(term, StringComparison.OrdinalIgnoreCase))
                    {
                        choices.Add(new DiscordAutoCompleteChoice(label, $"{t.Id}:{r.RosterGroup}"));
                        if (choices.Count >= 25)
                        {
                            return choices;
                        }
                    }
                }
            }

            return choices;
        }
    }

    public sealed class TeamRosterGroupCreateAutoComplete : IAutoCompleteProvider
    {
        // Only Duo and Squad are offered for creation
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var items = new List<DiscordAutoCompleteChoice>
            {
                new DiscordAutoCompleteChoice("Duo - 2v2", TeamSizeRosterGroup.Duo),
                new DiscordAutoCompleteChoice("Squad - 3v3/4v4", TeamSizeRosterGroup.Squad),
            };
            return ValueTask.FromResult<IEnumerable<DiscordAutoCompleteChoice>>(items);
        }
    }

    // Teams the user can manage (captain or roster manager on any roster of the team)
    public sealed class TeamRolesTeamAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;
            var userId = context.User.Id.ToString();

            var teams = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Teams.Where(t =>
                        t.Rosters.Any(r =>
                            r.RosterMembers.Any(m =>
                                m.DiscordUserId == userId && (m.IsRosterManager || m.Role == RosterRole.Captain)
                            )
                        )
                    )
                    .Where(t => EF.Functions.ILike(t.Name, $"%{term}%"))
                    .OrderBy(t => t.Name)
                    .Select(t => new { t.Id, t.Name })
                    .Take(25)
                    .ToListAsync();
            });

            return teams.Select(t => new DiscordAutoCompleteChoice(t.Name, t.Id.ToString()));
        }
    }
}
