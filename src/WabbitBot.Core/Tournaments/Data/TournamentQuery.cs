using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Core.Tournaments;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data;

namespace WabbitBot.Core.Tournaments.Data
{
    public class TournamentQuery : BaseQuery
    {
        public TournamentQuery() : base("Tournaments")
        {
            SetOrderBy("StartDate", true);
        }

        public TournamentQuery ByStatus(TournamentStatus status)
        {
            AddWhereClause("Status = @Status", new { Status = (int)status });
            return this;
        }

        public TournamentQuery ByGameSize(GameSize gameSize)
        {
            AddWhereClause("GameSize = @GameSize", new { GameSize = (int)gameSize });
            return this;
        }

        public TournamentQuery ByDateRange(DateTime startDate, DateTime endDate)
        {
            AddWhereClause("StartDate BETWEEN @StartDate AND @EndDate",
                new { StartDate = startDate, EndDate = endDate });
            return this;
        }

        public TournamentQuery ByMaxParticipants(int maxParticipants)
        {
            AddWhereClause("MaxParticipants <= @MaxParticipants",
                new { MaxParticipants = maxParticipants });
            return this;
        }

        public TournamentQuery ByName(string name)
        {
            AddWhereClause("Name LIKE @Name", new { Name = $"%{name}%" });
            return this;
        }

        public TournamentQuery ByDescription(string description)
        {
            AddWhereClause("Description LIKE @Description",
                new { Description = $"%{description}%" });
            return this;
        }

        public TournamentQuery OrderBy(string column, bool descending = true)
        {
            SetOrderBy(column, descending);
            return this;
        }

        public TournamentQuery Limit(int limit)
        {
            SetLimit(limit);
            return this;
        }

        public TournamentQuery Offset(int offset)
        {
            SetOffset(offset);
            return this;
        }
    }
}
