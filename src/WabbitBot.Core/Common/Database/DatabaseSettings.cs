using System;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// Database configuration settings
    /// </summary>
    public class DatabaseSettings
    {
        public string Provider { get; set; } = "PostgreSQL";
        public string ConnectionString { get; set; } = string.Empty;
        public int MaxPoolSize { get; set; } = 10;

        // Legacy SQLite support
        public string Path { get; set; } = "data/wabbitbot.db";

        /// <summary>
        /// Gets the effective connection string based on the provider
        /// </summary>
        public string GetEffectiveConnectionString()
        {
            if (string.IsNullOrEmpty(Provider))
                throw new InvalidOperationException("Database provider is not specified");

            return Provider.ToLowerInvariant() switch
            {
                "postgresql" => ConnectionString,
                "sqlite" => $"Data Source={Path};Pooling=True;Max Pool Size={MaxPoolSize};",
                _ => throw new NotSupportedException($"Database provider '{Provider}' is not supported")
            };
        }

        /// <summary>
        /// Validates the database configuration
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(Provider))
                throw new InvalidOperationException("Database provider must be specified");

            if (Provider.ToLowerInvariant() == "postgresql" && string.IsNullOrEmpty(ConnectionString))
                throw new InvalidOperationException("PostgreSQL connection string must be specified");

            if (Provider.ToLowerInvariant() == "sqlite" && string.IsNullOrEmpty(Path))
                throw new InvalidOperationException("SQLite database path must be specified");
        }
    }
}
