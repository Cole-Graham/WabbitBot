using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class SeasonQuery : BaseQuery
    {
        public SeasonQuery() : base("Seasons")
        {
        }

        public SeasonQuery ByActive(bool isActive = true)
        {
            AddWhereClause("IsActive = @IsActive", new { IsActive = isActive });
            return this;
        }

        public SeasonQuery ByDateRange(DateTime startDate, DateTime endDate)
        {
            AddWhereClause("StartDate >= @StartDate AND EndDate <= @EndDate",
                new { StartDate = startDate, EndDate = endDate });
            return this;
        }

        public SeasonQuery ByName(string name)
        {
            AddWhereClause("Name LIKE @Name", new { Name = $"%{name}%" });
            return this;
        }

        public SeasonQuery ByTeamId(string teamId)
        {
            AddWhereClause("TeamStats LIKE @TeamIdPattern",
                new { TeamIdPattern = $"%{teamId}%" });
            return this;
        }

        public SeasonQuery OrderByStartDate(bool descending = true)
        {
            SetOrderBy("StartDate", descending);
            return this;
        }

        public SeasonQuery OrderByEndDate(bool descending = true)
        {
            SetOrderBy("EndDate", descending);
            return this;
        }

        public SeasonQuery OrderByName(bool descending = false)
        {
            SetOrderBy("Name", descending);
            return this;
        }

        public SeasonQuery Limit(int limit)
        {
            SetLimit(limit);
            return this;
        }

        public SeasonQuery Offset(int offset)
        {
            SetOffset(offset);
            return this;
        }
    }
}
