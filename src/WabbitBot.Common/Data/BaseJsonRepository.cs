using System;
using System.Collections.Generic;
using System.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data
{
    public abstract class BaseJsonRepository<TEntity> : BaseRepository<TEntity> where TEntity : BaseEntity
    {
        protected BaseJsonRepository(
            IDatabaseConnection connection,
            string tableName,
            string[] columns,
            string idColumn = "Id")
            : base(connection, tableName, columns, idColumn)
        {
        }

        protected T DeserializeJson<T>(string json)
        {
            return JsonUtil.Deserialize<T>(json);
        }

        protected string SerializeJson<T>(T obj)
        {
            return JsonUtil.Serialize(obj);
        }

        protected override TEntity MapEntity(IDataReader reader)
        {
            var entity = CreateEntity();
            var properties = typeof(TEntity).GetProperties();

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

                prop.SetValue(entity, value);
            }

            return entity;
        }

        protected override object BuildParameters(TEntity entity)
        {
            var parameters = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
            var properties = typeof(TEntity).GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(entity);

                // Handle JSON fields
                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    value = SerializeJson(value);
                }

                parameters[prop.Name] = value;
            }

            return parameters;
        }

        protected abstract TEntity CreateEntity();
    }
}