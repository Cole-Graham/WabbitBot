using System;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common.Data
{
    public class TeamArchive : BaseArchive<Team>
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Name", "TeamCaptainId", "TeamSize", "MaxRosterSize",
            "Roster", "CreatedAt", "LastActive", "Stats", "Tag", "Description",
            "UpdatedAt", "ArchivedAt"
        };

        public TeamArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedTeams", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Team MapEntity(IDataReader reader)
        {
            return new Team
            {
                Id = reader.GetString(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                TeamCaptainId = reader.GetString(reader.GetOrdinal("TeamCaptainId")),
                TeamSize = (GameSize)reader.GetInt32(reader.GetOrdinal("TeamSize")),
                MaxRosterSize = reader.GetInt32(reader.GetOrdinal("MaxRosterSize")),
                Roster = JsonUtil.Deserialize<List<TeamMember>>(reader.GetString(reader.GetOrdinal("Roster"))) ?? new(),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastActive = reader.GetDateTime(reader.GetOrdinal("LastActive")),
                Stats = JsonUtil.Deserialize<Dictionary<GameSize, TeamStats>>(reader.GetString(reader.GetOrdinal("Stats"))) ?? new(),
                Tag = reader.IsDBNull(reader.GetOrdinal("Tag")) ? null : reader.GetString(reader.GetOrdinal("Tag")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override object BuildParameters(Team entity)
        {
            return new
            {
                entity.Id,
                entity.Name,
                entity.TeamCaptainId,
                TeamSize = (int)entity.TeamSize,
                entity.MaxRosterSize,
                Roster = JsonUtil.Serialize(entity.Roster),
                entity.CreatedAt,
                entity.LastActive,
                Stats = JsonUtil.Serialize(entity.Stats),
                entity.Tag,
                entity.Description,
                entity.UpdatedAt,
                ArchivedAt = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<Team>> GetTeamsByCaptainAsync(string captainId)
        {
            const string sql = @"
                SELECT * FROM ArchivedTeams 
                WHERE TeamCaptainId = @CaptainId 
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { CaptainId = captainId });
        }

        public async Task<IEnumerable<Team>> GetTeamsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await GetByDateRangeAsync(startDate, endDate);
        }
    }
}