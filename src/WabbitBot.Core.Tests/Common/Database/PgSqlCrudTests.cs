using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Models.Common;
using Xunit;

namespace WabbitBot.Core.Common.Database.Tests
{
    [Collection("pgsql")]
    public sealed class PgSqlCrudTests
    {
        private readonly PgSqlFixture _fx;
        public PgSqlCrudTests(PgSqlFixture fx) => _fx = fx;

        [Fact]
        public async Task Player_Crud_Works_With_Postgres()
        {
            if (!_fx.Available)
            {
                throw new InvalidOperationException("Docker not available: start Docker Desktop or ensure Docker Engine is running to execute container-backed integration tests.");
            }

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_fx.ConnectionString);
            dataSourceBuilder.EnableDynamicJson();
            await using var dataSource = dataSourceBuilder.Build();

            var options = new DbContextOptionsBuilder<TestPlayerDbContext>()
                .UseNpgsql(dataSource)
                .Options;

            await using var db = new TestPlayerDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var player = new Player
            {
                Name = "TC_User",
                TeamIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
                PreviousUserIds = new Dictionary<string, List<string>>
                {
                    ["Discord"] = new() { "u1", "u2" },
                },
                LastActive = DateTime.UtcNow,
            };

            db.Players.Add(player);
            await db.SaveChangesAsync();

            var found = await db.Players.FindAsync(player.Id);
            Assert.NotNull(found);
            Assert.Equal("TC_User", found!.Name);
            Assert.Equal(2, found.TeamIds.Count);
            Assert.True(found.PreviousUserIds.ContainsKey("Discord"));
        }
    }
}


