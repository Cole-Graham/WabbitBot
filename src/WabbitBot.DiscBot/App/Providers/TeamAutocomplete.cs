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
                    .Teams.Include(t => t.TeamMajor)
                    .ThenInclude(p => p.MashinaUser)
                    .Where(t =>
                        t.Rosters.Any(r => r.RosterMembers.Any(m => m.DiscordUserId == userId && m.IsRosterManager))
                        || t.TeamMajor.MashinaUser.DiscordUserId == context.User.Id
                    )
                    .Where(t => t.TeamType == TeamType.Team) // exclude Solo teams from management UI
                    .Where(t => EF.Functions.ILike(t.Name, $"%{term}%"))
                    .OrderBy(t => t.Name)
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        MajorDiscordGlobalName = t.TeamMajor.MashinaUser.DiscordGlobalname,
                    })
                    .Take(25)
                    .ToListAsync();
            });

            return teams.Select(t =>
            {
                var label = t.Name;
                if (!string.IsNullOrWhiteSpace(t.MajorDiscordGlobalName))
                {
                    label += $" ({t.MajorDiscordGlobalName})";
                }
                return new DiscordAutoCompleteChoice(label, t.Id.ToString());
            });
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

            var teamRosters = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Teams.Where(t => t.TeamType == TeamType.Team) // exclude Solo teams from selection
                    .Include(t => t.Rosters)
                    .Include(t => t.TeamMajor)
                    .ThenInclude(p => p.MashinaUser)
                    .OrderBy(t => t.Name)
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        Rosters = t.Rosters,
                        MajorDiscordGlobalName = t.TeamMajor.MashinaUser.DiscordGlobalname,
                    })
                    .Take(10)
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
                    var teamLabel = t.Name;
                    if (!string.IsNullOrWhiteSpace(t.MajorDiscordGlobalName))
                    {
                        teamLabel += $" ({t.MajorDiscordGlobalName})";
                    }
                    var label = $"{teamLabel} - {RosterLabel(r.RosterGroup)}";
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
                new DiscordAutoCompleteChoice("Duo - 2v2", (long)TeamSizeRosterGroup.Duo),
                new DiscordAutoCompleteChoice("Squad - 3v3/4v4", (long)TeamSizeRosterGroup.Squad),
            };
            return ValueTask.FromResult<IEnumerable<DiscordAutoCompleteChoice>>(items);
        }
    }

    // All teams in the guild (no management restriction), by name match
    public sealed class AllTeamsAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;

            var teams = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Teams.Include(t => t.TeamMajor)
                    .ThenInclude(p => p.MashinaUser)
                    .Where(t => EF.Functions.ILike(t.Name, $"%{term}%"))
                    .OrderBy(t => t.Name)
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        MajorDiscordGlobalName = t.TeamMajor.MashinaUser.DiscordGlobalname,
                    })
                    .Take(25)
                    .ToListAsync();
            });

            return teams.Select(t =>
            {
                var label = t.Name;
                if (!string.IsNullOrWhiteSpace(t.MajorDiscordGlobalName))
                {
                    label += $" ({t.MajorDiscordGlobalName})";
                }
                return new DiscordAutoCompleteChoice(label, t.Id.ToString());
            });
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
                    .Teams.Include(t => t.TeamMajor)
                    .ThenInclude(p => p.MashinaUser)
                    .Where(t =>
                        t.Rosters.Any(r =>
                            r.RosterMembers.Any(m =>
                                m.DiscordUserId == userId && (m.IsRosterManager || m.Role == RosterRole.Captain)
                            )
                        )
                        || t.TeamMajor.MashinaUser.DiscordUserId == context.User.Id
                    )
                    .Where(t => EF.Functions.ILike(t.Name, $"%{term}%"))
                    .OrderBy(t => t.Name)
                    .Select(t => new
                    {
                        t.Id,
                        t.Name,
                        MajorDiscordGlobalName = t.TeamMajor.MashinaUser.DiscordGlobalname,
                    })
                    .Take(25)
                    .ToListAsync();
            });

            return teams.Select(t =>
            {
                var label = t.Name;
                if (!string.IsNullOrWhiteSpace(t.MajorDiscordGlobalName))
                {
                    label += $" ({t.MajorDiscordGlobalName})";
                }
                return new DiscordAutoCompleteChoice(label, t.Id.ToString());
            });
        }
    }
}
