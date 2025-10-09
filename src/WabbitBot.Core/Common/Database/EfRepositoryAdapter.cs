using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Database
{
    /// <summary>
    /// EF Core implementation of IRepositoryAdapter using WabbitBotDbContext.
    /// Uses static WabbitBotDbContextProvider for database access (no runtime DI).
    /// </summary>
    public class EfRepositoryAdapter<TEntity> : IRepositoryAdapter<TEntity>
        where TEntity : Entity
    {
        public async Task<TEntity?> GetByIdAsync(object id)
        {
            await using var db = WabbitBotDbContextProvider.CreateDbContext();
            return await db.Set<TEntity>().FindAsync(id);
        }

        public async Task<bool> ExistsAsync(object id)
        {
            await using var db = WabbitBotDbContextProvider.CreateDbContext();
            var entity = await db.Set<TEntity>().FindAsync(id);
            return entity is not null;
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            await using var db = WabbitBotDbContextProvider.CreateDbContext();
            return await db.Set<TEntity>().AsNoTracking().ToListAsync();
        }

        public async Task<Result<TEntity>> CreateAsync(TEntity entity)
        {
            try
            {
                await using var db = WabbitBotDbContextProvider.CreateDbContext();
                await db.Set<TEntity>().AddAsync(entity);
                await db.SaveChangesAsync();
                return Result<TEntity>.CreateSuccess(entity);
            }
            catch (System.Exception ex)
            {
                return Result<TEntity>.Failure($"Create failed: {ex.Message}");
            }
        }

        public async Task<Result<TEntity>> UpdateAsync(TEntity entity)
        {
            try
            {
                await using var db = WabbitBotDbContextProvider.CreateDbContext();
                db.Set<TEntity>().Update(entity);
                await db.SaveChangesAsync();
                return Result<TEntity>.CreateSuccess(entity);
            }
            catch (System.Exception ex)
            {
                return Result<TEntity>.Failure($"Update failed: {ex.Message}");
            }
        }

        public async Task<Result<TEntity>> DeleteAsync(object id)
        {
            try
            {
                await using var db = WabbitBotDbContextProvider.CreateDbContext();
                var set = db.Set<TEntity>();
                var entity = await set.FindAsync(id);
                if (entity is null)
                    return Result<TEntity>.Failure("Entity not found");
                set.Remove(entity);
                await db.SaveChangesAsync();
                return Result<TEntity>.CreateSuccess(entity);
            }
            catch (System.Exception ex)
            {
                return Result<TEntity>.Failure($"Delete failed: {ex.Message}");
            }
        }

        public async Task<TEntity?> GetByNameAsync(string name)
        {
            await using var db = WabbitBotDbContextProvider.CreateDbContext();
            // Use EF.Property to avoid reflection and support entities without a compile-time Name member
            return await db.Set<TEntity>().Where(e => EF.Property<string>(e, "Name") == name).FirstOrDefaultAsync();
        }
    }
}
