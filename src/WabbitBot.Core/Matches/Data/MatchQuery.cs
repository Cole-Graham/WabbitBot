using System;
using System.Collections.Generic;
using System.Linq;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Data;

namespace WabbitBot.Core.Matches.Data
{
    public class MatchQuery : BaseQuery
    {
        public MatchQuery() : base("Matches")
        {
            SetOrderBy("CreatedAt", true);
        }

        public MatchQuery ByTeam(string teamId)
        {
            AddWhereClause("(Team1Id = @TeamId OR Team2Id = @TeamId)",
                new { TeamId = teamId });
            return this;
        }

        public MatchQuery ByStatus(MatchStatus status)
        {
            AddWhereClause("Status = @Status", new { Status = (int)status });
            return this;
        }

        public MatchQuery ByGameSize(GameSize gameSize)
        {
            AddWhereClause("GameSize = @GameSize", new { GameSize = (int)gameSize });
            return this;
        }

        public MatchQuery ByDateRange(DateTime startDate, DateTime endDate)
        {
            AddWhereClause("CreatedAt BETWEEN @StartDate AND @EndDate",
                new { StartDate = startDate, EndDate = endDate });
            return this;
        }

        public MatchQuery ByWinner(string teamId)
        {
            AddWhereClause("WinnerId = @WinnerId", new { WinnerId = teamId });
            return this;
        }

        public MatchQuery ByParent(string parentId, string parentType)
        {
            AddWhereClause("ParentId = @ParentId AND ParentType = @ParentType",
                new { ParentId = parentId, ParentType = parentType });
            return this;
        }

        public MatchQuery ByStage(MatchStage stage)
        {
            AddWhereClause("Stage = @Stage", new { Stage = (int)stage });
            return this;
        }

        public MatchQuery OrderBy(string column, bool descending = true)
        {
            SetOrderBy(column, descending);
            return this;
        }

        public MatchQuery Limit(int limit)
        {
            SetLimit(limit);
            return this;
        }

        public MatchQuery Offset(int offset)
        {
            SetOffset(offset);
            return this;
        }
    }
}
