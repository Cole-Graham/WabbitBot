using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.DiscBot.App.Renderers
{
    public static class TeamRenderer
    {
        // Summary renderer removed; roster view is the single renderer now

        public static async Task<DiscordContainerComponent> RenderRosterAsync(
            Guid teamId,
            TeamSizeRosterGroup rosterGroup
        )
        {
            var team = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.Rosters)
                    .ThenInclude(r => r.RosterMembers)
                    .ThenInclude(rm => rm.MashinaUser)
                    .ThenInclude(mu => mu.Player)
                    .FirstOrDefaultAsync(t => t.Id == teamId)
            );

            if (team is null)
            {
                return new DiscordContainerComponent(
                    new List<DiscordComponent> { new DiscordTextDisplayComponent(content: "Team not found") }
                );
            }

            var roster = team.Rosters.FirstOrDefault(r => r.RosterGroup == rosterGroup);
            if (roster is null)
            {
                return new DiscordContainerComponent(
                    new List<DiscordComponent> { new DiscordTextDisplayComponent(content: "Roster not found") }
                );
            }

            var captains = roster.RosterMembers.Where(m => m.Role == RosterRole.Captain).ToList();
            var cores = roster.RosterMembers.Where(m => m.Role == RosterRole.Core).ToList();
            var otherMembers = roster
                .RosterMembers.Where(m => m.Role != RosterRole.Captain && m.Role != RosterRole.Core)
                .ToList();

            string RosterLabel(TeamSizeRosterGroup g) =>
                g switch
                {
                    TeamSizeRosterGroup.Duo => "Duo - 2v2",
                    TeamSizeRosterGroup.Squad => "Squad - 3v3/4v4",
                    TeamSizeRosterGroup.Solo => "Solo",
                    _ => g.ToString(),
                };

            long unixTimestamp = ((DateTimeOffset)team.LastActive).ToUnixTimeSeconds();

            // Display format helper is inlined in LINQ selects below

            var components = new List<DiscordComponent>
            {
                new DiscordTextDisplayComponent(content: $"# {team.Name} - {RosterLabel(rosterGroup)}"),
                new DiscordSeparatorComponent(true),
                new DiscordTextDisplayComponent(content: $"Last Active: <t:{unixTimestamp}:R>"),
                new DiscordSeparatorComponent(true),
                new DiscordTextDisplayComponent(
                    content: string.Concat(
                        captains.Any()
                            ? string.Join(
                                ", ",
                                captains.Select(m =>
                                {
                                    var steam = m.MashinaUser?.Player?.CurrentSteamUsername;
                                    var steamDisplay = string.IsNullOrWhiteSpace(steam) ? "N/A" : steam;
                                    var mentionDisplay = m.MashinaUser?.DiscordMention ?? "N/A";
                                    return $"✪ **{steamDisplay}** {mentionDisplay}";
                                })
                            )
                            : string.Empty,
                        cores.Any()
                            ? string.Join(
                                ", ",
                                cores.Select(m =>
                                {
                                    var steam = m.MashinaUser?.Player?.CurrentSteamUsername;
                                    var steamDisplay = string.IsNullOrWhiteSpace(steam) ? "N/A" : steam;
                                    var mentionDisplay = m.MashinaUser?.DiscordMention ?? "N/A";
                                    return $"\n     **{steamDisplay}** {mentionDisplay}";
                                })
                            )
                            : string.Empty,
                        otherMembers.Any()
                            ? string.Join(
                                ", ",
                                otherMembers.Select(m =>
                                {
                                    var steam = m.MashinaUser?.Player?.CurrentSteamUsername;
                                    var steamDisplay = string.IsNullOrWhiteSpace(steam) ? "N/A" : steam;
                                    var mentionDisplay = m.MashinaUser?.DiscordMention ?? "N/A";
                                    return $"\n     **{steamDisplay}** {mentionDisplay}";
                                })
                            )
                            : string.Empty
                    )
                ),
            };

            return new DiscordContainerComponent(components);
        }

        public static async Task<DiscordContainerComponent> RenderTeamRoleEditorAsync(
            ulong discordUserId,
            Guid? selectedTeamId = null,
            TeamSizeRosterGroup? selectedRosterGroup = null,
            Guid? selectedMemberPlayerId = null
        )
        {
            var components = new List<DiscordComponent>();

            // Load teams where user is a roster captain or manager
            var teams = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Where(t =>
                        t.Rosters.Any(r =>
                            r.RosterMembers.Any(m =>
                                m.MashinaUser != null
                                && m.MashinaUser.DiscordUserId == discordUserId
                                && (m.Role == RosterRole.Captain || m.IsRosterManager)
                            )
                        )
                    )
                    .OrderBy(t => t.Name)
                    .Select(t => new { t.Id, t.Name })
                    .Take(25)
                    .ToListAsync()
            );

            var teamOptions = teams
                .Select(t => new DiscordSelectComponentOption(
                    t.Name,
                    t.Id.ToString(),
                    isDefault: selectedTeamId.HasValue && selectedTeamId.Value == t.Id
                ))
                .ToList();

            if (teamOptions.Count == 0)
            {
                teamOptions.Add(new DiscordSelectComponentOption("No teams available", "none"));
            }
            var teamSelect = new DiscordSelectComponent(
                $"team_roles_team_{discordUserId}",
                "Select Team",
                teamOptions,
                minOptions: 0,
                maxOptions: 1
            );
            components.Add(new DiscordActionRowComponent([teamSelect]));

            // Roster select (for selected team)
            List<DiscordSelectComponentOption> rosterOptions = new();
            if (selectedTeamId.HasValue)
            {
                var rosters = await CoreService.WithDbContext(async db =>
                    await db
                        .TeamRosters.Where(r => r.TeamId == selectedTeamId.Value)
                        .Select(r => new { r.RosterGroup })
                        .ToListAsync()
                );

                static string RosterLabel(TeamSizeRosterGroup g) =>
                    g switch
                    {
                        TeamSizeRosterGroup.Duo => "Duo",
                        TeamSizeRosterGroup.Squad => "Squad",
                        _ => "Solo",
                    };

                rosterOptions = rosters
                    .Where(r => r.RosterGroup != TeamSizeRosterGroup.Solo)
                    .Select(r => new DiscordSelectComponentOption(
                        RosterLabel(r.RosterGroup),
                        r.RosterGroup.ToString(),
                        isDefault: selectedRosterGroup.HasValue && selectedRosterGroup.Value == r.RosterGroup
                    ))
                    .ToList();
            }

            if (rosterOptions.Count == 0)
            {
                rosterOptions.Add(
                    new DiscordSelectComponentOption(
                        selectedTeamId.HasValue ? "No rosters available" : "Select a team first",
                        "none"
                    )
                );
            }
            var rosterSelect = new DiscordSelectComponent(
                $"team_roles_roster_{selectedTeamId?.ToString() ?? "none"}",
                "Select Roster",
                rosterOptions,
                minOptions: 0,
                maxOptions: 1
            );
            components.Add(new DiscordActionRowComponent([rosterSelect]));

            // Member select (for selected team and roster)
            List<DiscordSelectComponentOption> memberOptions = new();
            if (selectedTeamId.HasValue && selectedRosterGroup.HasValue)
            {
                var members = await CoreService.WithDbContext(async db =>
                    await db
                        .TeamMembers.Where(tm =>
                            tm.TeamRoster.TeamId == selectedTeamId.Value
                            && tm.TeamRoster.RosterGroup == selectedRosterGroup.Value
                            && tm.ValidTo == null
                        )
                        .Include(tm => tm.MashinaUser)
                        .Select(tm => new
                        {
                            tm.PlayerId,
                            Name = tm.MashinaUser!.DiscordGlobalname
                                ?? tm.MashinaUser!.DiscordUsername
                                ?? tm.DiscordUserId,
                        })
                        .OrderBy(x => x.Name)
                        .Take(25)
                        .ToListAsync()
                );

                memberOptions = members
                    .Select(m => new DiscordSelectComponentOption(
                        m.Name,
                        m.PlayerId.ToString(),
                        isDefault: selectedMemberPlayerId.HasValue && selectedMemberPlayerId.Value == m.PlayerId
                    ))
                    .ToList();
            }

            if (memberOptions.Count == 0)
            {
                var placeholder =
                    (selectedTeamId.HasValue && selectedRosterGroup.HasValue)
                        ? "No members available"
                        : "Select team and roster first";
                memberOptions.Add(new DiscordSelectComponentOption(placeholder, "none"));
            }
            var memberSelect = new DiscordSelectComponent(
                $"team_roles_member_{selectedTeamId?.ToString() ?? "none"}_{selectedRosterGroup?.ToString() ?? "none"}",
                "Select Team Member",
                memberOptions,
                minOptions: 0,
                maxOptions: 1
            );
            components.Add(new DiscordActionRowComponent([memberSelect]));

            components.Add(new DiscordSeparatorComponent(true));

            // Role buttons
            components.Add(
                new DiscordActionRowComponent(
                    [
                        new DiscordButtonComponent(
                            DiscordButtonStyle.Primary,
                            "team_roles_set_captain",
                            "✪ Roster Captain"
                        ),
                        new DiscordButtonComponent(
                            DiscordButtonStyle.Success,
                            "team_roles_set_core",
                            "︽ Roster Core Player"
                        ),
                        new DiscordButtonComponent(
                            DiscordButtonStyle.Secondary,
                            "team_roles_toggle_manager",
                            "︿ Manager"
                        ),
                        new DiscordButtonComponent(DiscordButtonStyle.Danger, "team_roles_clear", "None"),
                    ]
                )
            );

            components.Add(new DiscordSeparatorComponent(true));

            // Roster display with emojis
            if (selectedTeamId.HasValue && selectedRosterGroup.HasValue)
            {
                var team = await CoreService.WithDbContext(async db =>
                    await db
                        .Teams.Include(t => t.Rosters)
                        .ThenInclude(r => r.RosterMembers)
                        .ThenInclude(m => m.MashinaUser)
                        .FirstOrDefaultAsync(t => t.Id == selectedTeamId.Value)
                );

                if (team is not null)
                {
                    var roster = team.Rosters.FirstOrDefault(r => r.RosterGroup == selectedRosterGroup.Value);
                    if (roster is not null)
                    {
                        static string? IconFor(TeamMember m)
                        {
                            if (m.Role == RosterRole.Captain)
                                return "✪ ";
                            if (m.Role == RosterRole.Core)
                                return "︽ ";
                            if (m.IsRosterManager)
                                return "︿ ";
                            return "     ";
                        }

                        var lines = roster
                            .RosterMembers.Where(m => m.ValidTo == null)
                            .Select(m =>
                            {
                                var icon = IconFor(m);
                                var name = m.MashinaUser?.DiscordMention ?? m.DiscordUserId;
                                return $"{icon}{name}";
                            });

                        components.Add(new DiscordTextDisplayComponent(string.Join("\n", lines)));
                    }
                }
            }

            return new DiscordContainerComponent(components);
        }
    }
}
