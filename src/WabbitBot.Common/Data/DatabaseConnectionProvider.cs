using System.Data;
using Npgsql;

namespace WabbitBot.Common.Data
{
    /// <summary>
    /// PostgreSQL connection provider using Npgsql
    /// </summary>
    public static class DatabaseConnectionProvider
    {
        private static string? _connectionString;
        private static NpgsqlConnection? _sharedConnection;

        /// <summary>
        /// Initialize the PostgreSQL connection provider
        /// </summary>
        /// <param name="connectionString">PostgreSQL connection string</param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Get a PostgreSQL connection
        /// </summary>
        public static async Task<NpgsqlConnection> GetConnectionAsync()
        {
            if (_connectionString is null)
            {
                throw new InvalidOperationException(
                    "DatabaseConnectionProvider has not been initialized. Call Initialize() first."
                );
            }

            // For now, create new connections. In production, consider connection pooling.
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        /// <summary>
        /// Get a shared connection (use carefully, not thread-safe)
        /// </summary>
        public static async Task<NpgsqlConnection> GetSharedConnectionAsync()
        {
            if (_connectionString is null)
            {
                throw new InvalidOperationException(
                    "DatabaseConnectionProvider has not been initialized. Call Initialize() first."
                );
            }

            if (_sharedConnection is null || _sharedConnection.State != ConnectionState.Open)
            {
                _sharedConnection = new NpgsqlConnection(_connectionString);
                await _sharedConnection.OpenAsync();
            }

            return _sharedConnection;
        }

        /// <summary>
        /// Close the shared connection
        /// </summary>
        public static async Task CloseSharedConnectionAsync()
        {
            if (_sharedConnection is not null)
            {
                await _sharedConnection.CloseAsync();
                await _sharedConnection.DisposeAsync();
                _sharedConnection = null;
            }
        }

        /// <summary>
        /// Test the database connection
        /// </summary>
        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = await GetConnectionAsync();
                using var command = new NpgsqlCommand("SELECT 1", connection);
                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
