using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// EF Core-based archive provider. Persists immutable snapshots to the generated {Entity}Archive tables.
    /// Uses WabbitBotDbContextProvider for database access (no runtime DI).
    /// </summary>
    public sealed class EfArchiveProvider<TEntity> : IArchiveProvider<TEntity> where TEntity : Entity
    {
        private static Type ResolveArchiveType()
        {
            var entityType = typeof(TEntity);
            var ns = entityType.Namespace ?? "";
            var archiveName = entityType.Name + "Archive";
            var fullName = string.IsNullOrEmpty(ns) ? archiveName : ns + "." + archiveName;
            var archiveType = entityType.Assembly.GetType(fullName);
            if (archiveType is null)
            {
                // Fallback: scan assembly
                archiveType = entityType.Assembly.GetTypes()
                    .FirstOrDefault(t => string.Equals(t.Namespace, ns, StringComparison.Ordinal)
                        && string.Equals(t.Name, archiveName, StringComparison.Ordinal));
            }
            return archiveType ?? throw new InvalidOperationException($"Archive type not found for {entityType.FullName}");
        }

        private static void CopyScalarProperties(object destination, TEntity source)
        {
            var destType = destination.GetType();
            var srcType = typeof(TEntity);
            foreach (var dp in destType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!dp.CanWrite) continue;
                var name = dp.Name;
                if (name is "ArchiveId" or "EntityId" or "Version" or "ArchivedAt" or "ArchivedBy" or "Reason" or "Id" or "CreatedAt" or "UpdatedAt" or "Domain")
                    continue;
                var sp = srcType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (sp is null || !sp.CanRead) continue;
                var value = sp.GetValue(source);
                dp.SetValue(destination, value);
            }
        }

        public async Task SaveSnapshotAsync(TEntity entity, Guid archivedBy, string? reason)
        {
            var archiveType = ResolveArchiveType();
            await using var db = WabbitBotDbContextProvider.CreateDbContext();

            var archive = Activator.CreateInstance(archiveType)!;
            // Required metadata
            archiveType.GetProperty("ArchiveId")!.SetValue(archive, Guid.NewGuid());
            archiveType.GetProperty("EntityId")!.SetValue(archive, entity.Id);
            // Monotonic version using seconds since epoch (simple, collision-resistant for our usage)
            var nextVersion = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            archiveType.GetProperty("Version")!.SetValue(archive, nextVersion);
            archiveType.GetProperty("ArchivedAt")!.SetValue(archive, DateTime.UtcNow);
            archiveType.GetProperty("ArchivedBy")!.SetValue(archive, archivedBy);
            archiveType.GetProperty("Reason")!.SetValue(archive, reason);

            // Prefer generated mapper if available, otherwise fallback to reflection
            var mapperType = Type.GetType($"WabbitBot.Core.Common.Database.Mappers.{typeof(TEntity).Name}ArchiveMapper, {typeof(TEntity).Assembly.GetName().Name}");
            var mapMethod = mapperType?.GetMethod("MapToArchive", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (mapperType is not null && mapMethod is not null)
            {
                mapMethod.Invoke(null, new object[] { entity, archive });
            }
            else
            {
                CopyScalarProperties(archive, entity);
            }

            // Persist
            db.Add(archive);
            await db.SaveChangesAsync();
        }

        public async Task<IEnumerable<TEntity>> GetHistoryAsync(Guid entityId, int? limit = null)
        {
            // Not implemented: archive rows contain archive model, not TEntity; caller should consume archive models.
            // For now, return empty history of live entities.
            await Task.CompletedTask;
            return Array.Empty<TEntity>();
        }

        public Task<TEntity?> GetLatestAsync(Guid entityId)
        {
            // Not implemented: see note in GetHistoryAsync
            return Task.FromResult<TEntity?>(null);
        }

        public Task RestoreAsync(TEntity snapshot)
        {
            // To be implemented: map archive back to live entity and upsert
            return Task.CompletedTask;
        }

        public Task PurgeAsync(Guid entityId, DateTime? olderThan = null)
        {
            return CoreService.TryWithDbContext(async db =>
            {
                var archiveType = ResolveArchiveType();
                var entityIdProp = archiveType.GetProperty("EntityId")!;
                var archivedAtProp = archiveType.GetProperty("ArchivedAt")!;

                var setGeneric = typeof(Microsoft.EntityFrameworkCore.DbContext)
                    .GetMethods()
                    .First(m => m.Name == "Set" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
                    .MakeGenericMethod(archiveType)
                    .Invoke(db, null)!;

                var list = new List<object>();
                foreach (var item in (System.Collections.IEnumerable)setGeneric)
                {
                    list.Add(item);
                }

                var filtered = list.AsQueryable().Cast<object>();
                if (entityId != Guid.Empty)
                {
                    filtered = filtered.Where(x => (Guid)entityIdProp.GetValue(x)! == entityId);
                }
                if (olderThan is not null)
                {
                    filtered = filtered.Where(x => (DateTime)archivedAtProp.GetValue(x)! < olderThan.Value);
                }

                var toDelete = filtered.ToList();
                if (toDelete.Count == 0) return;
                foreach (var r in toDelete)
                {
                    db.Remove(r);
                }
                await db.SaveChangesAsync();
            }, "Archive Purge");
        }
    }
}


