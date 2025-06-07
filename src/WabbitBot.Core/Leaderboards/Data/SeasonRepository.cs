using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Leaderboards;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class SeasonRepository : BaseJsonRepository<Season>
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Name", "StartDate", "EndDate", "IsActive",
            "TeamStats", "Config", "CreatedAt", "UpdatedAt"
        };

        public SeasonRepository(IDatabaseConnection connection)
            : base(connection, "Seasons", Columns)
        {
        }

        protected override Season CreateEntity()
        {
            return new Season();
        }

        public async Task<Season> GetActiveSeasonAsync()
        {
            const string sql = "SELECT * FROM Seasons WHERE IsActive = 1";
            var results = await QueryAsync(sql);
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Season>> GetSeasonsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT * FROM Seasons 
                WHERE StartDate >= @StartDate 
                AND EndDate <= @EndDate";
            return await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
        }

        protected override Season MapEntity(IDataReader reader)
        {
            var entity = CreateEntity();
            var properties = typeof(Season).GetProperties();

            foreach (var prop in properties)
            {
                var columnName = prop.Name;
                var ordinal = reader.GetOrdinal(columnName);

                if (reader.IsDBNull(ordinal))
                {
                    continue;
                }

                var value = reader.GetValue(ordinal);

                // Handle JSON fields
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    value = JsonUtil.Deserialize(value.ToString(), prop.PropertyType);
                }
                // Handle Guid conversion
                else if (prop.PropertyType == typeof(Guid))
                {
                    value = Guid.Parse(value.ToString());
                }

                prop.SetValue(entity, value);
            }

            return entity;
        }

        protected override object BuildParameters(Season entity)
        {
            var parameters = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
            var properties = typeof(Season).GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(entity);

                // Handle JSON fields
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    value = JsonUtil.Serialize(value);
                }
                // Handle Guid conversion
                else if (prop.PropertyType == typeof(Guid))
                {
                    value = value.ToString();
                }

                parameters[prop.Name] = value;
            }

            return parameters;
        }
    }
}