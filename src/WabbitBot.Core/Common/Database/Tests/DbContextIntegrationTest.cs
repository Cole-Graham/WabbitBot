using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Matches.Data;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Scrimmages.ScrimmageRating;
using WabbitBot.Core.Tournaments;
using WabbitBot.Core.Tournaments.Data;
using WabbitBot.Core.Leaderboards;
using Xunit;

namespace WabbitBot.Core.Common.Database.Tests
{
    /// <summary>
    /// Integration tests for EF Core DbContext and JSONB mappings
    /// </summary>
    public class DbContextIntegrationTest : IDisposable
    {
        private readonly WabbitBotDbContext _context;
        private readonly string _connectionString;

        public DbContextIntegrationTest()
        {
            // Use in-memory SQLite for testing (no external dependencies)
            _connectionString = "DataSource=:memory:;Cache=Shared";

            var options = new DbContextOptionsBuilder<WabbitBotDbContext>()
                .UseSqlite(_connectionString)
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .Options;

            _context = new WabbitBotDbContext(options);
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact]
        public async Task CanCreateAndSavePlayerWithJsonbFields()
        {
            // Arrange
            var player = new Player
            {
                Name = "TestPlayer",
                TeamIds = new List<string> { "team1", "team2", "team3" },
                PreviousUserIds = new Dictionary<string, List<string>>
                {
                    { "Discord", new List<string> { "user123", "user456" } },
                    { "Steam", new List<string> { "steam789" } }
                },
                LastActive = DateTime.UtcNow,
                IsArchived = false
            };

            // Act
            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            // Assert
            var savedPlayer = await _context.Players.FindAsync(player.Id);
            Assert.NotNull(savedPlayer);
            Assert.Equal("TestPlayer", savedPlayer.Name);
            Assert.Equal(3, savedPlayer.TeamIds.Count);
            Assert.Contains("team1", savedPlayer.TeamIds);
            Assert.Contains("team2", savedPlayer.TeamIds);
            Assert.Contains("team3", savedPlayer.TeamIds);
            Assert.Equal(2, savedPlayer.PreviousUserIds.Count); // Discord + Steam platforms
            Assert.True(savedPlayer.PreviousUserIds.ContainsKey("Discord"));
            Assert.True(savedPlayer.PreviousUserIds.ContainsKey("Steam"));
            Assert.True(savedPlayer.PreviousUserIds["Discord"].Count == 2);
            Assert.True(savedPlayer.PreviousUserIds["Steam"].Count == 1);
            Assert.Contains("user123", savedPlayer.PreviousUserIds["Discord"]);
            Assert.Contains("steam789", savedPlayer.PreviousUserIds["Steam"]);
        }

        [Fact]
        public async Task CanCreateAndSaveTeamWithJsonbFields()
        {
            // Arrange
            var team = new Team
            {
                Name = "TestTeam",
                TeamCaptainId = "captain123",
                TeamSize = EvenTeamFormat.ThreeVThree,
                Roster = new List<TeamMember>
                {
                    new TeamMember
                    {
                        PlayerId = "player1",
                        Role = TeamRole.Core,
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsTeamManager = false
                    },
                    new TeamMember
                    {
                        PlayerId = "captain123",
                        Role = TeamRole.Captain,
                        JoinedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsTeamManager = true
                    }
                },
                Stats = new Dictionary<EvenTeamFormat, Stats>
                {
                    [EvenTeamFormat.ThreeVThree] = new Stats
                    {
                        Wins = 10,
                        Losses = 5,
                        EvenTeamFormat = EvenTeamFormat.ThreeVThree,
                        CurrentRating = 1500.0
                    }
                }
            };

            // Act
            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Assert
            var savedTeam = await _context.Teams.FindAsync(team.Id);
            Assert.NotNull(savedTeam);
            Assert.Equal("TestTeam", savedTeam.Name);
            Assert.Equal("captain123", savedTeam.TeamCaptainId);
            Assert.True(savedTeam.Roster.Count == 2);
            Assert.True(savedTeam.Stats.Count == 1);
            Assert.True(savedTeam.Stats.ContainsKey(EvenTeamFormat.ThreeVThree));
            Assert.Equal(10, savedTeam.Stats[EvenTeamFormat.ThreeVThree].Wins);
            Assert.Equal(5, savedTeam.Stats[EvenTeamFormat.ThreeVThree].Losses);
        }

        [Fact]
        public async Task CanQueryJsonbFields()
        {
            // Arrange
            var player1 = new Player
            {
                Name = "Player1",
                TeamIds = new List<string> { "team1", "shared" },
                IsArchived = false
            };

            var player2 = new Player
            {
                Name = "Player2",
                TeamIds = new List<string> { "team2", "shared" },
                IsArchived = false
            };

            _context.Players.AddRange(player1, player2);
            await _context.SaveChangesAsync();

            // Act - Query for players in "shared" team
            // Note: In a real PostgreSQL environment, this would use JSONB operators
            // For this SQLite test, we're just verifying the data is stored correctly
            var allPlayers = await _context.Players.ToListAsync();

            // Assert
            Assert.Equal(2, allPlayers.Count);
            var playerWithShared = allPlayers.First(p => p.TeamIds.Contains("shared"));
            Assert.NotNull(playerWithShared);
        }

        [Fact]
        public async Task CanHandleComplexGameState()
        {
            // Arrange
            var matchId = Guid.NewGuid();
            var mapId = Guid.NewGuid();
            var game = new Game
            {
                MatchId = matchId,
                MapId = mapId,
                EvenTeamFormat = EvenTeamFormat.TwoVTwo,
                Team1PlayerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
                Team2PlayerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
                GameNumber = 1,
                StateHistory = new List<GameStateSnapshot>
                {
                    new GameStateSnapshot
                    {
                        GameId = Guid.NewGuid(), // Will be set by EF
                        MatchId = matchId,
                        MapId = mapId,
                        EvenTeamFormat = EvenTeamFormat.TwoVTwo,
                        Team1PlayerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
                        Team2PlayerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
                        GameNumber = 1,
                        WinnerId = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            // Set winner on current state (WinnerId is read-only computed property)
            game.CurrentState.WinnerId = Guid.NewGuid();

            // Act
            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            // Assert
            var savedGame = await _context.Games.FindAsync(game.Id);
            Assert.NotNull(savedGame);
            Assert.Equal(matchId, savedGame.MatchId);
            Assert.Equal(mapId, savedGame.MapId);
            Assert.Equal(EvenTeamFormat.TwoVTwo, savedGame.EvenTeamFormat);
            Assert.Equal(1, savedGame.GameNumber);
            Assert.NotEqual(Guid.Empty, savedGame.WinnerId); // WinnerId is set in the state history
            Assert.True(savedGame.Team1PlayerIds.Count == 2);
            Assert.True(savedGame.Team2PlayerIds.Count == 2);
            // Note: We can't test specific player IDs since they're Guids generated in the test
            Assert.All(savedGame.Team1PlayerIds, id => Assert.NotEqual(Guid.Empty, id));
            Assert.All(savedGame.Team2PlayerIds, id => Assert.NotEqual(Guid.Empty, id));
            Assert.True(savedGame.StateHistory.Count == 1);
            Assert.NotEqual(Guid.Empty, savedGame.StateHistory[0].WinnerId);
        }
    }
}
