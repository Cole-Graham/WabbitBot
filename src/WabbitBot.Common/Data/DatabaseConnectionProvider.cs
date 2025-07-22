using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data
{
    /// <summary>
    /// Static provider for database connections following the project's service locator pattern
    /// </summary>
    public static class DatabaseConnectionProvider
    {
        private static IDatabaseConnection? _connection;
        private static ConnectionPoolingUtil? _connectionPool;

        /// <summary>
        /// Initialize the database connection provider
        /// </summary>
        /// <param name="dbPath">Path to the database file</param>
        /// <param name="maxPoolSize">Maximum number of connections in the pool</param>
        public static void Initialize(string dbPath, int maxPoolSize = 10)
        {
            _connectionPool = new ConnectionPoolingUtil(dbPath, maxPoolSize);
        }

        /// <summary>
        /// Get a database connection from the pool
        /// </summary>
        public static async Task<IDatabaseConnection> GetConnectionAsync()
        {
            if (_connectionPool is null)
            {
                throw new InvalidOperationException(
                    "DatabaseConnectionProvider has not been initialized. Call Initialize() first.");
            }

            return await _connectionPool.GetConnectionAsync();
        }

        /// <summary>
        /// Release a database connection back to the pool
        /// </summary>
        public static Task ReleaseConnectionAsync(IDatabaseConnection connection)
        {
            if (_connectionPool is null)
            {
                return Task.CompletedTask;
            }

            return _connectionPool.ReleaseConnectionAsync(connection);
        }

        /// <summary>
        /// Close all connections in the pool
        /// </summary>
        public static async Task CloseAllConnectionsAsync()
        {
            if (_connectionPool is not null)
            {
                await _connectionPool.CloseAllConnectionsAsync();
            }
        }
    }
}