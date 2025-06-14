using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data;

namespace WabbitBot.Core.Leaderboards.Data
{
    public class LeaderboardQuery : BaseQuery
    {
        public LeaderboardQuery() : base("Leaderboards")
        {
        }

        public LeaderboardQuery ByGameSize(GameSize gameSize)
        {
            AddWhereClause("GameSize = @GameSize", new { GameSize = (int)gameSize });
            return this;
        }

        public LeaderboardQuery ByDateRange(DateTime startDate, DateTime endDate)
        {
            AddWhereClause("CreatedAt >= @StartDate AND CreatedAt <= @EndDate",
                new { StartDate = startDate, EndDate = endDate });
            return this;
        }

        public LeaderboardQuery OrderByCreatedAt(bool descending = true)
        {
            SetOrderBy("CreatedAt", descending);
            return this;
        }

        public LeaderboardQuery OrderByUpdatedAt(bool descending = true)
        {
            SetOrderBy("UpdatedAt", descending);
            return this;
        }

        public LeaderboardQuery Limit(int limit)
        {
            SetLimit(limit);
            return this;
        }

        public LeaderboardQuery Offset(int offset)
        {
            SetOffset(offset);
            return this;
        }
    }
}
