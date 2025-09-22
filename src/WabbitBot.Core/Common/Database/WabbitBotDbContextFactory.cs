using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Factory for creating WabbitBotDbContext instances
    /// Used for EF Core migrations and runtime instantiation
    /// </summary>
    public class WabbitBotDbContextFactory : IDesignTimeDbContextFactory<WabbitBotDbContext>
    {
        public WabbitBotDbContext CreateDbContext(string[] args)
        {
            // For design-time operations (migrations), use environment variables or default connection string
            var connectionString = Environment.GetEnvironmentVariable("WABBITBOT_CONNECTION_STRING")
                ?? "Host=localhost;Database=wabbitbot;Username=wabbitbot;Password=password123";

            var optionsBuilder = new DbContextOptionsBuilder<WabbitBotDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new WabbitBotDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Creates a DbContext with the specified connection string
        /// </summary>
        public static WabbitBotDbContext CreateDbContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WabbitBotDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new WabbitBotDbContext(optionsBuilder.Options);
        }
    }
}
