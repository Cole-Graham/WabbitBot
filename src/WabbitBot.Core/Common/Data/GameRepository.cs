using System.Data;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Common.Data
{
    public class GameRepository : BaseRepository<Game>, IGameRepository
    {
        private const string TableName = "Games";
        private static readonly string[] ColumnNames = new[]
        {
            "Id", "MatchId", "MapId", "GameSize", "Team1PlayerIds", "Team2PlayerIds",
            "WinnerId", "StartedAt", "CompletedAt", "Status", "GameNumber",
            "CreatedAt", "UpdatedAt"
        };

        public GameRepository(IDatabaseConnection connection)
            : base(connection, TableName, ColumnNames, "Id")
        {
        }

        public async Task<IEnumerable<Game>> GetGamesByMatchAsync(string matchId)
        {
            const string sql = @"
                SELECT * FROM Games 
                WHERE MatchId = @MatchId 
                ORDER BY GameNumber ASC";

            return await QueryAsync(sql, new { MatchId = matchId });
        }

        protected override Game MapEntity(IDataReader reader)
        {
            return new Game
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                MatchId = reader.GetString(reader.GetOrdinal("MatchId")),
                MapId = reader.GetString(reader.GetOrdinal("MapId")),
                GameSize = (GameSize)reader.GetInt32(reader.GetOrdinal("GameSize")),
                Team1PlayerIds = reader.GetString(reader.GetOrdinal("Team1PlayerIds")).Split(',').ToList(),
                Team2PlayerIds = reader.GetString(reader.GetOrdinal("Team2PlayerIds")).Split(',').ToList(),
                WinnerId = reader.IsDBNull(reader.GetOrdinal("WinnerId")) ? null : reader.GetString(reader.GetOrdinal("WinnerId")),
                StartedAt = reader.GetDateTime(reader.GetOrdinal("StartedAt")),
                CompletedAt = reader.IsDBNull(reader.GetOrdinal("CompletedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("CompletedAt")),
                Status = (GameStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                GameNumber = reader.GetInt32(reader.GetOrdinal("GameNumber")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override object BuildParameters(Game entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.MatchId,
                entity.MapId,
                GameSize = (int)entity.GameSize,
                Team1PlayerIds = string.Join(",", entity.Team1PlayerIds),
                Team2PlayerIds = string.Join(",", entity.Team2PlayerIds),
                entity.WinnerId,
                entity.StartedAt,
                entity.CompletedAt,
                Status = (int)entity.Status,
                entity.GameNumber,
                entity.CreatedAt,
                entity.UpdatedAt
            };
        }

        public async Task SaveAsync(Game entity)
        {
            if (await ExistsAsync(entity.Id))
            {
                await UpdateAsync(entity);
            }
            else
            {
                await AddAsync(entity);
            }
        }
    }
}