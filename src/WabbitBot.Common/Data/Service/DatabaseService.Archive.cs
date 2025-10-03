using System;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;

namespace WabbitBot.Common.Data.Service;

public partial class DatabaseService<TEntity> where TEntity : Entity
{
    private IArchiveProvider<TEntity>? _archiveProvider;

    public void UseArchiveProvider(IArchiveProvider<TEntity> archiveProvider)
    {
        _archiveProvider = archiveProvider ?? throw new ArgumentNullException(nameof(archiveProvider));
    }

    protected virtual async Task<Result> ArchiveOnDeleteAsync(TEntity entity, Guid archivedBy, string? reason)
    {
        if (_archiveProvider is null)
        {
            return Result.CreateSuccess();
        }
        try
        {
            await _archiveProvider.SaveSnapshotAsync(entity, archivedBy, reason);
            return Result.CreateSuccess();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Archive snapshot failed: {ex.Message}");
        }
    }
}
