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
            try
            {
                return JsonUtil.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize JSON: {ex.Message}", ex);
            }
        }

        protected string SerializeJson<T>(T obj)
        {
            try
            {
                return JsonUtil.Serialize(obj);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to serialize JSON: {ex.Message}", ex);
            }
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
                if (value == null)
                {
                    throw new InvalidOperationException($"Null value for property {prop.Name}");
                }

                try
                {
                    // Handle JSON fields
                    if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    {
                        var jsonString = value.ToString();
                        if (string.IsNullOrEmpty(jsonString))
                        {
                            throw new InvalidOperationException($"Empty JSON string for property {prop.Name}");
                        }
                        value = JsonUtil.Deserialize(jsonString, prop.PropertyType);
                    }
                    // Handle Guid conversion
                    else if (prop.PropertyType == typeof(Guid))
                    {
                        var guidString = value.ToString();
                        if (string.IsNullOrEmpty(guidString))
                        {
                            throw new InvalidOperationException($"Empty GUID string for property {prop.Name}");
                        }
                        value = Guid.Parse(guidString);
                    }

                    prop.SetValue(entity, value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error mapping property {prop.Name}: {ex.Message}", ex);
                }
            }

            return entity;
        }

        protected override object BuildParameters(TEntity entity)
        {
            var parameters = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
            var properties = typeof(TEntity).GetProperties();

            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(entity);
                    if (value == null)
                    {
                        continue;
                    }

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

                    parameters[prop.Name] = value!;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error building parameters for property {prop.Name}: {ex.Message}", ex);
                }
            }

            return parameters;
        }

        protected abstract TEntity CreateEntity();
    }
}