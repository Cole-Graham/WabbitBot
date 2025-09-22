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
    public class TeamRepository : JsonRepository<Team>, ITeamRepository
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "Name", "TeamCaptainId", "TeamSize", "MaxRosterSize",
            "Roster", "LastActive", "Tag", "IsArchived", "ArchivedAt",
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

        public async Task<IEnumerable<Team>> GetTeamsByMemberAsync(string memberId)
        {
            if (string.IsNullOrEmpty(memberId))
            {
                throw new ArgumentNullException(nameof(memberId));
            }

            // This is a simplified implementation that assumes the roster is stored as JSON
            // In a real implementation, you might have a separate TeamMembers table
            const string sql = @"
                SELECT * FROM Teams 
                WHERE JSON_EXTRACT(Roster, '$[*].PlayerId') LIKE @MemberIdPattern
                AND IsArchived = 0
                ORDER BY LastActive DESC";

            var memberIdPattern = $"%{memberId}%";
            return await QueryAsync(sql, new { MemberIdPattern = memberIdPattern });
        }

        public async Task<IEnumerable<Team>> GetTeamsByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat)
        {
            const string sql = @"
                SELECT * FROM Teams 
                WHERE TeamSize = @EvenTeamFormat 
                ORDER BY LastActive DESC";

            return await QueryAsync(sql, new { EvenTeamFormat = evenTeamFormat });
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

        public async Task<IEnumerable<Team>> SearchTeamsAsync(string searchTerm, int limit = 25)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                // Return most recently active teams if no search term
                const string recentTeamsSql = @"
                    SELECT * FROM Teams 
                    WHERE IsArchived = 0 
                    ORDER BY LastActive DESC 
                    LIMIT @Limit";

                return await QueryAsync(recentTeamsSql, new { Limit = limit });
            }

            // Search by name or tag, prioritizing exact matches and active teams
            const string searchSql = @"
                SELECT * FROM Teams 
                WHERE IsArchived = 0 
                AND (Name LIKE @SearchPattern OR Tag LIKE @SearchPattern)
                ORDER BY 
                    CASE 
                        WHEN Name = @SearchTerm THEN 1
                        WHEN Name LIKE @SearchTermStart THEN 2
                        WHEN Name LIKE @SearchPattern THEN 3
                        ELSE 4
                    END,
                    LastActive DESC
                LIMIT @Limit";

            var searchPattern = $"%{searchTerm}%";
            var searchTermStart = $"{searchTerm}%";

            return await QueryAsync(searchSql, new
            {
                SearchPattern = searchPattern,
                SearchTerm = searchTerm,
                SearchTermStart = searchTermStart,
                Limit = limit
            });
        }

        public async Task<IEnumerable<Team>> SearchTeamsByEvenTeamFormatAsync(string searchTerm, EvenTeamFormat evenTeamFormat, int limit = 25)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                // Return most recently active teams of the specified game size if no search term
                const string recentTeamsSql = @"
                    SELECT * FROM Teams 
                    WHERE IsArchived = 0 
                    AND TeamSize = @EvenTeamFormat
                    ORDER BY LastActive DESC 
                    LIMIT @Limit";

                return await QueryAsync(recentTeamsSql, new { EvenTeamFormat = evenTeamFormat, Limit = limit });
            }

            // Search by name or tag, filtered by game size, prioritizing exact matches and active teams
            const string searchSql = @"
                SELECT * FROM Teams 
                WHERE IsArchived = 0 
                AND TeamSize = @EvenTeamFormat
                AND (Name LIKE @SearchPattern OR Tag LIKE @SearchPattern)
                ORDER BY 
                    CASE 
                        WHEN Name = @SearchTerm THEN 1
                        WHEN Name LIKE @SearchTermStart THEN 2
                        WHEN Name LIKE @SearchPattern THEN 3
                        ELSE 4
                    END,
                    LastActive DESC
                LIMIT @Limit";

            var searchPattern = $"%{searchTerm}%";
            var searchTermStart = $"{searchTerm}%";

            return await QueryAsync(searchSql, new
            {
                EvenTeamFormat = evenTeamFormat,
                SearchPattern = searchPattern,
                SearchTerm = searchTerm,
                SearchTermStart = searchTermStart,
                Limit = limit
            });
        }

        public async Task ArchiveTeamAsync(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                throw new ArgumentNullException(nameof(teamId));
            }

            const string sql = @"
                UPDATE Teams 
                SET IsArchived = 1, ArchivedAt = @ArchivedAt 
                WHERE Id = @TeamId";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new { TeamId = teamId, ArchivedAt = DateTime.UtcNow }
            );
        }

        public async Task UnarchiveTeamAsync(string teamId)
        {
            if (string.IsNullOrEmpty(teamId))
            {
                throw new ArgumentNullException(nameof(teamId));
            }

            const string sql = @"
                UPDATE Teams 
                SET IsArchived = 0, ArchivedAt = NULL 
                WHERE Id = @TeamId";

            await QueryUtil.ExecuteNonQueryAsync(
                await _connection.GetConnectionAsync(),
                sql,
                new { TeamId = teamId }
            );
        }

        public async Task<IEnumerable<Team>> GetArchivedTeamsAsync()
        {
            const string sql = @"
                SELECT * FROM Teams 
                WHERE IsArchived = 1 
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql);
        }

        public async Task<IEnumerable<Team>> GetArchivedTeamsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT * FROM Teams 
                WHERE IsArchived = 1 
                AND ArchivedAt BETWEEN @StartDate AND @EndDate
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
        }
    }
}