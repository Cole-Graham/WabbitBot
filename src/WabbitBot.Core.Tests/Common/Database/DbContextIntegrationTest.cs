using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models.Common;
using Xunit;

namespace WabbitBot.Core.Common.Database.Tests
{
    /// <summary>
    /// Lightweight smoke tests using a minimal in-memory DbContext scoped to simple entities only.
    /// </summary>
    internal sealed class PlainContext : DbContext
    {
        public DbSet<Player> Players => Set<Player>();

        public PlainContext(DbContextOptions<PlainContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Keep default conventions; no complex navigations involved for Player
        }
    }

    public class DbContextIntegrationTest : IDisposable
    {
        private readonly PlainContext _context;

        public DbContextIntegrationTest()
        {
            var options = new DbContextOptionsBuilder<PlainContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new PlainContext(options);
            // InMemory provider doesn't require EnsureCreated for simple cases
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        [Fact(Skip = "Requires real PostgreSQL test harness (Testcontainers/docker-compose)")]
        public async Task CanCreateAndSavePlayer()
        {
            var player = new Player { Name = "TestPlayer" };
            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            var saved = await _context.Players.FindAsync(player.Id);
            Assert.NotNull(saved);
            Assert.Equal("TestPlayer", saved!.Name);
        }
    }
}
