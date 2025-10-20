using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
        /// Seeds the database with test data for development.
        /// This method is idempotent - safe to call multiple times.
        /// </summary>
        public static async Task SeedAsync(WabbitBotDbContext context)
        {
            Console.WriteLine("🌱 Seeding development data...");

            // Define test Discord user IDs
            var alphaTeamUserIds = new List<ulong> { 1348719242882584689, 1348724033306366055 };

            var bravoTeamUserIds = new List<ulong> { 1348724778906681447, 1348725467422916749 };

            // Seed AlphaTeam
            await SeedTeamAsync(
                context,
                teamName: "AlphaTeam",
                teamTag: "ALPHA",
                discordUserIds: alphaTeamUserIds,
                teamType: TeamType.Team,
                rosterGroup: TeamSizeRosterGroup.Duo
            );

            // Seed BravoTeam
            await SeedTeamAsync(
                context,
                teamName: "BravoTeam",
                teamTag: "BRAVO",
                discordUserIds: bravoTeamUserIds,
                teamType: TeamType.Team,
                rosterGroup: TeamSizeRosterGroup.Duo
            );

            await context.SaveChangesAsync();
            Console.WriteLine("✅ Development data seeded successfully!");
        }

        private static async Task SeedTeamAsync(
            WabbitBotDbContext context,
            string teamName,
            string teamTag,
            List<ulong> discordUserIds,
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

            foreach (var (discordUserId, index) in discordUserIds.Select((id, i) => (id, i)))
            {
                // Check if MashinaUser already exists
                var mashinaUser = await context.MashinaUsers.FirstOrDefaultAsync(u => u.DiscordUserId == discordUserId);
                if (mashinaUser is null)
                {
                    mashinaUser = new MashinaUser
                    {
                        DiscordUserId = discordUserId,
                        DiscordUsername = $"TestUser{index + 1}",
                        DiscordGlobalname = $"Test User {index + 1}",
                        DiscordMention = $"<@{discordUserId}>",
                        JoinedAt = DateTime.UtcNow,
                        LastActive = DateTime.UtcNow,
                        IsActive = true,
                    };
                    context.MashinaUsers.Add(mashinaUser);
                    await context.SaveChangesAsync(); // Save to get the Id
                }

                // Check if Player already exists for this MashinaUser
                var player = await context.Players.FirstOrDefaultAsync(p => p.MashinaUserId == mashinaUser.Id);
                if (player is null)
                {
                    player = new Player
                    {
                        MashinaUserId = mashinaUser.Id,
                        Name = $"Player{index + 1}",
                        LastActive = DateTime.UtcNow,
                        TeamJoinLimit = 5,
                    };
                    context.Players.Add(player);
                    await context.SaveChangesAsync(); // Save to get the Id
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
                    TeamCaptainId = captainPlayerId.Value,
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
                MaxRosterSize = discordUserIds.Count,
                LastActive = DateTime.UtcNow,
                RosterCaptainId = captainPlayerId.Value,
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
                    Role = index == 0 ? TeamRole.Captain : TeamRole.Core,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsTeamManager = index == 0,
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
    }
}
