using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Common.Data
{
    /// <summary>
    /// Base repository class for database operations
    /// </summary>
    public abstract class Repository<TEntity>
        where TEntity : Models.Entity
    {
        protected readonly IDatabaseConnection _connection;
        protected readonly string _tableName;
        protected readonly string[] _columns;
        protected readonly string _idColumn;
        protected readonly string _dateColumn;

        protected Repository(
            IDatabaseConnection connection,
            string tableName,
            string[] columns,
            string idColumn = "Id",
            string dateColumn = "CreatedAt"
        )
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            _columns = columns ?? throw new ArgumentNullException(nameof(columns));
            _idColumn = idColumn ?? throw new ArgumentNullException(nameof(idColumn));
            _dateColumn = dateColumn ?? throw new ArgumentNullException(nameof(dateColumn));
        }

        // Component classes contain ONLY configuration and properties - NO methods
        // All database operations moved to DatabaseService classes

        // Note: JSON operations are now handled natively by EF Core and Npgsql
        // Component classes contain ONLY configuration and properties - NO methods
        // All database operations moved to DatabaseService classes
    }
}
