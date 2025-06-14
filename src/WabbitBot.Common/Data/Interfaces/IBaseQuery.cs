using System.Collections.Generic;

namespace WabbitBot.Common.Data.Interfaces
{
    public interface IBaseQuery
    {
        /// <summary>
        /// Builds the complete SQL query string
        /// </summary>
        string BuildQuery();

        /// <summary>
        /// Builds the parameters dictionary for the query
        /// </summary>
        Dictionary<string, object> BuildParameters();
    }
}