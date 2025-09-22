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
using WabbitBot.Core.Tournaments.Data.Interface;

namespace WabbitBot.Core.Tournaments.Data
{
    public class TournamentRepository : Repository<Tournament>, ITournamentRepository
    {
        private const string TableName = "Tournaments";
        private static readonly string[] ColumnNames = new[]
        {
            "Id", "Name", "Description", "EvenTeamFormat", "StartDate", "EndDate",
            "MaxParticipants", "BestOf", "CreatedAt", "UpdatedAt", "SchemaVersion"
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
                EvenTeamFormat = (EvenTeamFormat)reader.GetInt32(reader.GetOrdinal("EvenTeamFormat")),
                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                EndDate = reader.IsDBNull(reader.GetOrdinal("EndDate")) ? null : reader.GetDateTime(reader.GetOrdinal("EndDate")),
                MaxParticipants = reader.GetInt32(reader.GetOrdinal("MaxParticipants")),
                BestOf = reader.GetInt32(reader.GetOrdinal("BestOf")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                SchemaVersion = reader.GetInt32(reader.GetOrdinal("SchemaVersion"))
            };
        }

        protected override object BuildParameters(Tournament entity)
        {
            return new
            {
                Id = entity.Id.ToString(),
                entity.Name,
                entity.Description,
                EvenTeamFormat = (int)entity.EvenTeamFormat,
                entity.StartDate,
                entity.EndDate,
                entity.MaxParticipants,
                entity.BestOf,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.SchemaVersion
            };
        }

        public async Task<IEnumerable<Tournament>> GetTournamentsByStatusAsync<T>() where T : TournamentStateSnapshot
        {
            // Note: This method would need to be implemented with a TournamentService
            // that can filter tournaments based on their current state snapshot type
            // For now, return all tournaments and let the service layer filter by state
            return await GetAllAsync();
        }

        public async Task<IEnumerable<Tournament>> GetActiveTournamentsAsync()
        {
            // Note: This would need to filter tournaments with TournamentInProgress state snapshots
            // For now, return all tournaments and let the service layer filter by state
            return await GetAllAsync();
        }

        public async Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync()
        {
            // Note: This would need to filter tournaments with TournamentRegistration state snapshots
            // For now, return all tournaments and let the service layer filter by state
            return await GetAllAsync();
        }


        public async Task<IEnumerable<Tournament>> GetTournamentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            const string sql = @"
                SELECT * FROM Tournaments 
                WHERE StartDate >= @StartDate 
                AND EndDate <= @EndDate 
                ORDER BY StartDate DESC";

            return await QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
        }

        public async Task<IEnumerable<Tournament>> GetCompletedTournamentsAsync()
        {
            // Note: This would need to filter tournaments with TournamentCompleted state snapshots
            // For now, return all tournaments and let the service layer filter by state
            return await GetAllAsync();
        }

        public async Task<IEnumerable<Tournament>> GetTournamentsByEvenTeamFormatAsync(EvenTeamFormat evenTeamFormat)
        {
            const string sql = "SELECT * FROM Tournaments WHERE EvenTeamFormat = @EvenTeamFormat ORDER BY StartDate DESC";
            return await QueryAsync(sql, new { EvenTeamFormat = evenTeamFormat });
        }
    }
}