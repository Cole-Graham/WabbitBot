using System;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Common.Data
{
    public class TeamArchive : Archive<Team>, ITeamArchive
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Name", "TeamCaptainId", "TeamSize", "MaxRosterSize",
            "Roster", "LastActive", "Tag", "IsArchived", "ArchivedAt",
            "CreatedAt", "UpdatedAt"
        };

        public TeamArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedTeams", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Team MapEntity(IDataReader reader)
        {
            return new Team
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                TeamCaptainId = reader.GetString(reader.GetOrdinal("TeamCaptainId")),
                TeamSize = (EvenTeamFormat)reader.GetInt32(reader.GetOrdinal("TeamSize")),
                MaxRosterSize = reader.GetInt32(reader.GetOrdinal("MaxRosterSize")),
                Roster = JsonUtil.Deserialize<List<TeamMember>>(reader.GetString(reader.GetOrdinal("Roster"))) ?? new(),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                LastActive = reader.GetDateTime(reader.GetOrdinal("LastActive")),
                Tag = reader.IsDBNull(reader.GetOrdinal("Tag")) ? null : reader.GetString(reader.GetOrdinal("Tag")),
                IsArchived = reader.GetBoolean(reader.GetOrdinal("IsArchived")),
                ArchivedAt = reader.IsDBNull(reader.GetOrdinal("ArchivedAt")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ArchivedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override object BuildParameters(Team entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Name,
                entity.TeamCaptainId,
                TeamSize = (int)entity.TeamSize,
                entity.MaxRosterSize,
                Roster = JsonUtil.Serialize(entity.Roster),
                entity.CreatedAt,
                entity.LastActive,
                entity.Tag,
                entity.IsArchived,
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

        public async Task<IEnumerable<Team>> GetArchivedTeamsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await GetByDateRangeAsync(startDate, endDate);
        }

        public async Task<IEnumerable<Team>> GetArchivedTeamsByCaptainIdAsync(string captainId)
        {
            return await GetTeamsByCaptainAsync(captainId);
        }

        public async Task<IEnumerable<Team>> GetArchivedTeamsByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat)
        {
            const string sql = @"
                SELECT * FROM ArchivedTeams 
                WHERE TeamSize = @EvenTeamFormat 
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { EvenTeamFormat = evenTeamFormat });
        }
    }
}