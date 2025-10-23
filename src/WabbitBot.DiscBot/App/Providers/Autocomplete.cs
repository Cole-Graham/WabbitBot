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

            // Discord slash commands lowercase parameter names
            var teamSize = context
                .Options.FirstOrDefault(o => o.Name.Equals("team_size", StringComparison.OrdinalIgnoreCase))
                ?.Value?.ToString();

            // Debug logging
            Console.WriteLine(
                $"[UserTeamAutoComplete] User ID: {context.User.Id}, TeamSize: '{teamSize}', Search: '{term}'"
            );
            Console.WriteLine(
                $"[UserTeamAutoComplete] Options: {string.Join(", ", context.Options.Select(o => $"{o.Name}={o.Value}"))}"
            );

            var teamSizeEnum = !string.IsNullOrEmpty(teamSize)
                ? Enum.Parse<TeamSize>(teamSize, true)
                : TeamSize.TwoVTwo;
            var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(teamSizeEnum);

            Console.WriteLine($"[UserTeamAutoComplete] Parsed TeamSize: {teamSizeEnum}, RosterGroup: {rosterGroup}");

            var userIdString = context.User.Id.ToString();
            var teams = await CoreService.WithDbContext(async db =>
            {
                var result = await db
                    .Teams.Where(t =>
                        t.Rosters.Any(r =>
                            r.RosterGroup == rosterGroup && r.RosterMembers.Any(m => m.DiscordUserId == userIdString)
                        )
                    )
                    .Where(t => EF.Functions.ILike(t.Name, $"%{term}%"))
                    .OrderBy(t => t.Name)
                    .Select(t => new { t.Name, t.Id })
                    .Take(25)
                    .ToListAsync();

                Console.WriteLine($"[UserTeamAutoComplete] Found {result.Count} teams");
                return result;
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

            // Discord slash commands convert PascalCase to snake_case
            var teamSize = context
                .Options.FirstOrDefault(o => o.Name.Equals("team_size", StringComparison.OrdinalIgnoreCase))
                ?.Value?.ToString();

            var teamSizeEnum = !string.IsNullOrEmpty(teamSize)
                ? Enum.Parse<TeamSize>(teamSize, true)
                : TeamSize.TwoVTwo;

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
                    .Where(t => EF.Functions.ILike(t.Name, $"%{term}%"))
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

            // Discord slash commands convert PascalCase to snake_case
            var challengerTeamName = context
                .Options.FirstOrDefault(o => o.Name.Equals("challenger_team_name", StringComparison.OrdinalIgnoreCase))
                ?.Value?.ToString();
            var teamSize = context
                .Options.FirstOrDefault(o => o.Name.Equals("team_size", StringComparison.OrdinalIgnoreCase))
                ?.Value?.ToString();

            if (string.IsNullOrEmpty(challengerTeamName))
            {
                return [];
            }

            // Get the roster group for the selected team size
            var teamSizeEnum = !string.IsNullOrEmpty(teamSize)
                ? Enum.Parse<TeamSize>(teamSize, true)
                : TeamSize.TwoVTwo;
            var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(teamSizeEnum);

            var players = await CoreService.WithDbContext(async db =>
            {
                return await db
                    .Teams.Where(t => EF.Functions.ILike(t.Name, challengerTeamName))
                    .SelectMany(t => t.Rosters)
                    .Where(r => r.RosterGroup == rosterGroup) // Filter to the right roster group
                    .SelectMany(r => r.RosterMembers) // Get members from that roster
                    .Where(tm => tm.ValidTo == null)
                    .Include(tm => tm.MashinaUser)
                    .Where(tm =>
                        tm.MashinaUser != null
                        && (
                            EF.Functions.ILike(tm.MashinaUser.DiscordUsername ?? string.Empty, $"%{term}%")
                            || EF.Functions.ILike(tm.MashinaUser.DiscordGlobalname ?? string.Empty, $"%{term}%")
                        )
                    )
                    .OrderBy(tm => tm.MashinaUser!.DiscordUsername ?? string.Empty)
                    .Select(tm => new
                    {
                        DisplayName = string.IsNullOrEmpty(tm.MashinaUser!.DiscordGlobalname)
                            ? tm.MashinaUser.DiscordUsername ?? "Unknown"
                            : tm.MashinaUser.DiscordGlobalname,
                        PlayerId = tm.PlayerId,
                    })
                    .Take(25)
                    .ToListAsync();
            });

            return players.Select(p => new DiscordAutoCompleteChoice(
                p.DisplayName ?? "Unknown",
                p.PlayerId.ToString()
            ));
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
                        EF.Functions.ILike(c.ChallengerTeam!.Name, $"%{term}%")
                        || EF.Functions.ILike(c.OpponentTeam!.Name, $"%{term}%")
                    )
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new { DisplayName = $"{c.ChallengerTeam!.Name} vs {c.OpponentTeam!.Name}", c.Id })
                    .Take(25)
                    .ToListAsync();
            });

            return challenges.Select(c => new DiscordAutoCompleteChoice(c.DisplayName, c.Id.ToString()));
        }
    }

    /// <summary>
    /// Autocomplete provider for cancellable scrimmage challenges
    /// </summary>
    /// <remarks>
    /// This provider only shows pending challenges where the user is authorized to cancel them:
    /// - User is the player who issued the challenge, OR
    /// - User is the captain of the challenger team
    /// </remarks>
    public sealed class CancellableChallengeAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;

            var challenges = await CoreService.WithDbContext(async db =>
            {
                // Get the player associated with the Discord user
                var player = await db
                    .Players.Where(p => p.MashinaUser.DiscordUserId == context.User.Id)
                    .Select(p => new { p.Id })
                    .FirstOrDefaultAsync();

                if (player is null)
                {
                    return [];
                }

                // Find challenges where:
                // 1. Status is Pending
                // 2. User is the issuer OR user is the team captain
                return await db
                    .ScrimmageChallenges.Where(c => c.ChallengeStatus == ScrimmageChallengeStatus.Pending)
                    .Include(c => c.ChallengerTeam)
                    .Include(c => c.OpponentTeam)
                    .Where(c =>
                        c.ChallengerTeam != null
                        && c.OpponentTeam != null
                        && (c.IssuedByPlayerId == player.Id || c.ChallengerTeam.TeamMajorId == player.Id)
                    )
                    .Where(c =>
                        EF.Functions.ILike(c.ChallengerTeam!.Name, $"%{term}%")
                        || EF.Functions.ILike(c.OpponentTeam!.Name, $"%{term}%")
                    )
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new { DisplayName = $"{c.ChallengerTeam!.Name} vs {c.OpponentTeam!.Name}", c.Id })
                    .Take(25)
                    .ToListAsync();
            });

            return challenges.Select(c => new DiscordAutoCompleteChoice(c.DisplayName, c.Id.ToString()));
        }
    }

    /// <summary>
    /// Autocomplete provider for active games where the user is a participant
    /// </summary>
    /// <remarks>
    /// This provider shows games where the user is on one of the teams and the game hasn't been completed yet.
    /// Shows in format: "Team1 vs Team2 - Game 1 (Map Name)"
    /// </remarks>
    public sealed class UserActiveGameAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;

            var games = await CoreService.WithDbContext(async db =>
            {
                // Get the player associated with the Discord user
                var player = await db
                    .Players.Where(p => p.MashinaUser.DiscordUserId == context.User.Id)
                    .Select(p => new { p.Id })
                    .FirstOrDefaultAsync();

                if (player is null)
                {
                    return [];
                }

                // Find games where:
                // 1. Match is not completed
                // 2. User is in one of the teams (check Match.Team1PlayerIds or Team2PlayerIds)
                // 3. Required navigation properties are loaded (null checks)
                return await db
                    .Games.Include(g => g.Match)
                    .ThenInclude(m => m.Team1)
                    .Include(g => g.Match)
                    .ThenInclude(m => m.Team2)
                    .Include(g => g.Map)
                    .Where(g =>
                        g.Match.CompletedAt == null
                        && g.Match.Team1 != null
                        && g.Match.Team2 != null
                        && g.Map != null
                        && (
                            g.Match.Team1Players != null
                            && g.Match.Team1Players.Any(p => p.Id == player.Id)
                            && g.Match.Team2Players != null
                            && g.Match.Team2Players.Any(p => p.Id == player.Id)
                        )
                    )
                    .Where(g =>
                        EF.Functions.ILike(g.Match.Team1.Name, $"%{term}%")
                        || EF.Functions.ILike(g.Match.Team2.Name, $"%{term}%")
                        || EF.Functions.ILike(g.Map.Name, $"%{term}%")
                    )
                    .OrderByDescending(g => g.CreatedAt)
                    .Select(g => new
                    {
                        DisplayName = $"{g.Match.Team1.Name} vs {g.Match.Team2.Name} - Game {g.GameNumber} ({g.Map.Name})",
                        g.Id,
                    })
                    .Take(25)
                    .ToListAsync();
            });

            return games.Select(g => new DiscordAutoCompleteChoice(g.DisplayName, g.Id.ToString()));
        }
    }

    /// <summary>
    /// Autocomplete provider for active scrimmages where the user is a participant
    /// </summary>
    /// <remarks>
    /// This provider shows scrimmages where the user is on one of the teams and the scrimmage is in progress.
    /// Shows in format: "Team1 vs Team2"
    /// </remarks>
    public sealed class UserActiveScrimmageAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;

            var scrimmages = await CoreService.WithDbContext(async db =>
            {
                // Get the player associated with the Discord user
                var player = await db
                    .Players.Where(p => p.MashinaUser.DiscordUserId == context.User.Id)
                    .Select(p => new { p.Id })
                    .FirstOrDefaultAsync();

                if (player is null)
                {
                    return [];
                }

                // Find scrimmages where:
                // 1. Scrimmage is in progress (CompletedAt is null)
                // 2. User is in one of the teams (check ChallengerTeamPlayers or OpponentTeamPlayers)
                return await db
                    .Scrimmages.Include(s => s.ChallengerTeam)
                    .Include(s => s.OpponentTeam)
                    .Where(s =>
                        s.CompletedAt == null
                        && (
                            s.ChallengerTeamPlayers.Any(p => p.Id == player.Id)
                            || s.OpponentTeamPlayers.Any(p => p.Id == player.Id)
                        )
                    )
                    .Where(s =>
                        EF.Functions.ILike(s.ChallengerTeam.Name, $"%{term}%")
                        || EF.Functions.ILike(s.OpponentTeam.Name, $"%{term}%")
                    )
                    .OrderByDescending(s => s.StartedAt)
                    .Select(s => new { DisplayName = $"{s.ChallengerTeam.Name} vs {s.OpponentTeam.Name}", s.Id })
                    .Take(25)
                    .ToListAsync();
            });

            return scrimmages.Select(s => new DiscordAutoCompleteChoice(s.DisplayName, s.Id.ToString()));
        }
    }

    /// <summary>
    /// Autocomplete provider for players currently in a scrimmage
    /// </summary>
    /// <remarks>
    /// This provider shows players from the user's team in the selected scrimmage.
    /// </remarks>
    public sealed class ScrimmagePlayerAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;
            var scrimmageIdStr = context.Options.FirstOrDefault(o => o.Name == "scrimmageId")?.Value?.ToString();

            if (string.IsNullOrEmpty(scrimmageIdStr) || !Guid.TryParse(scrimmageIdStr, out var scrimmageId))
            {
                return [];
            }

            var players = await CoreService.WithDbContext(async db =>
            {
                // Get the player associated with the Discord user
                var currentUser = await db
                    .Players.Where(p => p.MashinaUser.DiscordUserId == context.User.Id)
                    .Select(p => new { p.Id })
                    .FirstOrDefaultAsync();

                if (currentUser is null)
                {
                    return [];
                }

                // Find the scrimmage
                var scrimmage = await db.Scrimmages.FirstOrDefaultAsync(s => s.Id == scrimmageId);
                if (scrimmage is null)
                {
                    return [];
                }

                // Determine which team the user is on
                var userTeamPlayerIds =
                    scrimmage.ChallengerTeamPlayers.Any(p => p.Id == currentUser.Id)
                        ? scrimmage.ChallengerTeamPlayers.Select(p => p.Id)
                    : scrimmage.OpponentTeamPlayers.Any(p => p.Id == currentUser.Id)
                        ? scrimmage.OpponentTeamPlayers.Select(p => p.Id)
                    : [];

                if (!userTeamPlayerIds.Any())
                {
                    return [];
                }

                // Get players from user's team (excluding the user themselves)
                return await db
                    .Players.Where(p => userTeamPlayerIds.Contains(p.Id) && p.Id != currentUser.Id)
                    .Where(p =>
                        p.MashinaUser != null
                        && (
                            EF.Functions.ILike(p.MashinaUser.DiscordUsername ?? string.Empty, $"%{term}%")
                            || EF.Functions.ILike(p.MashinaUser.DiscordGlobalname ?? string.Empty, $"%{term}%")
                        )
                    )
                    .OrderBy(p => p.MashinaUser!.DiscordUsername ?? string.Empty)
                    .Select(p => new
                    {
                        DisplayName = string.IsNullOrEmpty(p.MashinaUser!.DiscordGlobalname)
                            ? p.MashinaUser.DiscordUsername ?? "Unknown"
                            : p.MashinaUser.DiscordGlobalname,
                        p.Id,
                    })
                    .Take(25)
                    .ToListAsync();
            });

            return players.Select(p => new DiscordAutoCompleteChoice(p.DisplayName ?? "Unknown", p.Id.ToString()));
        }
    }

    /// <summary>
    /// Autocomplete provider for eligible substitute players from the roster
    /// </summary>
    /// <remarks>
    /// This provider shows active roster members from the user's team who are NOT currently in the scrimmage.
    /// </remarks>
    public sealed class SubstitutePlayerAutoComplete : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            var term = context.UserInput ?? string.Empty;
            var scrimmageIdStr = context.Options.FirstOrDefault(o => o.Name == "scrimmageId")?.Value?.ToString();

            if (string.IsNullOrEmpty(scrimmageIdStr) || !Guid.TryParse(scrimmageIdStr, out var scrimmageId))
            {
                return [];
            }

            var players = await CoreService.WithDbContext(async db =>
            {
                // Get the player associated with the Discord user
                var currentUser = await db
                    .Players.Where(p => p.MashinaUser.DiscordUserId == context.User.Id)
                    .Select(p => new { p.Id })
                    .FirstOrDefaultAsync();

                if (currentUser is null)
                {
                    return [];
                }

                // Find the scrimmage
                var scrimmage = await db
                    .Scrimmages.Include(s => s.Match)
                    .ThenInclude(m => m!.Team1)
                    .Include(s => s.Match)
                    .ThenInclude(m => m!.Team2)
                    .FirstOrDefaultAsync(s => s.Id == scrimmageId);

                if (scrimmage is null || scrimmage.Match is null)
                {
                    return [];
                }

                // Determine which team the user is on
                Guid userTeamId;
                ICollection<Guid> currentPlayerIds;
                if (scrimmage.ChallengerTeamPlayers.Any(p => p.Id == currentUser.Id))
                {
                    userTeamId = scrimmage.ChallengerTeamId;
                    currentPlayerIds = [.. scrimmage.ChallengerTeamPlayers.Select(p => p.Id)];
                }
                else if (scrimmage.OpponentTeamPlayers.Any(p => p.Id == currentUser.Id))
                {
                    userTeamId = scrimmage.OpponentTeamId;
                    currentPlayerIds = [.. scrimmage.OpponentTeamPlayers.Select(p => p.Id)];
                }
                else
                {
                    return [];
                }

                // Get the roster group for the scrimmage team size
                var rosterGroup = TeamCore.TeamSizeRosterGrouping.GetRosterGroup(scrimmage.TeamSize);

                // Get roster members who are NOT currently in the scrimmage
                return await db
                    .Teams.Where(t => t.Id == userTeamId)
                    .SelectMany(t => t.Rosters)
                    .Where(r => r.RosterGroup == rosterGroup)
                    .SelectMany(r => r.RosterMembers)
                    .Where(tm => tm.ValidTo == null && !currentPlayerIds.Contains(tm.PlayerId))
                    .Include(tm => tm.MashinaUser)
                    .Where(tm =>
                        tm.MashinaUser != null
                        && (
                            EF.Functions.ILike(tm.MashinaUser.DiscordUsername ?? string.Empty, $"%{term}%")
                            || EF.Functions.ILike(tm.MashinaUser.DiscordGlobalname ?? string.Empty, $"%{term}%")
                        )
                    )
                    .OrderBy(tm => tm.MashinaUser!.DiscordUsername ?? string.Empty)
                    .Select(tm => new
                    {
                        DisplayName = string.IsNullOrEmpty(tm.MashinaUser!.DiscordGlobalname)
                            ? tm.MashinaUser.DiscordUsername ?? "Unknown"
                            : tm.MashinaUser.DiscordGlobalname,
                        tm.PlayerId,
                    })
                    .Take(25)
                    .ToListAsync();
            });

            return players.Select(p => new DiscordAutoCompleteChoice(
                p.DisplayName ?? "Unknown",
                p.PlayerId.ToString()
            ));
        }
    }
}
