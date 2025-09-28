using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
        /// Initializes the provider with database settings from configuration
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            var databaseSettings = new DatabaseSettings();
            configuration.GetSection("Bot:Database").Bind(databaseSettings);
            databaseSettings.Validate();

            _connectionString = databaseSettings.GetEffectiveConnectionString();

            var optionsBuilder = new DbContextOptionsBuilder<WabbitBotDbContext>();

            if (databaseSettings.Provider.ToLowerInvariant() == "postgresql")
            {
                optionsBuilder.UseNpgsql(_connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
            }
            else
            {
                throw new NotSupportedException($"Database provider '{databaseSettings.Provider}' is not supported. Only 'postgresql' is supported.");
            }

            if (configuration.GetValue<bool>("UseDetailedErrors", false))
            {
                optionsBuilder.EnableDetailedErrors();
            }

            if (configuration.GetValue<bool>("UseSensitiveDataLogging", false))
            {
                optionsBuilder.EnableSensitiveDataLogging();
            }

            _options = optionsBuilder.Options;
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
