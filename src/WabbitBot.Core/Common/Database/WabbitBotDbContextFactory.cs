using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

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
            // For design-time operations (migrations), read from actual configuration files
            // This ensures consistency between runtime and design-time
            var connectionString = GetConnectionStringFromConfiguration();

            var optionsBuilder = new DbContextOptionsBuilder<WabbitBotDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new WabbitBotDbContext(optionsBuilder.Options);
        }

        private static string GetConnectionStringFromConfiguration()
        {
            // Check environment variable first (for production/CI scenarios)
            var envConnectionString = Environment.GetEnvironmentVariable("WABBITBOT_CONNECTION_STRING");
            if (!string.IsNullOrEmpty(envConnectionString))
            {
                return envConnectionString;
            }

            // Build configuration from appsettings files (same as Program.cs)
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Find the Host project directory (appsettings files are there)
            var hostProjectPath = FindHostProjectPath();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(hostProjectPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration["Bot:Database:ConnectionString"];

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string not found in configuration. "
                        + "Please ensure 'Bot:Database:ConnectionString' is set in appsettings.json or via environment variables."
                );
            }

            return connectionString;
        }

        private static string FindHostProjectPath()
        {
            // Start from current directory and search upwards for the Host project
            var currentDir = Directory.GetCurrentDirectory();

            // Check if we're already in the Host directory
            if (File.Exists(Path.Combine(currentDir, "appsettings.json")))
            {
                return currentDir;
            }

            // Look for src/WabbitBot.Host from current directory
            var hostPath = Path.Combine(currentDir, "src", "WabbitBot.Host");
            if (Directory.Exists(hostPath) && File.Exists(Path.Combine(hostPath, "appsettings.json")))
            {
                return hostPath;
            }

            // Search parent directories
            var directory = new DirectoryInfo(currentDir);
            while (directory?.Parent is not null)
            {
                hostPath = Path.Combine(directory.Parent.FullName, "src", "WabbitBot.Host");
                if (Directory.Exists(hostPath) && File.Exists(Path.Combine(hostPath, "appsettings.json")))
                {
                    return hostPath;
                }
                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                $"Could not find WabbitBot.Host project directory with appsettings.json. Current directory: {currentDir}"
            );
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
