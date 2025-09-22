using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data.Interface;

/// <summary>
/// Interface for Game-specific archive operations
/// </summary>
public interface IGameArchive : IArchive<Game>
{
    /// <summary>
    /// Gets archived games by match ID
    /// </summary>
    Task<IEnumerable<Game>> GetArchivedGamesByMatchAsync(string matchId);

    /// <summary>
    /// Archives multiple games at once
    /// </summary>
    Task<int> ArchiveGamesAsync(IEnumerable<Game> games);
}
