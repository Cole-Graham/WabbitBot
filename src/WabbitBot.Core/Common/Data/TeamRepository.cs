using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Common.Data
{
    public class TeamRepository : BaseJsonRepository<Team>, ITeamRepository
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Name", "TeamCaptainId", "TeamSize", "MaxRosterSize",
            "Roster", "LastActive", "Tag", "Description", "IsArchived", "ArchivedAt",
            "CreatedAt", "UpdatedAt"
        };

        public TeamRepository(IDatabaseConnection connection)
            : base(connection, "Teams", Columns)
        {
        }

        protected override Team CreateEntity()
        {
            return new Team();
        }

        public async Task<Team?> GetByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            const string sql = "SELECT * FROM Teams WHERE Name = @Name";
            var results = await QueryAsync(sql, new { Name = name });
            return results.FirstOrDefault();
        }

        public async Task<Team?> GetByTagAsync(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentNullException(nameof(tag));
            }

            const string sql = "SELECT * FROM Teams WHERE Tag = @Tag";
            var results = await QueryAsync(sql, new { Tag = tag });
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<Team>> GetTeamsByCaptainAsync(string captainId)
        {
            if (string.IsNullOrEmpty(captainId))
            {
                throw new ArgumentNullException(nameof(captainId));
            }

            const string sql = @"
                SELECT * FROM Teams 
                WHERE TeamCaptainId = @CaptainId 
                ORDER BY LastActive DESC";

            return await QueryAsync(sql, new { CaptainId = captainId });
        }

        public async Task<IEnumerable<Team>> GetTeamsByGameSizeAsync(GameSize gameSize)
        {
            const string sql = @"
                SELECT * FROM Teams 
                WHERE TeamSize = @GameSize 
                ORDER BY LastActive DESC";

            return await QueryAsync(sql, new { GameSize = gameSize });
        }

        public async Task<IEnumerable<Team>> GetInactiveTeamsAsync(TimeSpan inactivityThreshold)
        {
            var cutoffDate = DateTime.UtcNow.Subtract(inactivityThreshold);
            const string sql = @"
                SELECT * FROM Teams 
                WHERE LastActive < @CutoffDate 
                ORDER BY LastActive DESC";

            return await QueryAsync(sql, new { CutoffDate = cutoffDate });
        }

        public async Task UpdateLastActiveAsync(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                throw new ArgumentNullException(nameof(teamId));
            }

            const string sql = @"
                UPDATE Teams 
                SET LastActive = @LastActive 
                WHERE Id = @TeamId";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new { TeamId = teamId, LastActive = DateTime.UtcNow }
            );
        }
    }
}