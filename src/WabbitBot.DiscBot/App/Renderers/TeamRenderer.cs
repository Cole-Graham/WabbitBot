using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using WabbitBot.Common.Configuration;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Services.DiscBot;

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
            var config = ConfigurationProvider.GetConfigurationService();
            var emojiOptions = config.GetSection<EmojisOptions>("Bot:Emojis");

            var team = await CoreService.WithDbContext(async db =>
                await db
                    .Teams.Include(t => t.TeamMajor)
                    .ThenInclude(p => p.MashinaUser)
                    .Include(t => t.Rosters)
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
            var teamMajor = team.TeamMajor;
            var allMembers = roster.RosterMembers.ToList();
            var teamRole1Emoji = DiscordEmoji.FromGuildEmote(DiscBotService.Client, emojiOptions.TeamRole1EmojiId);
            string RosterLabel(TeamSizeRosterGroup g) =>
                g switch
                {
                    TeamSizeRosterGroup.Duo =>
                        $"[{teamRole1Emoji}{teamMajor.MashinaUser?.DiscordMention ?? "??"}] Duo - 2v2",
                    TeamSizeRosterGroup.Squad =>
                        $"[{teamRole1Emoji}{teamMajor.MashinaUser?.DiscordMention ?? "??"}] Squad - 3v3/4v4",
                    TeamSizeRosterGroup.Solo => "Solo",
                    _ => g.ToString(),
                };

            long unixTimestamp = ((DateTimeOffset)team.LastActive).ToUnixTimeSeconds();

            // Display format helper is inlined in LINQ selects below
            var teamRole2Emoji = DiscordEmoji.FromGuildEmote(DiscBotService.Client, emojiOptions.TeamRole2EmojiId);
            var components = new List<DiscordComponent>
            {
                new DiscordTextDisplayComponent(content: $"# {team.Name} {RosterLabel(rosterGroup)}"),
                new DiscordSeparatorComponent(true),
                new DiscordTextDisplayComponent(content: $"Last Active: <t:{unixTimestamp}:R>"),
                new DiscordSeparatorComponent(true),
                new DiscordTextDisplayComponent(
                    content: string.Join(
                        "\n",
                        allMembers
                            .OrderBy(m =>
                            {
                                // Priority ordering: Major (1), Captain (2), Core (3), Manager (4), Standard (5)
                                if (
                                    m.TeamRoster.Team.TeamMajor.MashinaUser.DiscordUserId
                                    == m.MashinaUser?.DiscordUserId
                                )
                                    return 1; // Team Major
                                if (m.Role == RosterRole.Captain)
                                    return 2; // Captain
                                if (m.Role == RosterRole.Core)
                                    return 3; // Core Player
                                if (m.IsRosterManager)
                                    return 4; // Manager
                                return 5; // Standard member
                            })
                            .ThenBy(m =>
                                m.MashinaUser?.DiscordGlobalname
                                ?? m.MashinaUser?.DiscordUsername
                                ?? m.DiscordUserId.ToString()
                            )
                            .Select(m =>
                            {
                                string IconFor(TeamMember member)
                                {
                                    if (
                                        member.TeamRoster.Team.TeamMajor.MashinaUser.DiscordUserId
                                        == member.MashinaUser?.DiscordUserId
                                    )
                                        return $"{teamRole1Emoji} ";
                                    if (member.Role == RosterRole.Captain)
                                        return $"{teamRole2Emoji} ";
                                    if (member.Role == RosterRole.Core)
                                        return "︽   ";
                                    if (member.IsRosterManager)
                                        return "︿   ";
                                    return "     ";
                                }

                                var steam = m.MashinaUser?.Player?.CurrentSteamUsername;
                                var steamDisplay = string.IsNullOrWhiteSpace(steam) ? "N/A" : steam;
                                var mentionDisplay = m.MashinaUser?.DiscordMention ?? "N/A";
                                var icon = IconFor(m);
                                return $"{icon}{mentionDisplay} **{steamDisplay}**";
                            })
                    )
                ),
            };

            return new DiscordContainerComponent(components);
        }

        public static async Task<DiscordContainerComponent> RenderTeamEditorAsync(
            ulong discordUserId,
            Guid? selectedTeamId = null,
            TeamSizeRosterGroup? selectedRosterGroup = null,
            Guid? selectedMemberPlayerId = null,
            bool adminMode = false,
            bool awaitingAddInput = false
        )
        {
            var config = ConfigurationProvider.GetConfigurationService();
            var emojiOptions = config.GetSection<EmojisOptions>("Bot:Emojis");

            var components = new List<DiscordComponent>();

            // Load teams where user is a roster captain, manager, or team major
            var teams = await CoreService.WithDbContext(async db =>
            {
                var query = db.Teams.AsQueryable();

                if (!adminMode)
                {
                    query = query.Where(t =>
                        t.Rosters.Any(r =>
                            r.RosterMembers.Any(m =>
                                m.MashinaUser != null
                                && m.MashinaUser.DiscordUserId == discordUserId
                                && (m.Role == RosterRole.Captain || m.IsRosterManager)
                            )
                        )
                        || t.TeamMajor.MashinaUser.DiscordUserId == discordUserId
                    );
                }

                return await query.OrderBy(t => t.Name).Select(t => new { t.Id, t.Name }).Take(25).ToListAsync();
            });

            if (!adminMode)
            {
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
            }

            var teamRole1Emoji = DiscordEmoji.FromGuildEmote(DiscBotService.Client, emojiOptions.TeamRole1EmojiId);

            // Admin header: show which team is being edited
            if (adminMode && selectedTeamId.HasValue)
            {
                var teamInfo = await CoreService.WithDbContext(async db =>
                {
                    return await db
                        .Teams.Where(t => t.Id == selectedTeamId.Value)
                        .Select(t => new { t.Name, MajorDiscordGlobalName = t.TeamMajor.MashinaUser.DiscordMention })
                        .FirstOrDefaultAsync();
                });
                if (!string.IsNullOrWhiteSpace(teamInfo?.Name))
                {
                    var headerText = $"Editing team: **{teamInfo.Name}**";
                    if (!string.IsNullOrWhiteSpace(teamInfo.MajorDiscordGlobalName))
                    {
                        headerText += $" ({teamRole1Emoji} {teamInfo.MajorDiscordGlobalName})";
                    }
                    components.Add(new DiscordTextDisplayComponent(content: headerText));
                }
            }

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
            var teamRole1ComponentEmoji = new DiscordComponentEmoji(
                DiscordEmoji.FromGuildEmote(DiscBotService.Client, emojiOptions.TeamRole1EmojiId)
            );
            var teamRole2ComponentEmoji = new DiscordComponentEmoji(
                DiscordEmoji.FromGuildEmote(DiscBotService.Client, emojiOptions.TeamRole2EmojiId)
            );

            // Role buttons
            components.Add(
                new DiscordActionRowComponent(
                    [
                        new DiscordButtonComponent(
                            DiscordButtonStyle.Secondary,
                            "team_roles_set_major",
                            string.Empty,
                            disabled: false,
                            emoji: teamRole1ComponentEmoji
                        ),
                        new DiscordButtonComponent(
                            DiscordButtonStyle.Secondary,
                            "team_roles_set_captain",
                            "Roster Captain",
                            disabled: false,
                            emoji: teamRole2ComponentEmoji
                        ),
                        new DiscordButtonComponent(
                            DiscordButtonStyle.Secondary,
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

            // Admin add/remove controls
            if (adminMode && selectedTeamId.HasValue)
            {
                components.Add(
                    new DiscordActionRowComponent(
                        [
                            new DiscordButtonComponent(
                                DiscordButtonStyle.Success,
                                "team_roles_admin_add_player",
                                "+ Add Player"
                            ),
                            new DiscordButtonComponent(
                                DiscordButtonStyle.Danger,
                                "team_roles_admin_remove_player",
                                "− Remove Player"
                            ),
                            new DiscordButtonComponent(
                                DiscordButtonStyle.Danger,
                                "team_roles_admin_delete_roster",
                                "Delete Roster"
                            ),
                            new DiscordButtonComponent(
                                DiscordButtonStyle.Danger,
                                "team_roles_admin_delete_team",
                                "Delete Team"
                            ),
                        ]
                    )
                );
                if (awaitingAddInput)
                {
                    components.Add(
                        new DiscordTextDisplayComponent(
                            content: "Reply to this message with a Discord mention or numeric ID of"
                                + "the user you want to add."
                        )
                    );
                }
            }

            components.Add(new DiscordSeparatorComponent(true));

            // Roster display with emojis
            if (selectedTeamId.HasValue && selectedRosterGroup.HasValue)
            {
                var team = await CoreService.WithDbContext(async db =>
                    await db
                        .Teams.Include(t => t.TeamMajor.MashinaUser)
                        .Include(t => t.Rosters)
                        .ThenInclude(r => r.RosterMembers)
                        .ThenInclude(m => m.MashinaUser)
                        .FirstOrDefaultAsync(t => t.Id == selectedTeamId.Value)
                );
                var scrimmageConfig = ConfigurationProvider.GetSection<ScrimmageOptions>(ScrimmageOptions.SectionName);
                var teamRules = scrimmageConfig.TeamRules;
                var maxRosterSlots = TeamCore.TeamSizeRosterGrouping.GetMaxRosterSlots(selectedRosterGroup.Value);
                var (maxCaptainSlots, maxCoreSlots) = TeamCore.TeamSizeRosterGrouping.GetRosterRoleCaps(
                    selectedRosterGroup.Value
                );
                if (team is null)
                {
                    components.Add(new DiscordTextDisplayComponent(content: "Team not found"));
                    return new DiscordContainerComponent(components);
                }
                var rosterSlotsUsed = team.Rosters.Sum(r => r.RosterMembers.Count(m => m.ValidTo == null));
                var captainSlotsUsed = team.Rosters.Sum(r =>
                    r.RosterMembers.Count(m =>
                        m.ValidTo == null && (m.Role == RosterRole.Captain || m.PlayerId == team.TeamMajorId)
                    )
                );
                var coreSlotsUsed = team.Rosters.Sum(r =>
                    r.RosterMembers.Count(m => m.ValidTo == null && m.Role == RosterRole.Core)
                );
                var majorEmoji = DiscordEmoji.FromGuildEmote(DiscBotService.Client, emojiOptions.TeamRole1EmojiId);
                var captainEmoji = DiscordEmoji.FromGuildEmote(DiscBotService.Client, emojiOptions.TeamRole2EmojiId);
                components.Add(
                    new DiscordTextDisplayComponent(
                        content: $"**Roster:**        👥 {rosterSlotsUsed}/{maxRosterSlots}        "
                            + $"{captainEmoji} : {captainSlotsUsed}/{maxCaptainSlots}        "
                            + $"︽ : {coreSlotsUsed}/{maxCoreSlots}"
                    )
                );
                components.Add(new DiscordSeparatorComponent(true));

                if (team is not null)
                {
                    var roster = team.Rosters.FirstOrDefault(r => r.RosterGroup == selectedRosterGroup.Value);
                    if (roster is not null)
                    {
                        string? IconFor(TeamMember m)
                        {
                            if (m.TeamRoster.Team.TeamMajor.MashinaUser.DiscordUserId == m.MashinaUser?.DiscordUserId)
                                return $"{majorEmoji} ";
                            if (m.Role == RosterRole.Captain)
                                return $"{captainEmoji} ";
                            if (m.Role == RosterRole.Core)
                                return "︽ ";
                            if (m.IsRosterManager)
                                return "︿ ";
                            return "     ";
                        }

                        var lines = roster
                            .RosterMembers.Where(m => m.ValidTo == null)
                            .OrderBy(m =>
                            {
                                // Priority ordering: Major (1), Captain (2), Core (3), Manager (4), Standard (5)
                                if (
                                    m.TeamRoster.Team.TeamMajor.MashinaUser.DiscordUserId
                                    == m.MashinaUser?.DiscordUserId
                                )
                                    return 1; // Team Major
                                if (m.Role == RosterRole.Captain)
                                    return 2; // Captain
                                if (m.Role == RosterRole.Core)
                                    return 3; // Core Player
                                if (m.IsRosterManager)
                                    return 4; // Manager
                                return 5; // Standard member
                            })
                            .ThenBy(m =>
                                m.MashinaUser?.DiscordGlobalname ?? m.MashinaUser?.DiscordUsername ?? m.DiscordUserId
                            )
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
