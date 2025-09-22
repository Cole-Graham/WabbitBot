using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Leaderboards.Data.Interface;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class LeaderboardRepository : JsonRepository<Leaderboard>, ILeaderboardRepository
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "EvenTeamFormat", "Rankings", "InitialRating", "KFactor",
            "CreatedAt", "UpdatedAt"
        };

        public LeaderboardRepository(IDatabaseConnection connection)
            : base(connection, "Leaderboards", Columns)
        {
        }

        protected override Leaderboard CreateEntity()
        {
            return new Leaderboard();
        }

        public async Task<Leaderboard?> GetLeaderboardAsync(string leaderboardId)
        {
            return await GetByIdAsync(leaderboardId);
        }

        public async Task<IEnumerable<Leaderboard>> GetLeaderboardsByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat)
        {
            const string sql = @"
                SELECT * FROM Leaderboards 
                WHERE EvenTeamFormat = @EvenTeamFormat
                ORDER BY UpdatedAt DESC";

            return await QueryAsync(sql, new { EvenTeamFormat = (int)evenTeamFormat });
        }

        public async Task<IEnumerable<LeaderboardItem>> GetTopRankingsAsync(EvenTeamFormat evenTeamFormat, int count = 10)
        {
            const string sql = @"
                SELECT Rankings 
                FROM Leaderboards 
                WHERE EvenTeamFormat = @EvenTeamFormat
                ORDER BY UpdatedAt DESC
                LIMIT 1";

            var leaderboard = (await QueryAsync(sql, new { EvenTeamFormat = (int)evenTeamFormat })).FirstOrDefault();
            if (leaderboard == null || !leaderboard.Rankings.ContainsKey(evenTeamFormat))
            {
                return Enumerable.Empty<LeaderboardItem>();
            }

            return leaderboard.Rankings[evenTeamFormat].Values
                .OrderByDescending(e => e.Rating)
                .Take(count);
        }

        public async Task<IEnumerable<LeaderboardItem>> GetTeamRankingsAsync(string teamId, EvenTeamFormat evenTeamFormat)
        {
            const string sql = @"
                SELECT Rankings 
                FROM Leaderboards 
                WHERE EvenTeamFormat = @EvenTeamFormat
                ORDER BY UpdatedAt DESC
                LIMIT 1";

            var leaderboard = (await QueryAsync(sql, new { EvenTeamFormat = (int)evenTeamFormat })).FirstOrDefault();
            if (leaderboard == null || !leaderboard.Rankings.ContainsKey(evenTeamFormat))
            {
                return Enumerable.Empty<LeaderboardItem>();
            }

            return leaderboard.Rankings[evenTeamFormat].Values
                .Where(e => e.Name == teamId && e.IsTeam)
                .OrderByDescending(e => e.Rating);
        }

        public async Task<IEnumerable<LeaderboardItem>> GetRankingsByTeamIdAsync(string teamId, EvenTeamFormat evenTeamFormat)
        {
            return await GetTeamRankingsAsync(teamId, evenTeamFormat);
        }

        public async Task<IEnumerable<LeaderboardItem>> GetRankingsByDateRangeAsync(DateTime startDate, DateTime endDate, EvenTeamFormat evenTeamFormat)
        {
            const string sql = @"
                SELECT Rankings 
                FROM Leaderboards 
                WHERE EvenTeamFormat = @EvenTeamFormat
                AND UpdatedAt >= @StartDate 
                AND UpdatedAt <= @EndDate
                ORDER BY UpdatedAt DESC";

            var leaderboards = await QueryAsync(sql, new
            {
                EvenTeamFormat = (int)evenTeamFormat,
                StartDate = startDate,
                EndDate = endDate
            });

            var allEntries = new List<LeaderboardItem>();
            foreach (var leaderboard in leaderboards)
            {
                if (leaderboard.Rankings.ContainsKey(evenTeamFormat))
                {
                    allEntries.AddRange(leaderboard.Rankings[evenTeamFormat].Values);
                }
            }

            return allEntries.OrderByDescending(e => e.Rating);
        }

        public async Task<IEnumerable<LeaderboardItem>> GetPlayerRankingsAsync(string playerId, EvenTeamFormat evenTeamFormat)
        {
            const string sql = @"
                SELECT Rankings 
                FROM Leaderboards 
                WHERE EvenTeamFormat = @EvenTeamFormat
                ORDER BY UpdatedAt DESC
                LIMIT 1";

            var leaderboard = (await QueryAsync(sql, new { EvenTeamFormat = (int)evenTeamFormat })).FirstOrDefault();
            if (leaderboard == null || !leaderboard.Rankings.ContainsKey(evenTeamFormat))
            {
                return Enumerable.Empty<LeaderboardItem>();
            }

            return leaderboard.Rankings[evenTeamFormat].Values
                .Where(e => e.Name == playerId && !e.IsTeam)
                .OrderByDescending(e => e.Rating);
        }

        public async Task UpdateRankingsAsync(EvenTeamFormat evenTeamFormat, Dictionary<string, LeaderboardItem> rankings)
        {
            var leaderboard = (await GetLeaderboardsByEvenTeamFormatAsync(evenTeamFormat)).FirstOrDefault();
            if (leaderboard == null)
            {
                leaderboard = new Leaderboard();
                leaderboard.Rankings[evenTeamFormat] = rankings;
                await AddAsync(leaderboard);
            }
            else
            {
                leaderboard.Rankings[evenTeamFormat] = rankings;
                await UpdateAsync(leaderboard);
            }
        }

        public async Task UpdateEntryAsync(EvenTeamFormat evenTeamFormat, LeaderboardItem entry)
        {
            var leaderboard = (await GetLeaderboardsByEvenTeamFormatAsync(evenTeamFormat)).FirstOrDefault();
            if (leaderboard == null)
            {
                leaderboard = new Leaderboard();
                leaderboard.Rankings[evenTeamFormat] = new Dictionary<string, LeaderboardItem> { { entry.Name, entry } };
                await AddAsync(leaderboard);
            }
            else
            {
                if (!leaderboard.Rankings.ContainsKey(evenTeamFormat))
                {
                    leaderboard.Rankings[evenTeamFormat] = new Dictionary<string, LeaderboardItem>();
                }
                leaderboard.Rankings[evenTeamFormat][entry.Name] = entry;
                await UpdateAsync(leaderboard);
            }
        }
    }
}
