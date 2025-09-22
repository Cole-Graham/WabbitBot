using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Core.Common.Data.Interface;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data;

/// <summary>
/// Implementation of Game-specific archive operations
/// </summary>
public class GameArchive : Archive<Game>, IGameArchive
{
    private const string TableName = "ArchivedGames";
    private static readonly string[] ColumnNames = new[]
    {
        "Id", "MatchId", "MapId", "EvenTeamFormat", "Team1PlayerIds", "Team2PlayerIds",
        "WinnerId", "StartedAt", "CompletedAt", "Status", "GameNumber",
        "Team1DeckCode", "Team2DeckCode", "Team1DeckSubmittedAt", "Team2DeckSubmittedAt",
        "CreatedAt", "UpdatedAt", "ArchivedAt"
    };

    public GameArchive(IDatabaseConnection connection) : base(connection, TableName, ColumnNames)
    {
    }

    public async Task<IEnumerable<Game>> GetArchivedGamesByMatchAsync(string matchId)
    {
        const string whereClause = "MatchId = @MatchId ORDER BY GameNumber ASC";
        var parameters = new Dictionary<string, object>
        {
            ["MatchId"] = matchId
        };

        return await QueryAsync(whereClause, parameters);
    }

    public async Task<int> ArchiveGamesAsync(IEnumerable<Game> games)
    {
        var gamesList = games.ToList();
        if (!gamesList.Any())
            return 0;

        var totalArchived = 0;
        foreach (var game in gamesList)
        {
            var result = await ArchiveAsync(game);
            if (result > 0)
                totalArchived++;
        }

        return totalArchived;
    }

    protected override Game MapEntity(IDataReader reader)
    {
        var game = new Game
        {
            Id = reader.GetGuid(reader.GetOrdinal("Id")),
            MatchId = reader.GetString(reader.GetOrdinal("MatchId")),
            MapId = reader.GetString(reader.GetOrdinal("MapId")),
            EvenTeamFormat = (EvenTeamFormat)reader.GetInt32(reader.GetOrdinal("EvenTeamFormat")),
            Team1PlayerIds = reader.GetString(reader.GetOrdinal("Team1PlayerIds")).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Team2PlayerIds = reader.GetString(reader.GetOrdinal("Team2PlayerIds")).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            GameNumber = reader.GetInt32(reader.GetOrdinal("GameNumber")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
        };

        // Create state snapshot from database data
        game.CurrentState = new GameStateSnapshot
        {
            GameId = game.Id,
            MatchId = game.MatchId,
            MapId = game.MapId,
            EvenTeamFormat = game.EvenTeamFormat,
            Team1PlayerIds = game.Team1PlayerIds,
            Team2PlayerIds = game.Team2PlayerIds,
            GameNumber = game.GameNumber,
            WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId")) ? null : reader.GetString(reader.GetOrdinal("WinnerId")),
            StartedAt = reader.GetDateTime(reader.GetOrdinal("StartedAt")),
            CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
            Team1DeckCode = reader.IsDBNull(reader.GetOrdinal("Team1DeckCode")) ? null : reader.GetString(reader.GetOrdinal("Team1DeckCode")),
            Team2DeckCode = reader.IsDBNull(reader.GetOrdinal("Team2DeckCode")) ? null : reader.GetString(reader.GetOrdinal("Team2DeckCode")),
            Team1DeckSubmittedAt = reader.IsDBNull(reader.GetOrdinal("Team1DeckSubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("Team1DeckSubmittedAt")),
            Team2DeckSubmittedAt = reader.IsDBNull(reader.GetOrdinal("Team2DeckSubmittedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("Team2DeckSubmittedAt")),
            Timestamp = DateTime.UtcNow
        };

        return game;
    }

    protected override Dictionary<string, object> BuildParameters(Game entity)
    {
        return new Dictionary<string, object>
        {
            ["Id"] = entity.Id,
            ["MatchId"] = entity.MatchId,
            ["MapId"] = entity.MapId,
            ["EvenTeamFormat"] = (int)entity.EvenTeamFormat,
            ["Team1PlayerIds"] = string.Join(",", entity.Team1PlayerIds),
            ["Team2PlayerIds"] = string.Join(",", entity.Team2PlayerIds),
            ["WinnerId"] = entity.WinnerId ?? (object)DBNull.Value,
            ["StartedAt"] = entity.StartedAt,
            ["CompletedAt"] = entity.CompletedAt ?? (object)DBNull.Value,
            ["Status"] = (int)entity.Status,
            ["GameNumber"] = entity.GameNumber,
            ["Team1DeckCode"] = entity.Team1DeckCode ?? (object)DBNull.Value,
            ["Team2DeckCode"] = entity.Team2DeckCode ?? (object)DBNull.Value,
            ["Team1DeckSubmittedAt"] = entity.Team1DeckSubmittedAt ?? (object)DBNull.Value,
            ["Team2DeckSubmittedAt"] = entity.Team2DeckSubmittedAt ?? (object)DBNull.Value,
            ["CreatedAt"] = entity.CreatedAt,
            ["UpdatedAt"] = entity.UpdatedAt,
            ["ArchivedAt"] = DateTime.UtcNow
        };
    }
}
