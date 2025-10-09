// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Microsoft.EntityFrameworkCore;
// using Npgsql;
// using WabbitBot.Core.Common.Database;
// using WabbitBot.Core.Common.Database.Tests;
// using WabbitBot.Core.Common.Models.Common;
// using Xunit;

// namespace WabbitBot.Core.Tests.Common.Database
// {
//     [Collection("pgsql")]
//     public sealed class WabbitBotDbContextPostgresTests
//     {
//         private readonly PgSqlFixture _fx;
//         public WabbitBotDbContextPostgresTests(PgSqlFixture fx) => _fx = fx;

//         [Fact]
//         public async Task DbContext_Has_Generated_EntityTypes()
//         {
//             if (!_fx.Available)
//             {
//                 throw new InvalidOperationException("Docker not available: start Docker Desktop to run Postgres-backed tests.");
//             }

//             var dsBuilder = new NpgsqlDataSourceBuilder(_fx.ConnectionString);
//             dsBuilder.EnableDynamicJson();
//             await using var dataSource = dsBuilder.Build();

//             var options = new DbContextOptionsBuilder<WabbitBotDbContext>()
//                 .UseNpgsql(dataSource)
//                 .Options;

//             await using var db = new WabbitBotDbContext(options);

//             // Check if the model contains our entities
//             var model = db.Model;
//             var entityTypes = model.GetEntityTypes().Select(e => e.ClrType.Name).ToList();

//             // Log what we found
//             Console.WriteLine($"Found {entityTypes.Count} entity types:");
//             foreach (var entityType in entityTypes)
//             {
//                 Console.WriteLine($"  - {entityType}");
//             }

//             // Verify key entities are present
//             Assert.Contains("Map", entityTypes);
//             Assert.Contains("Match", entityTypes);
//             Assert.Contains("Game", entityTypes);
//         }

//         [Fact]
//         public async Task Game_With_StateHistory_RoundTrips()
//         {
//             if (!_fx.Available)
//             {
//                 throw new InvalidOperationException("Docker not available: start Docker Desktop to run Postgres-backed tests.");
//             }

//             var dsBuilder = new NpgsqlDataSourceBuilder(_fx.ConnectionString);
//             dsBuilder.EnableDynamicJson();
//             await using var dataSource = dsBuilder.Build();

//             var options = new DbContextOptionsBuilder<WabbitBotDbContext>()
//                 .UseNpgsql(dataSource)
//                 .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
//                 .EnableSensitiveDataLogging()
//                 .Options;

//             await using var db = new WabbitBotDbContext(options);

//             // Log entity count before EnsureCreated
//             var entityCount = db.Model.GetEntityTypes().Count();
//             Console.WriteLine($"DbContext has {entityCount} entity types registered");

//             await db.Database.EnsureCreatedAsync();

//             var map = new Map { Name = "TestMap", Size = "1v1", IsActive = true };
//             db.Maps.Add(map);
//             await db.SaveChangesAsync();

//             var match = new Match
//             {
//                 TeamSize = TeamSize.OneVOne,
//                 AvailableMaps = new List<string> { "TestMap" },
//                 BestOf = 1,
//                 PlayToCompletion = false,
//                 Team1Id = Guid.NewGuid(),
//                 Team2Id = Guid.NewGuid(),
//                 Team1PlayerIds = new List<Guid> { Guid.NewGuid() },
//                 Team2PlayerIds = new List<Guid> { Guid.NewGuid() },
//             };
//             db.Matches.Add(match);
//             await db.SaveChangesAsync();

//             var game = new Game
//             {
//                 MatchId = match.Id,
//                 MapId = map.Id,
//                 TeamSize = TeamSize.OneVOne,
//                 Team1PlayerIds = new List<Guid> { Guid.NewGuid() },
//                 Team2PlayerIds = new List<Guid> { Guid.NewGuid() },
//                 GameNumber = 1,
//             };

//             var player = new Player { Name = "Alice" };
//             db.Players.Add(player);
//             await db.SaveChangesAsync();

//             var s1 = new GameStateSnapshot
//             {
//                 GameId = game.Id,
//                 MatchId = game.MatchId,
//                 MapId = map.Id,
//                 TeamSize = game.TeamSize,
//                 Team1PlayerIds = game.Team1PlayerIds,
//                 Team2PlayerIds = game.Team2PlayerIds,
//                 GameNumber = 1,
//                 Timestamp = DateTime.UtcNow,
//                 TriggeredByUserId = player.Id,
//                 TriggeredByUserName = player.Name,
//                 AdditionalData = new Dictionary<string, object> { ["k"] = "v" },
//             };
//             var s2 = new GameStateSnapshot
//             {
//                 GameId = game.Id,
//                 MatchId = game.MatchId,
//                 MapId = map.Id,
//                 TeamSize = game.TeamSize,
//                 Team1PlayerIds = game.Team1PlayerIds,
//                 Team2PlayerIds = game.Team2PlayerIds,
//                 GameNumber = 1,
//                 Timestamp = DateTime.UtcNow.AddSeconds(1),
//                 TriggeredByUserId = player.Id,
//                 TriggeredByUserName = "Bob",
//                 AdditionalData = new Dictionary<string, object> { ["x"] = 123 },
//             };

//             game.StateHistory.Add(s1);
//             game.StateHistory.Add(s2);

//             db.Games.Add(game);
//             await db.SaveChangesAsync();

//             var loaded = await db.Games
//                 .Include(g => g.StateHistory)
//                 .FirstOrDefaultAsync(g => g.Id == game.Id);

//             Assert.NotNull(loaded);
//             Assert.Equal(2, loaded!.StateHistory.Count);
//             Assert.Contains(loaded.StateHistory, s => s.TriggeredByUserName == "Alice");
//             Assert.Contains(loaded.StateHistory, s => s.TriggeredByUserName == "Bob");
//         }

//         [Fact]
//         public async Task Match_Arrays_RoundTrip()
//         {
//             if (!_fx.Available)
//             {
//                 throw new InvalidOperationException("Docker not available: start Docker Desktop to run Postgres-backed tests.");
//             }

//             var dsBuilder = new NpgsqlDataSourceBuilder(_fx.ConnectionString);
//             dsBuilder.EnableDynamicJson();
//             await using var dataSource = dsBuilder.Build();

//             var options = new DbContextOptionsBuilder<WabbitBotDbContext>()
//                 .UseNpgsql(dataSource)
//                 .Options;

//             await using var db = new WabbitBotDbContext(options);
//             await db.Database.EnsureCreatedAsync();

//             var match = new Match
//             {
//                 TeamSize = TeamSize.OneVOne,
//                 AvailableMaps = new List<string> { "Alpha", "Beta" },
//                 Team1Id = Guid.NewGuid(),
//                 Team2Id = Guid.NewGuid(),
//                 Team1PlayerIds = new List<Guid> { Guid.NewGuid() },
//                 Team2PlayerIds = new List<Guid> { Guid.NewGuid() },
//                 BestOf = 3,
//                 PlayToCompletion = false,
//             };

//             db.Matches.Add(match);
//             await db.SaveChangesAsync();

//             var loaded = await db.Matches.FirstOrDefaultAsync(m => m.Id == match.Id);
//             Assert.NotNull(loaded);
//             Assert.Equal(match.Team1PlayerIds, loaded!.Team1PlayerIds);
//             Assert.Equal(match.Team2PlayerIds, loaded.Team2PlayerIds);
//             Assert.Equal(match.AvailableMaps, loaded.AvailableMaps);
//         }
//     }
// }


