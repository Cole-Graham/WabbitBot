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
using WabbitBot.Core.Matches.Data.Interface;

namespace WabbitBot.Core.Matches.Data
{
    /// <summary>
    /// Implementation of IMatchRepository that provides data access operations for matches.
    /// </summary>
    public class MatchRepository : Repository<Match>, IMatchRepository
    {
        private const string TableName = "Matches";
        private static readonly string[] ColumnNames = new[]
        {
            "Id", "Team1Id", "Team2Id", "Team1PlayerIds", "Team2PlayerIds",
            "EvenTeamFormat", "CreatedAt", "StartedAt", "CompletedAt", "WinnerId",
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

        public async Task<IEnumerable<Match>> GetMatchesByStatusAsync<T>() where T : MatchStateSnapshot
        {
            // Since we removed Status from the Match table, we need to filter by state snapshot type
            // This would require a more complex query or we could use the MatchStateMachine
            // For now, return all matches and let the caller filter by state snapshot type
            const string sql = @"
                SELECT * FROM Matches 
                ORDER BY CreatedAt DESC";

            var matches = await QueryAsync(sql);
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
                EvenTeamFormat = (EvenTeamFormat)reader.GetInt32(reader.GetOrdinal("EvenTeamFormat")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                StartedAt = reader.IsDBNull(reader.GetOrdinal("StartedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId")) ? null : reader.GetString(reader.GetOrdinal("WinnerId")),
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
                EvenTeamFormat = (int)entity.EvenTeamFormat,
                entity.CreatedAt,
                entity.StartedAt,
                entity.CompletedAt,
                entity.WinnerId,
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

        public async Task<Match?> GetMatchAsync(string matchId)
        {
            const string sql = "SELECT * FROM Matches WHERE Id = @MatchId";
            var results = await QueryAsync(sql, new { MatchId = matchId });
            var match = results.FirstOrDefault();
            if (match != null)
            {
                match.Games = (await _gameRepository.GetGamesByMatchAsync(match.Id.ToString())).ToList();
            }
            return match;
        }

        public async Task<IEnumerable<Match>> GetMatchesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT * FROM Matches 
                WHERE CreatedAt >= @StartDate 
                AND CreatedAt <= @EndDate 
                ORDER BY CreatedAt DESC";

            var matches = await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
            foreach (var match in matches)
            {
                match.Games = (await _gameRepository.GetGamesByMatchAsync(match.Id.ToString())).ToList();
            }
            return matches;
        }

        public async Task<IEnumerable<Match>> GetActiveMatchesAsync()
        {
            return await GetMatchesByStateAsync(MatchState.InProgress);
        }

        public async Task<IEnumerable<Match>> GetCompletedMatchesAsync()
        {
            return await GetMatchesByStateAsync(MatchState.Completed);
        }

        public async Task<IEnumerable<Match>> GetMatchesByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat)
        {
            const string sql = "SELECT * FROM Matches WHERE EvenTeamFormat = @EvenTeamFormat ORDER BY CreatedAt DESC";
            var matches = await QueryAsync(sql, new { EvenTeamFormat = evenTeamFormat });
            foreach (var match in matches)
            {
                match.Games = (await _gameRepository.GetGamesByMatchAsync(match.Id.ToString())).ToList();
            }
            return matches;
        }

        public async Task<IEnumerable<Match>> GetMatchesByTeamIdAsync(string teamId)
        {
            return await GetMatchesByTeamAsync(teamId);
        }

        public async Task<IEnumerable<Match>> GetMatchesByTournamentIdAsync(string tournamentId)
        {
            return await GetMatchesByParentAsync(tournamentId, "Tournament");
        }

        private async Task<IEnumerable<Match>> GetMatchesByStateAsync(MatchState state)
        {
            // Get all matches and filter by state using the MatchStateSnapshot
            var allMatches = await GetAllAsync();
            return allMatches.Where(match => match.CurrentState == state);
        }
    }
}