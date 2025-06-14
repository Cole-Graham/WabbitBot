using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Common.Data
{
    public abstract class BaseQuery : IBaseQuery
    {
        protected readonly List<string> _whereClauses = new();
        protected readonly Dictionary<string, object> _parameters = new();
        protected string _orderByClause = string.Empty;
        protected int? _limit;
        protected int? _offset;
        protected readonly string _tableName;

        protected BaseQuery(string tableName)
        {
            _tableName = tableName;
        }

        protected string BuildWhereClause()
        {
            return _whereClauses.Any()
                ? $"WHERE {string.Join(" AND ", _whereClauses)}"
                : string.Empty;
        }

        protected string BuildOrderByClause()
        {
            return _orderByClause;
        }

        protected string BuildLimitOffsetClause()
        {
            var clauses = new List<string>();
            if (_limit.HasValue)
            {
                clauses.Add($"LIMIT {_limit.Value}");
            }
            if (_offset.HasValue)
            {
                clauses.Add($"OFFSET {_offset.Value}");
            }
            return string.Join(" ", clauses);
        }

        public string BuildQuery()
        {
            var clauses = new List<string>
            {
                $"SELECT * FROM {_tableName}",
                BuildWhereClause(),
                BuildOrderByClause(),
                BuildLimitOffsetClause()
            };

            return string.Join(" ", clauses.Where(c => !string.IsNullOrEmpty(c)));
        }

        public Dictionary<string, object> BuildParameters()
        {
            return _parameters;
        }

        protected void AddWhereClause(string clause, object parameters)
        {
            _whereClauses.Add(clause);
            var props = parameters.GetType().GetProperties();
            foreach (var prop in props)
            {
                var value = prop.GetValue(parameters);
                if (value != null)
                {
                    _parameters[prop.Name] = value;
                }
            }
        }

        protected void SetOrderBy(string column, bool descending = true)
        {
            _orderByClause = $"ORDER BY {column} {(descending ? "DESC" : "ASC")}";
        }

        protected void SetLimit(int limit)
        {
            _limit = limit;
        }

        protected void SetOffset(int offset)
        {
            _offset = offset;
        }
    }
}