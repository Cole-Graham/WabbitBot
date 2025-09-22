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
    public class SeasonArchive : Archive<Season>, ISeasonArchive
    {
        private static readonly string[] Columns = new[]
        {
            "Id", "SeasonGroupId", "EvenTeamFormat", "StartDate", "EndDate", "IsActive",
            "ParticipatingTeams", "Config", "CreatedAt", "UpdatedAt", "ArchivedAt"
        };

        public SeasonArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedSeasons", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Season MapEntity(IDataReader reader)
        {
            var config = JsonUtil.Deserialize<SeasonConfig>(
                reader.GetString(reader.GetOrdinal("Config"))) ?? new();
            var participatingTeams = JsonUtil.Deserialize<Dictionary<string, string>>(
                reader.GetString(reader.GetOrdinal("ParticipatingTeams"))) ?? new();

            return new Season
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                SeasonGroupId = reader.GetString(reader.GetOrdinal("SeasonGroupId")),
                EvenTeamFormat = (EvenTeamFormat)reader.GetInt32(reader.GetOrdinal("EvenTeamFormat")),
                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                ParticipatingTeams = participatingTeams,
                Config = config,
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
            };
        }

        protected override object BuildParameters(Season entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.SeasonGroupId,
                EvenTeamFormat = (int)entity.EvenTeamFormat,
                entity.StartDate,
                entity.EndDate,
                entity.IsActive,
                ParticipatingTeams = JsonUtil.Serialize(entity.ParticipatingTeams),
                Config = JsonUtil.Serialize(entity.Config),
                entity.CreatedAt,
                entity.UpdatedAt,
                ArchivedAt = DateTime.UtcNow
            };
        }

        public override async Task<IEnumerable<Season>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT * FROM ArchivedSeasons 
                WHERE StartDate >= @StartDate 
                AND EndDate <= @EndDate
                ORDER BY ArchivedAt DESC";

            return await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
        }

        public async Task<IEnumerable<Season>> GetByTeamIdAsync(string teamId)
        {
            // TODO: This method needs to be reimplemented since Season entities no longer store team data
            // The team participation data should be stored in a separate table or queried differently
            // For now, return empty collection to avoid breaking existing code
            return await Task.FromResult(Array.Empty<Season>());
        }
    }
}
