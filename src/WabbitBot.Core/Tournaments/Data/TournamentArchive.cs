using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Data.Utilities;
using WabbitBot.Core.Tournaments;
using WabbitBot.Core.Tournaments.Data.Interface;

namespace WabbitBot.Core.Tournaments.Data
{
    public class TournamentArchive : Archive<Tournament>, ITournamentArchive
    {
        private static readonly IEnumerable<string> Columns = new[]
        {
            "Id",
            "Name",
            "Description",
            "StartDate",
            "EndDate",
            "MaxParticipants",
            "BestOf",
            "CreatedAt",
            "UpdatedAt",
            "SchemaVersion",
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
                entity.Id,
                entity.Name,
                entity.Description,
                entity.StartDate,
                entity.EndDate,
                entity.MaxParticipants,
                entity.BestOf,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.SchemaVersion,
                ArchivedAt = DateTime.UtcNow
            };
        }
    }
}