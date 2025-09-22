using System;
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Common.Data
{
    /// <summary>
    /// PostgreSQL database connection using Npgsql
    /// </summary>
    public class DatabaseConnection : IDatabaseConnection
    {
        private NpgsqlConnection? _connection;
        private readonly string _connectionString;
        private bool _disposed;

        public bool IsConnected => _connection?.State == ConnectionState.Open;

        public DatabaseConnection(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (_connection == null)
                {
                    _connection = new NpgsqlConnection(_connectionString);
                }

                if (_connection.State != ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection?.State == ConnectionState.Open)
            {
                await _connection.CloseAsync();
            }
        }

        public Task<IDbConnection> GetConnectionAsync()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Database connection is not open");
            }

            return Task.FromResult<IDbConnection>(_connection!);
        }

        public async Task<IDbTransaction> BeginTransactionAsync()
        {
            if (!IsConnected)
            {
                await ConnectAsync();
            }

            return _connection!.BeginTransaction();
        }

        public Task CommitTransactionAsync(IDbTransaction transaction)
        {
            transaction?.Commit();
            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync(IDbTransaction transaction)
        {
            transaction?.Rollback();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _connection?.Dispose();
            }

            _disposed = true;
        }
    }
}
