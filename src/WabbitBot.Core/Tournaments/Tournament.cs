using System;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Tournaments
{
    public enum TournamentStatus
    {
        Created,
        Registration,
        InProgress,
        Completed,
        Cancelled
    }

    public class Tournament : VersionedEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GameSize GameSize { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TournamentStatus Status { get; set; }
        public int MaxParticipants { get; set; }
        public int BestOf { get; set; } = 1; // Number of games to win each match

        public Tournament()
        {
            CreatedAt = DateTime.UtcNow;
            Status = TournamentStatus.Created;
        }

        public Tournament Clone()
        {
            return new Tournament
            {
                Id = Id,
                Name = Name,
                Description = Description,
                GameSize = GameSize,
                StartDate = StartDate,
                EndDate = EndDate,
                Status = Status,
                MaxParticipants = MaxParticipants,
                BestOf = BestOf,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Version = Version
            };
        }
    }
}