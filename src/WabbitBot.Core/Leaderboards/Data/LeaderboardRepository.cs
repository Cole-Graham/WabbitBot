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
    public class LeaderboardRepository : BaseJsonRepository<Leaderboard>, ILeaderboardRepository
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "GameSize", "Rankings", "InitialRating", "KFactor",
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

        public async Task<IEnumerable<Leaderboard>> GetLeaderboardsByGameSizeAsync(GameSize gameSize)
        {
            const string sql = @"
                SELECT * FROM Leaderboards 
                WHERE GameSize = @GameSize
                ORDER BY UpdatedAt DESC";

            return await QueryAsync(sql, new { GameSize = (int)gameSize });
        }

        public async Task<IEnumerable<LeaderboardEntry>> GetTopRankingsAsync(GameSize gameSize, int count = 10)
        {
            const string sql = @"
                SELECT Rankings 
                FROM Leaderboards 
                WHERE GameSize = @GameSize
                ORDER BY UpdatedAt DESC
                LIMIT 1";

            var leaderboard = (await QueryAsync(sql, new { GameSize = (int)gameSize })).FirstOrDefault();
            if (leaderboard == null || !leaderboard.Rankings.ContainsKey(gameSize))
            {
                return Enumerable.Empty<LeaderboardEntry>();
            }

            return leaderboard.Rankings[gameSize].Values
                .OrderByDescending(e => e.Rating)
                .Take(count);
        }

        public async Task<IEnumerable<LeaderboardEntry>> GetTeamRankingsAsync(string teamId, GameSize gameSize)
        {
            const string sql = @"
                SELECT Rankings 
                FROM Leaderboards 
                WHERE GameSize = @GameSize
                ORDER BY UpdatedAt DESC
                LIMIT 1";

            var leaderboard = (await QueryAsync(sql, new { GameSize = (int)gameSize })).FirstOrDefault();
            if (leaderboard == null || !leaderboard.Rankings.ContainsKey(gameSize))
            {
                return Enumerable.Empty<LeaderboardEntry>();
            }

            return leaderboard.Rankings[gameSize].Values
                .Where(e => e.Name == teamId && e.IsTeam)
                .OrderByDescending(e => e.Rating);
        }

        public async Task<IEnumerable<LeaderboardEntry>> GetPlayerRankingsAsync(string playerId, GameSize gameSize)
        {
            const string sql = @"
                SELECT Rankings 
                FROM Leaderboards 
                WHERE GameSize = @GameSize
                ORDER BY UpdatedAt DESC
                LIMIT 1";

            var leaderboard = (await QueryAsync(sql, new { GameSize = (int)gameSize })).FirstOrDefault();
            if (leaderboard == null || !leaderboard.Rankings.ContainsKey(gameSize))
            {
                return Enumerable.Empty<LeaderboardEntry>();
            }

            return leaderboard.Rankings[gameSize].Values
                .Where(e => e.Name == playerId && !e.IsTeam)
                .OrderByDescending(e => e.Rating);
        }

        public async Task UpdateRankingsAsync(GameSize gameSize, Dictionary<string, LeaderboardEntry> rankings)
        {
            var leaderboard = (await GetLeaderboardsByGameSizeAsync(gameSize)).FirstOrDefault();
            if (leaderboard == null)
            {
                leaderboard = new Leaderboard();
                leaderboard.Rankings[gameSize] = rankings;
                await AddAsync(leaderboard);
            }
            else
            {
                leaderboard.Rankings[gameSize] = rankings;
                await UpdateAsync(leaderboard);
            }
        }

        public async Task UpdateEntryAsync(GameSize gameSize, LeaderboardEntry entry)
        {
            var leaderboard = (await GetLeaderboardsByGameSizeAsync(gameSize)).FirstOrDefault();
            if (leaderboard == null)
            {
                leaderboard = new Leaderboard();
                leaderboard.Rankings[gameSize] = new Dictionary<string, LeaderboardEntry> { { entry.Name, entry } };
                await AddAsync(leaderboard);
            }
            else
            {
                if (!leaderboard.Rankings.ContainsKey(gameSize))
                {
                    leaderboard.Rankings[gameSize] = new Dictionary<string, LeaderboardEntry>();
                }
                leaderboard.Rankings[gameSize][entry.Name] = entry;
                await UpdateAsync(leaderboard);
            }
        }
    }
}
