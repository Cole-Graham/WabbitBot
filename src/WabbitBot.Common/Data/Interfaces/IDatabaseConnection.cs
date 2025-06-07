using System;
using System.Data;
using System.Threading.Tasks;

namespace WabbitBot.Common.Data.Interfaces
{
    public interface IDatabaseConnection : IDisposable
    {
        bool IsConnected { get; }
        Task<bool> ConnectAsync();
        Task DisconnectAsync();
        Task<IDbConnection> GetConnectionAsync();
        Task<IDbTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync(IDbTransaction transaction);
        Task RollbackTransactionAsync(IDbTransaction transaction);
    }
}
