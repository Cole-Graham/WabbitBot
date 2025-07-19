using System.Data;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Matches.Data
{
    /// <summary>
    /// Implementation of IMatchRepository that provides data access operations for matches.
    /// </summary>
    public class MatchRepository : BaseRepository<Match>, IMatchRepository
    {
        private const string TableName = "Matches";
        private static readonly string[] ColumnNames = new[]
        {
            "Id", "Team1Id", "Team2Id", "Team1PlayerIds", "Team2PlayerIds",
            "GameSize", "CreatedAt", "StartedAt", "CompletedAt", "WinnerId",
            "Status", "Stage", "ParentId", "ParentType", "BestOf", "PlayToCompletion"
        };

        private readonly IGameRepository _gameRepository;

        public MatchRepository(IDatabaseConnection connection, IGameRepository gameRepository)
            : base(connection, TableName, ColumnNames, "Id")
        {
            _gameRepository = gameRepository;
            // Subscribe to player archive check events
            CoreEventBus.Instance.Subscribe<PlayerArchiveCheckEvent>(HandlePlayerArchiveCheck);
        }

        private async Task HandlePlayerArchiveCheck(PlayerArchiveCheckEvent @event)
        {
            // Check for any active matches involving this player
            const string sql = @"
                SELECT COUNT(*) FROM Matches 
                WHERE Status IN (0, 1) -- Created or InProgress
                AND (Team1PlayerIds LIKE @PlayerId OR Team2PlayerIds LIKE @PlayerId)";

            var count = await QueryUtil.ExecuteScalarAsync<int>(
                await _connection.GetConnectionAsync(),
                sql,
                new { PlayerId = $"%{@event.PlayerId}%" }
            );
            @event.HasActiveMatches = count > 0;
        }

        public async Task<IEnumerable<Match>> GetMatchesByStatusAsync(MatchStatus status)
        {
            const string sql = @"
                SELECT * FROM Matches 
                WHERE Status = @Status 
                ORDER BY CreatedAt DESC";

            var matches = await QueryAsync(sql, new { Status = status });
            foreach (var match in matches)
            {
                match.Games = (await _gameRepository.GetGamesByMatchAsync(match.Id.ToString())).ToList();
            }
            return matches;
        }

        public async Task<IEnumerable<Match>> GetMatchesByTeamAsync(string teamId)
        {
            const string sql = @"
                SELECT * FROM Matches 
                WHERE Team1Id = @TeamId OR Team2Id = @TeamId 
                ORDER BY CreatedAt DESC";

            return await QueryAsync(sql, new { TeamId = teamId });
        }

        public async Task<IEnumerable<Match>> GetMatchesByParentAsync(string parentId, string parentType)
        {
            const string sql = @"
                SELECT * FROM Matches 
                WHERE ParentId = @ParentId AND ParentType = @ParentType 
                ORDER BY CreatedAt DESC";

            var matches = await QueryAsync(sql, new { ParentId = parentId, ParentType = parentType });
            foreach (var match in matches)
            {
                match.Games = (await _gameRepository.GetGamesByMatchAsync(match.Id.ToString())).ToList();
            }
            return matches;
        }

        public async Task<IEnumerable<Match>> GetRecentMatchesAsync(int count)
        {
            const string sql = @"
                SELECT * FROM Matches 
                ORDER BY CreatedAt DESC 
                LIMIT @Count";

            var matches = await QueryAsync(sql, new { Count = count });
            foreach (var match in matches)
            {
                match.Games = (await _gameRepository.GetGamesByMatchAsync(match.Id.ToString())).ToList();
            }
            return matches;
        }

        protected override Match MapEntity(IDataReader reader)
        {
            return new Match
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Team1Id = reader.GetString(reader.GetOrdinal("Team1Id")),
                Team2Id = reader.GetString(reader.GetOrdinal("Team2Id")),
                Team1PlayerIds = reader.GetString(reader.GetOrdinal("Team1PlayerIds")).Split(',').ToList(),
                Team2PlayerIds = reader.GetString(reader.GetOrdinal("Team2PlayerIds")).Split(',').ToList(),
                GameSize = (GameSize)reader.GetInt32(reader.GetOrdinal("GameSize")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId")) ? null : reader.GetString(reader.GetOrdinal("WinnerId")),
                Status = (MatchStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                Stage = (MatchStage)reader.GetInt32(reader.GetOrdinal("Stage")),
                ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : reader.GetString(reader.GetOrdinal("ParentId")),
                ParentType = reader.IsDBNull(reader.GetOrdinal("ParentType")) ? null : reader.GetString(reader.GetOrdinal("ParentType")),
                BestOf = reader.GetInt32(reader.GetOrdinal("BestOf"))
            };
        }

        protected override object BuildParameters(Match entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Team1Id,
                entity.Team2Id,
                Team1PlayerIds = string.Join(",", entity.Team1PlayerIds),
                Team2PlayerIds = string.Join(",", entity.Team2PlayerIds),
                GameSize = (int)entity.GameSize,
                entity.CreatedAt,
                entity.StartedAt,
                entity.CompletedAt,
                entity.WinnerId,
                Status = (int)entity.Status,
                Stage = (int)entity.Stage,
                entity.ParentId,
                entity.ParentType,
                entity.BestOf
            };
        }

        public async Task SaveAsync(Match entity)
        {
            using var transaction = await _connection.BeginTransactionAsync();
            try
            {
                if (await ExistsAsync(entity.Id))
                {
                    await UpdateAsync(entity);
                }
                else
                {
                    await AddAsync(entity);
                }

                // Save all games
                foreach (var game in entity.Games)
                {
                    await _gameRepository.SaveAsync(game);
                }

                await _connection.CommitTransactionAsync(transaction);
            }
            catch
            {
                await _connection.RollbackTransactionAsync(transaction);
                throw;
            }
        }
    }
}