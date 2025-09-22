using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Leaderboards;
using WabbitBot.Core.Leaderboards.Data.Interface;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class LeaderboardArchive : Archive<Leaderboard>, ILeaderboardArchive
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "EvenTeamFormat", "Rankings", "InitialRating", "KFactor",
            "CreatedAt", "UpdatedAt", "ArchivedAt"
        };

        public LeaderboardArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedLeaderboards", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Leaderboard MapEntity(IDataReader reader)
        {
            var rankings = JsonUtil.Deserialize<Dictionary<EvenTeamFormat, Dictionary<string, LeaderboardItem>>>(
                reader.GetString(reader.GetOrdinal("Rankings"))) ?? new();

            return new Leaderboard
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Rankings = rankings,
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override object BuildParameters(Leaderboard entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                Rankings = JsonUtil.Serialize(entity.Rankings),
                InitialRating = Leaderboard.InitialRating,
                KFactor = Leaderboard.KFactor,
                entity.CreatedAt,
                entity.UpdatedAt,
                ArchivedAt = DateTime.UtcNow
            };
        }

        public async Task<IEnumerable<Leaderboard>> GetByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat)
        {
            const string sql = @"
                SELECT * FROM ArchivedLeaderboards 
                WHERE EvenTeamFormat = @EvenTeamFormat
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { EvenTeamFormat = (int)evenTeamFormat });
        }
    }
}
