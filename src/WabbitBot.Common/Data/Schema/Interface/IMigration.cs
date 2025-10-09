using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;

namespace WabbitBot.Common.Data.Schema.Interface
{
    /// <summary>
    /// Defines the contract for database migrations.
    /// Each migration represents a step in the database schema evolution.
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        /// The order in which this migration should be applied.
        /// This value should correspond to a constant in MigrationOrder.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// A description of what this migration does.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Applies the migration to the database.
        /// </summary>
        Task UpAsync(IDatabaseConnection connection);

        /// <summary>
        /// Reverts the migration from the database.
        /// </summary>
        Task DownAsync(IDatabaseConnection connection);
    }
}
