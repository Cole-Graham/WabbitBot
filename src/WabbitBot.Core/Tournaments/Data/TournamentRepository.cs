using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Common.Models;
using WabbitBot.Core.Tournaments;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Tournaments.Data
{
    public class TournamentRepository : BaseRepository<Tournament>, ITournamentRepository
    {
        private const string TableName = "Tournaments";
        private static readonly string[] ColumnNames = new[]
        {
            "Id", "Name", "Description", "GameSize", "StartDate", "EndDate",
            "Status", "MaxParticipants", "BestOf", "CreatedAt", "UpdatedAt", "Version"
        };

        public TournamentRepository(IDatabaseConnection connection)
            : base(connection, TableName, ColumnNames, "Id")
        {
        }

        public async Task<Tournament?> GetTournamentAsync(string tournamentId)
        {
            return await GetByIdAsync(tournamentId);
        }

        protected override Tournament MapEntity(IDataReader reader)
        {
            return new Tournament
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                GameSize = (GameSize)reader.GetInt32(reader.GetOrdinal("GameSize")),
                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                EndDate = reader.IsDBNull(reader.GetOrdinal("EndDate")) ? null : reader.GetDateTime(reader.GetOrdinal("EndDate")),
                Status = (TournamentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                MaxParticipants = reader.GetInt32(reader.GetOrdinal("MaxParticipants")),
                BestOf = reader.GetInt32(reader.GetOrdinal("BestOf")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                Version = reader.GetInt32(reader.GetOrdinal("Version"))
            };
        }

        protected override object BuildParameters(Tournament entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Name,
                entity.Description,
                GameSize = (int)entity.GameSize,
                entity.StartDate,
                entity.EndDate,
                Status = (int)entity.Status,
                entity.MaxParticipants,
                entity.BestOf,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.Version
            };
        }

        public async Task<IEnumerable<Tournament>> GetTournamentsByStatusAsync(TournamentStatus status)
        {
            const string sql = @"
                SELECT * FROM Tournaments 
                WHERE Status = @Status 
                ORDER BY StartDate DESC";

            return await QueryAsync(sql, new { Status = status });
        }

        public async Task<IEnumerable<Tournament>> GetActiveTournamentsAsync()
        {
            const string sql = @"
                SELECT * FROM Tournaments 
                WHERE Status = @Status 
                ORDER BY StartDate ASC";

            return await QueryAsync(sql, new { Status = TournamentStatus.InProgress });
        }

        public async Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync()
        {
            const string sql = @"
                SELECT * FROM Tournaments 
                WHERE Status = @Status 
                ORDER BY StartDate ASC";

            return await QueryAsync(sql, new { Status = TournamentStatus.Registration });
        }
    }
}