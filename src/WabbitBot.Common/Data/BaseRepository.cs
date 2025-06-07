using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data
{
    public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : BaseEntity
    {
        protected readonly IDatabaseConnection _connection;
        protected readonly string _tableName;
        protected readonly IEnumerable<string> _columns;
        protected readonly string _idColumn;

        protected BaseRepository(
            IDatabaseConnection connection,
            string tableName,
            IEnumerable<string> columns,
            string idColumn = "Id")
        {
            _connection = connection;
            _tableName = tableName;
            _columns = columns;
            _idColumn = idColumn;
        }

        public virtual async Task<TEntity> GetByIdAsync(object id)
        {
            var sql = QueryUtil.BuildSelectQuery(_tableName, whereClause: $"{_idColumn} = @Id");
            var results = await QueryUtil.QueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                MapEntity,
                new { Id = id }
            );

            return results.FirstOrDefault();
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            var sql = QueryUtil.BuildSelectQuery(_tableName);
            return await QueryUtil.QueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                MapEntity
            );
        }

        public virtual async Task<int> AddAsync(TEntity entity)
        {
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = entity.CreatedAt;

            var sql = QueryUtil.BuildInsertQuery(_tableName, _columns);
            return await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                BuildParameters(entity)
            );
        }

        public virtual async Task<bool> UpdateAsync(TEntity entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;

            var sql = QueryUtil.BuildUpdateQuery(_tableName, _columns, $"{_idColumn} = @{_idColumn}");
            var result = await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                BuildParameters(entity)
            );
            return result > 0;
        }

        public virtual async Task<bool> DeleteAsync(object id)
        {
            var sql = $"DELETE FROM {_tableName} WHERE {_idColumn} = @Id";
            var result = await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new { Id = id }
            );
            return result > 0;
        }

        public virtual async Task<bool> ExistsAsync(object id)
        {
            var sql = $"SELECT COUNT(1) FROM {_tableName} WHERE {_idColumn} = @Id";
            var count = await QueryUtil.ExecuteScalarAsync<int>(
                await _connection.GetConnectionAsync(),
                sql,
                new { Id = id }
            );
            return count > 0;
        }

        public virtual async Task<IEnumerable<TEntity>> QueryAsync(string whereClause, object parameters = null)
        {
            var sql = QueryUtil.BuildSelectQuery(_tableName, whereClause: whereClause);
            return await QueryUtil.QueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                MapEntity,
                parameters
            );
        }

        protected abstract TEntity MapEntity(System.Data.IDataReader reader);
        protected abstract object BuildParameters(TEntity entity);
    }
}
