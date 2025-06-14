using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data;

namespace WabbitBot.Core.Scrimmages.Data
{
    public class ScrimmageQuery : BaseQuery
    {
        public ScrimmageQuery() : base("Scrimmages")
        {
            SetOrderBy("CreatedAt", true);
        }

        public ScrimmageQuery ByTeam(string teamId)
        {
            AddWhereClause("(Team1Id = @TeamId OR Team2Id = @TeamId)",
                new { TeamId = teamId });
            return this;
        }

        public ScrimmageQuery ByStatus(ScrimmageStatus status)
        {
            AddWhereClause("Status = @Status", new { Status = (int)status });
            return this;
        }

        public ScrimmageQuery ByGameSize(GameSize gameSize)
        {
            AddWhereClause("GameSize = @GameSize", new { GameSize = (int)gameSize });
            return this;
        }

        public ScrimmageQuery ByDateRange(DateTime startDate, DateTime endDate)
        {
            AddWhereClause("CreatedAt BETWEEN @StartDate AND @EndDate",
                new { StartDate = startDate, EndDate = endDate });
            return this;
        }

        public ScrimmageQuery ByWinner(string teamId)
        {
            AddWhereClause("WinnerId = @WinnerId", new { WinnerId = teamId });
            return this;
        }

        public ScrimmageQuery OrderBy(string column, bool descending = true)
        {
            SetOrderBy(column, descending);
            return this;
        }

        public ScrimmageQuery Limit(int limit)
        {
            SetLimit(limit);
            return this;
        }

        public ScrimmageQuery Offset(int offset)
        {
            SetOffset(offset);
            return this;
        }
    }
}
