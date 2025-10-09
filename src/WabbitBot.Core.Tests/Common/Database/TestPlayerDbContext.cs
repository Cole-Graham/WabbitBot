using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Models.Common;

namespace WabbitBot.Core.Common.Database.Tests
{
    /// <summary>
    /// Minimal DbContext for Postgres tests that only includes the Player entity.
    /// Avoids pulling the full application model which contains unsupported mappings for this test scope.
    /// </summary>
    public sealed class TestPlayerDbContext : DbContext
    {
        public TestPlayerDbContext(DbContextOptions<TestPlayerDbContext> options)
            : base(options) { }

        public DbSet<Player> Players { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Player>();

            // Match production table name from EntityMetadata
            entity.ToTable("players");

            // Key from Entity base
            entity.HasKey(p => p.Id);

            // Arrays and JSONB mappings for Postgres
            entity.Property(p => p.TeamIds).HasColumnType("uuid[]");

            entity.Property(p => p.PreviousGameUsernames).HasColumnType("text[]");

            entity.Property(p => p.TeamJoinCooldowns).HasColumnType("jsonb");

            entity.Property(p => p.PreviousPlatformIds).HasColumnType("jsonb");
        }
    }
}
