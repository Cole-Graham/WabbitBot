using System;
using Microsoft.EntityFrameworkCore;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Static provider for WabbitBotDbContext instances
    /// Since the application avoids runtime dependency injection,
    /// this provides a global way to access the database context
    /// </summary>
    public static class WabbitBotDbContextProvider
    {
        private static DbContextOptions<WabbitBotDbContext>? _options;
        private static string? _connectionString;

        /// <summary>
        /// Initializes the provider with database settings
        /// </summary>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

            _options = new DbContextOptionsBuilder<WabbitBotDbContext>()
                .UseNpgsql(connectionString)
                .Options;
        }

        /// <summary>
        /// Creates a new DbContext instance
        /// </summary>
        public static WabbitBotDbContext CreateDbContext()
        {
            if (_options == null)
                throw new InvalidOperationException("DbContextProvider has not been initialized. Call Initialize() first.");

            return new WabbitBotDbContext(_options);
        }

        /// <summary>
        /// Gets the connection string (for migration purposes)
        /// </summary>
        public static string GetConnectionString()
        {
            return _connectionString ?? throw new InvalidOperationException("DbContextProvider has not been initialized.");
        }
    }
}
