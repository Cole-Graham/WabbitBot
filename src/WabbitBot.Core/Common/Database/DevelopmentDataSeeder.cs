using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Seeds development data for testing purposes.
    /// Should only be used in Development environment.
    /// </summary>
    public static class DevelopmentDataSeeder
    {
        /// <summary>
        /// Seeds the database with test data for development using actual Discord user information.
        /// This method is idempotent - safe to call multiple times.
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="alphaTeamUsers">Discord user info for AlphaTeam members</param>
        /// <param name="bravoTeamUsers">Discord user info for BravoTeam members</param>
        public static async Task SeedAsync(
            WabbitBotDbContext context,
            List<DiscordUserInfo> alphaTeamUsers,
            List<DiscordUserInfo> bravoTeamUsers
        )
        {
            Console.WriteLine("🌱 Seeding development data...");

            if (alphaTeamUsers is null || alphaTeamUsers.Count == 0)
            {
                Console.WriteLine("⚠️  No AlphaTeam users provided, skipping AlphaTeam...");
            }
            else
            {
                // Seed AlphaTeam
                await SeedTeamAsync(
                    context,
                    teamName: "AlphaTeam",
                    teamTag: "ALPHA",
                    discordUserInfos: alphaTeamUsers,
                    teamType: TeamType.Team,
                    rosterGroup: TeamSizeRosterGroup.Duo
                );
            }

            if (bravoTeamUsers is null || bravoTeamUsers.Count == 0)
            {
                Console.WriteLine("⚠️  No BravoTeam users provided, skipping BravoTeam...");
            }
            else
            {
                // Seed BravoTeam
                await SeedTeamAsync(
                    context,
                    teamName: "BravoTeam",
                    teamTag: "BRAVO",
                    discordUserInfos: bravoTeamUsers,
                    teamType: TeamType.Team,
                    rosterGroup: TeamSizeRosterGroup.Duo
                );
            }

            await context.SaveChangesAsync();
            Console.WriteLine("✅ Development data seeded successfully!");
        }

        /// <summary>
        /// Seeds the database with default test data using hardcoded Discord user IDs.
        /// Falls back to placeholder data if Discord user info cannot be fetched.
        /// This is the legacy method - prefer using SeedAsync with actual Discord user info.
        /// </summary>
        public static async Task SeedWithDefaultDataAsync(WabbitBotDbContext context)
        {
            Console.WriteLine("🌱 Seeding development data with default user IDs...");
            Console.WriteLine("⚠️  Using placeholder Discord data - real Discord info not available");

            // Define test Discord user IDs
            var alphaTeamUserIds = new List<ulong> { 1348719242882584689, 1348724033306366055 };
            var bravoTeamUserIds = new List<ulong> { 1348724778906681447, 1348725467422916749 };

            // Create placeholder Discord user info
            var alphaTeamUsers = alphaTeamUserIds
                .Select(
                    (id, index) =>
                        new DiscordUserInfo
                        {
                            DiscordUserId = id,
                            Username = $"AlphaUser{index + 1}",
                            GlobalName = $"Alpha User {index + 1}",
                            Mention = $"<@{id}>",
                        }
                )
                .ToList();

            var bravoTeamUsers = bravoTeamUserIds
                .Select(
                    (id, index) =>
                        new DiscordUserInfo
                        {
                            DiscordUserId = id,
                            Username = $"BravoUser{index + 1}",
                            GlobalName = $"Bravo User {index + 1}",
                            Mention = $"<@{id}>",
                        }
                )
                .ToList();

            await SeedAsync(context, alphaTeamUsers, bravoTeamUsers);
        }

        private static async Task SeedTeamAsync(
            WabbitBotDbContext context,
            string teamName,
            string teamTag,
            List<DiscordUserInfo> discordUserInfos,
            TeamType teamType,
            TeamSizeRosterGroup rosterGroup
        )
        {
            // Check if team already exists with roster
            var existingTeam = await context
                .Teams.Include(t => t.Rosters)
                .ThenInclude(r => r.RosterMembers)
                .FirstOrDefaultAsync(t => t.Name == teamName);

            if (existingTeam is not null)
            {
                // Check if roster exists for this size
                var existingRoster = existingTeam.Rosters.FirstOrDefault(r => r.RosterGroup == rosterGroup);
                if (existingRoster is not null && existingRoster.RosterMembers.Any())
                {
                    Console.WriteLine($"⏭️  Team '{teamName}' with {rosterGroup} roster already exists, skipping...");
                    return;
                }

                // Team exists but roster is missing or incomplete - we'll add it
                Console.WriteLine($"🔧 Team '{teamName}' exists but missing {rosterGroup} roster, adding...");
            }

            // Create MashinaUsers and Players
            var players = new List<Player>();
            var teamMembers = new List<TeamMember>();
            Guid? captainPlayerId = null;

            foreach (var (userInfo, index) in discordUserInfos.Select((info, i) => (info, i)))
            {
                // Check if MashinaUser already exists
                var mashinaUser = await context.MashinaUsers.FirstOrDefaultAsync(u =>
                    u.DiscordUserId == userInfo.DiscordUserId
                );
                if (mashinaUser is null)
                {
                    mashinaUser = new MashinaUser
                    {
                        DiscordUserId = userInfo.DiscordUserId,
                        DiscordUsername = userInfo.Username,
                        DiscordGlobalname = userInfo.GlobalName,
                        DiscordMention = userInfo.Mention,
                        JoinedAt = DateTime.UtcNow,
                        LastActive = DateTime.UtcNow,
                        IsActive = true,
                    };
                    context.MashinaUsers.Add(mashinaUser);
                    await context.SaveChangesAsync(); // Save to get the Id
                    Console.WriteLine(
                        $"✅ Created MashinaUser: {userInfo.Username} ({userInfo.GlobalName}) - ID: {userInfo.DiscordUserId}"
                    );
                }
                else
                {
                    Console.WriteLine(
                        $"⏭️  MashinaUser already exists: {userInfo.Username} - ID: {userInfo.DiscordUserId}"
                    );
                }

                // Check if Player already exists for this MashinaUser
                var player = await context.Players.FirstOrDefaultAsync(p => p.MashinaUserId == mashinaUser.Id);
                if (player is null)
                {
                    // Generate a fake Steam ID (17 digits starting with 7656119)
                    var fakeSteamId = GenerateFakeSteamId(index);

                    player = new Player
                    {
                        MashinaUserId = mashinaUser.Id,
                        Name = userInfo.GlobalName, // Use actual Discord username for player name
                        LastActive = DateTime.UtcNow,
                        TeamJoinLimit = 5,
                        CurrentPlatformIds = new Dictionary<string, string> { ["Steam"] = fakeSteamId },
                        CurrentSteamUsername = $"{userInfo.GlobalName}",
                    };
                    context.Players.Add(player);
                    await context.SaveChangesAsync(); // Save to get the Id
                    Console.WriteLine(
                        $"✅ Created Player: {player.Name} {userInfo.GlobalName} (Steam ID: {fakeSteamId})"
                    );
                }
                else
                {
                    Console.WriteLine($"⏭️  Player already exists: {player.Name}");
                }

                players.Add(player);

                // First player is the captain
                if (index == 0)
                {
                    captainPlayerId = player.Id;
                }
            }

            if (captainPlayerId is null)
            {
                throw new InvalidOperationException("No captain assigned for team");
            }

            // Create Team if it doesn't exist
            Team team;
            if (existingTeam is null)
            {
                team = new Team
                {
                    Name = teamName,
                    Tag = teamTag,
                    TeamMajorId = captainPlayerId.Value,
                    LastActive = DateTime.UtcNow,
                    TeamType = teamType,
                };
                context.Teams.Add(team);
                await context.SaveChangesAsync(); // Save to get the team Id
            }
            else
            {
                team = existingTeam;
            }

            // Create TeamRoster
            var roster = new TeamRoster
            {
                TeamId = team.Id,
                RosterGroup = rosterGroup,
                MaxRosterSize = TeamCore.TeamSizeRosterGrouping.GetMaxRosterSlots(rosterGroup),
                LastActive = DateTime.UtcNow,
            };
            context.TeamRosters.Add(roster);
            await context.SaveChangesAsync(); // Save to get the roster Id

            // Create TeamMembers
            foreach (var (player, index) in players.Select((p, i) => (p, i)))
            {
                var teamMember = new TeamMember
                {
                    TeamRosterId = roster.Id,
                    MashinaUserId = player.MashinaUserId,
                    PlayerId = player.Id,
                    DiscordUserId = player.MashinaUser.DiscordUserId.ToString(),
                    Role = index == 0 ? RosterRole.Captain : RosterRole.Core,
                    ValidFrom = DateTime.UtcNow,
                    IsRosterManager = index == 0,
                    ReceiveScrimmagePings = true,
                };
                context.TeamMembers.Add(teamMember);
            }

            // Update Player TeamIds (only if not already in the team)
            foreach (var player in players)
            {
                if (!player.TeamIds.Contains(team.Id))
                {
                    player.TeamIds.Add(team.Id);
                    context.Players.Update(player);
                }
            }

            await context.SaveChangesAsync();

            if (existingTeam is null)
            {
                Console.WriteLine($"✅ Created team '{teamName}' with {rosterGroup} roster ({players.Count} players)");
            }
            else
            {
                Console.WriteLine(
                    $"✅ Added {rosterGroup} roster to existing team '{teamName}' ({players.Count} players)"
                );
            }
        }

        /// <summary>
        /// Generates a fake Steam ID for development purposes.
        /// Creates a 17-digit number starting with 7656119.
        /// </summary>
        /// <param name="index">Index to make the Steam ID unique</param>
        /// <returns>A fake Steam ID string</returns>
        private static string GenerateFakeSteamId(int index)
        {
            // Steam ID64 format: 7656119XXXXXXXXX (17 digits total)
            // The last 8 digits can be varied to create unique IDs
            var baseId = 76561190000000000L; // Base Steam ID64
            var uniquePart = (index + 1) * 1000000; // Add some variation based on index
            var randomPart = new Random().Next(100000, 999999); // Add some randomness

            return (baseId + uniquePart + randomPart).ToString();
        }
    }
}
