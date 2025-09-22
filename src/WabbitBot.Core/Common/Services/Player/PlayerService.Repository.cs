using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Repository operations for PlayerService
/// Contains direct database operations and Player-specific repository methods
/// </summary>
public partial class PlayerService
{
    #region Player-Specific Repository Operations

    /// <summary>
    /// Gets a player by name using repository directly (bypasses cache)
    /// </summary>
    public async Task<Player?> GetByNameFromRepositoryAsync(string playerName)
    {
        return await PlayerRepository.GetByNameAsync(playerName);
    }

    /// <summary>
    /// Gets inactive players from repository
    /// </summary>
    public async Task<IEnumerable<Player>> GetInactivePlayersAsync(TimeSpan inactivityThreshold)
    {
        return await PlayerRepository.GetInactivePlayersAsync(inactivityThreshold);
    }

    /// <summary>
    /// Gets players by team ID from repository
    /// </summary>
    public async Task<IEnumerable<Player>> GetPlayersByTeamIdAsync(string teamId)
    {
        return await PlayerRepository.GetPlayersByTeamIdAsync(teamId);
    }

    /// <summary>
    /// Gets archived players from repository
    /// </summary>
    public async Task<IEnumerable<Player>> GetArchivedPlayersAsync()
    {
        return await PlayerRepository.GetArchivedPlayersAsync();
    }

    /// <summary>
    /// Gets archived players by date range from repository
    /// </summary>
    public async Task<IEnumerable<Player>> GetArchivedPlayersByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await PlayerRepository.GetArchivedPlayersByDateRangeAsync(startDate, endDate);
    }

    /// <summary>
    /// Gets player with user details from repository
    /// </summary>
    public async Task<PlayerWithUserDetails?> GetPlayerWithUserDetailsAsync(string playerId)
    {
        return await PlayerRepository.GetPlayerWithUserDetailsAsync(playerId);
    }

    /// <summary>
    /// Gets players with user details by team ID from repository
    /// </summary>
    public async Task<IEnumerable<PlayerWithUserDetails>> GetPlayersWithUserDetailsByTeamIdAsync(string teamId)
    {
        return await PlayerRepository.GetPlayersWithUserDetailsByTeamIdAsync(teamId);
    }

    /// <summary>
    /// Updates last active timestamp in repository
    /// </summary>
    public async Task UpdateLastActiveAsync(string playerId)
    {
        await PlayerRepository.UpdateLastActiveAsync(playerId);
    }

    /// <summary>
    /// Archives a player in repository
    /// </summary>
    public async Task ArchivePlayerAsync(string playerId)
    {
        await PlayerRepository.ArchivePlayerAsync(playerId);
    }

    /// <summary>
    /// Unarchives a player in repository
    /// </summary>
    public async Task UnarchivePlayerAsync(string playerId)
    {
        await PlayerRepository.UnarchivePlayerAsync(playerId);
    }

    /// <summary>
    /// Executes custom query against player repository
    /// </summary>
    public async Task<IEnumerable<Player>> QueryRepositoryAsync(string sql, object? parameters = null)
    {
        return await PlayerRepository.QueryAsync(sql, parameters);
    }

    #endregion
}