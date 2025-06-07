using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Matches.Data
{
    public class MatchQuery
    {
        public string? TeamId { get; set; }
        public MatchStatus? Status { get; set; }
        public GameSize? GameSize { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Limit { get; set; }
        public int? Skip { get; set; }
        public bool IncludeArchived { get; set; }

        public IQueryable<Match> Apply(IQueryable<Match> query)
        {
            if (TeamId != null)
            {
                query = query.Where(m => m.Team1Id == TeamId || m.Team2Id == TeamId);
            }

            if (Status.HasValue)
            {
                query = query.Where(m => m.Status == Status.Value);
            }

            if (GameSize.HasValue)
            {
                query = query.Where(m => m.GameSize == GameSize.Value);
            }

            if (StartDate.HasValue)
            {
                query = query.Where(m => m.CreatedAt >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= EndDate.Value);
            }

            if (Skip.HasValue)
            {
                query = query.Skip(Skip.Value);
            }

            if (Limit.HasValue)
            {
                query = query.Take(Limit.Value);
            }

            return query;
        }
    }
}
