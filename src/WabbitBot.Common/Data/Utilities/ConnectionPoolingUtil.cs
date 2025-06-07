using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Common.Data.Utilities
{
    public class ConnectionPoolingUtil
    {
        private readonly ConcurrentDictionary<string, IDatabaseConnection> _connectionPool;
        private readonly int _maxPoolSize;
        private readonly string _dbPath;

        public ConnectionPoolingUtil(string dbPath, int maxPoolSize = 10)
        {
            _dbPath = dbPath;
            _maxPoolSize = maxPoolSize;
            _connectionPool = new ConcurrentDictionary<string, IDatabaseConnection>();
        }

        public async Task<IDatabaseConnection> GetConnectionAsync()
        {
            var connectionId = Guid.NewGuid().ToString();

            if (_connectionPool.Count >= _maxPoolSize)
            {
                // Try to reuse an existing connection
                foreach (var kvp in _connectionPool)
                {
                    if (_connectionPool.TryRemove(kvp.Key, out var connection))
                    {
                        if (connection.IsConnected)
                        {
                            _connectionPool.TryAdd(connectionId, connection);
                            return connection;
                        }
                        else
                        {
                            connection.Dispose();
                        }
                    }
                }
            }

            // Create new connection
            var newConnection = new DatabaseConnection(_dbPath);
            await newConnection.ConnectAsync();

            _connectionPool.TryAdd(connectionId, newConnection);
            return newConnection;
        }

        public async Task ReleaseConnectionAsync(IDatabaseConnection connection)
        {
            if (connection == null) return;

            // Remove any dead connections
            foreach (var kvp in _connectionPool.Where(x => !x.Value.IsConnected))
            {
                if (_connectionPool.TryRemove(kvp.Key, out var deadConnection))
                {
                    deadConnection.Dispose();
                }
            }

            // If pool is full, dispose the connection
            if (_connectionPool.Count >= _maxPoolSize)
            {
                connection.Dispose();
                return;
            }

            // Otherwise keep it in the pool
            if (connection.IsConnected)
            {
                _connectionPool.TryAdd(Guid.NewGuid().ToString(), connection);
            }
            else
            {
                connection.Dispose();
            }
        }

        public async Task CloseAllConnectionsAsync()
        {
            foreach (var kvp in _connectionPool)
            {
                if (_connectionPool.TryRemove(kvp.Key, out var connection))
                {
                    await connection.DisconnectAsync();
                    connection.Dispose();
                }
            }
        }
    }
}
