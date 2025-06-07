using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;

namespace WabbitBot.Core.Tournaments.Data
{
    public class TournamentArchive : BaseArchive<Tournament>
    {
        private static readonly IEnumerable<string> Columns = new[]
        {
            "Id",
            "Name",
            "Description",
            "StartDate",
            "EndDate",
            "Status",
            "MaxParticipants",
            "CreatedAt",
            "UpdatedAt",
            "Version",
            "ArchivedAt"  // Additional column for archive
        };

        public TournamentArchive(IDatabaseConnection connection)
            : base(connection, "ArchivedTournaments", Columns, dateColumn: "ArchivedAt")
        {
        }

        protected override Tournament MapEntity(IDataReader reader)
        {
            return new Tournament
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.GetString(reader.GetOrdinal("Description")),
                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                EndDate = reader.IsDBNull(reader.GetOrdinal("EndDate"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("EndDate")),
                Status = (TournamentStatus)reader.GetInt32(reader.GetOrdinal("Status")),
                MaxParticipants = reader.GetInt32(reader.GetOrdinal("MaxParticipants")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                Version = reader.GetInt32(reader.GetOrdinal("Version"))
            };
        }

        protected override object BuildParameters(Tournament entity)
        {
            return new
            {
                entity.Id,
                entity.Name,
                entity.Description,
                entity.StartDate,
                entity.EndDate,
                Status = (int)entity.Status,
                entity.MaxParticipants,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.Version,
                ArchivedAt = DateTime.UtcNow
            };
        }
    }
}