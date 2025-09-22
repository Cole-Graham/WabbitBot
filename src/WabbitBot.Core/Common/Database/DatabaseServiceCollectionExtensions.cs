using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Extension methods for configuring database services
    /// </summary>
    public static class DatabaseServiceCollectionExtensions
    {
        /// <summary>
        /// Adds database services to the DI container
        /// Note: This only registers the DbContext, not runtime service injection
        /// </summary>
        public static IServiceCollection AddDatabaseServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Bind database settings from configuration
            var databaseSettings = new DatabaseSettings();
            configuration.GetSection("Bot:Database").Bind(databaseSettings);
            databaseSettings.Validate();

            // Register database settings
            services.AddSingleton(databaseSettings);

            // Configure DbContext with the appropriate provider
            services.AddDbContext<WabbitBotDbContext>(options =>
            {
                var connectionString = databaseSettings.GetEffectiveConnectionString();

                if (databaseSettings.Provider.ToLowerInvariant() == "postgresql")
                {
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        // Configure Npgsql options for JSONB support
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);

                        // Enable JSONB support
                        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                }
                else if (databaseSettings.Provider.ToLowerInvariant() == "sqlite")
                {
                    options.UseSqlite(connectionString);
                }
                else
                {
                    throw new NotSupportedException($"Database provider '{databaseSettings.Provider}' is not supported");
                }

                // Common EF Core options
                if (configuration.GetValue<bool>("UseDetailedErrors", false))
                {
                    options.EnableDetailedErrors();
                }

                if (configuration.GetValue<bool>("UseSensitiveDataLogging", false))
                {
                    options.EnableSensitiveDataLogging();
                }
            });

            return services;
        }
    }
}
