using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data
{
    public abstract class BaseArchive<TEntity> : IBaseArchive<TEntity> where TEntity : BaseEntity
    {
        protected readonly IDatabaseConnection _connection;
        protected readonly string _tableName;
        protected readonly IEnumerable<string> _columns;
        protected readonly string _idColumn;
        protected readonly string _dateColumn;

        protected BaseArchive(
            IDatabaseConnection connection,
            string tableName,
            IEnumerable<string> columns,
            string idColumn = "Id",
            string dateColumn = "CreatedAt")
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            _idColumn = idColumn ?? throw new ArgumentNullException(nameof(idColumn));
            _dateColumn = dateColumn ?? throw new ArgumentNullException(nameof(dateColumn));
        }

        public virtual async Task<TEntity?> GetByIdAsync(object id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var sql = QueryUtil.BuildSelectQuery(_tableName, whereClause: $"{_idColumn} = @Id");
            var results = await QueryUtil.QueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                MapEntity,
                new { Id = id }
            );

            return results.FirstOrDefault();
        }

        public virtual async Task<IEnumerable<TEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var sql = QueryUtil.BuildSelectQuery(
                _tableName,
                whereClause: $"{_dateColumn} BETWEEN @StartDate AND @EndDate",
                orderByClause: $"{_dateColumn} DESC"
            );

            return await QueryUtil.QueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                MapEntity,
                new { StartDate = startDate, EndDate = endDate }
            );
        }

        public virtual async Task<int> ArchiveAsync(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            entity.UpdatedAt = DateTime.UtcNow;

            var sql = QueryUtil.BuildInsertQuery(_tableName, _columns);
            return await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                BuildParameters(entity)
            );
        }

        public virtual async Task<bool> DeleteAsync(object id)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var sql = $"DELETE FROM {_tableName} WHERE {_idColumn} = @Id";
            var result = await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new { Id = id }
            );
            return result > 0;
        }

        public virtual async Task<IEnumerable<TEntity>> QueryAsync(string whereClause, object? parameters = null)
        {
            if (string.IsNullOrEmpty(whereClause))
            {
                throw new ArgumentNullException(nameof(whereClause));
            }

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
