using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Linq;

namespace WabbitBot.Common.Data.Utilities
{
    public static class QueryUtil
    {
        public static async Task<int> ExecuteNonQueryAsync(
            IDbConnection connection,
            string sql,
            object? parameters = null,
            IDbTransaction? transaction = null)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(sql);

            using var command = CreateCommand(connection, sql, parameters, transaction);
            return await Task.Run(() => command.ExecuteNonQuery());
        }

        public static async Task<T> ExecuteScalarAsync<T>(
            IDbConnection connection,
            string sql,
            object? parameters = null,
            IDbTransaction? transaction = null)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(sql);

            using var command = CreateCommand(connection, sql, parameters, transaction);
            var result = await Task.Run(() => command.ExecuteScalar());

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException($"Query returned null or DBNull for scalar type {typeof(T).Name}");
            }

            return (T)Convert.ChangeType(result, typeof(T));
        }

        public static async Task<IEnumerable<T>> QueryAsync<T>(
            IDbConnection connection,
            string sql,
            Func<IDataReader, T> mapper,
            object? parameters = null,
            IDbTransaction? transaction = null)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(sql);
            ArgumentNullException.ThrowIfNull(mapper);

            using var command = CreateCommand(connection, sql, parameters, transaction);
            using var reader = await Task.Run(() => command.ExecuteReader());
            var results = new List<T>();

            while (reader.Read())
            {
                results.Add(mapper(reader));
            }

            return results;
        }

        private static IDbCommand CreateCommand(
            IDbConnection connection,
            string sql,
            object? parameters = null,
            IDbTransaction? transaction = null)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(sql);

            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;

            if (parameters != null)
            {
                foreach (var prop in parameters.GetType().GetProperties())
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@{prop.Name}";
                    parameter.Value = prop.GetValue(parameters) ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }
            }

            return command;
        }

        public static string BuildInsertQuery(string tableName, IEnumerable<string> columns)
        {
            ArgumentNullException.ThrowIfNull(tableName);
            ArgumentNullException.ThrowIfNull(columns);

            var columnList = string.Join(", ", columns);
            var parameterList = string.Join(", ", columns.Select(c => $"@{c}"));
            return $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList})";
        }

        public static string BuildUpdateQuery(string tableName, IEnumerable<string> columns, string whereClause)
        {
            ArgumentNullException.ThrowIfNull(tableName);
            ArgumentNullException.ThrowIfNull(columns);
            ArgumentNullException.ThrowIfNull(whereClause);

            var setClause = string.Join(", ", columns.Select(c => $"{c} = @{c}"));
            return $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";
        }

        public static string BuildSelectQuery(
            string tableName,
            IEnumerable<string>? columns = null,
            string? whereClause = null,
            string? orderByClause = null,
            int? limit = null)
        {
            ArgumentNullException.ThrowIfNull(tableName);

            var columnList = columns != null ? string.Join(", ", columns) : "*";
            var sql = $"SELECT {columnList} FROM {tableName}";

            if (!string.IsNullOrEmpty(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            if (!string.IsNullOrEmpty(orderByClause))
            {
                sql += $" ORDER BY {orderByClause}";
            }

            if (limit.HasValue)
            {
                sql += $" LIMIT {limit.Value}";
            }

            return sql;
        }
    }
}
