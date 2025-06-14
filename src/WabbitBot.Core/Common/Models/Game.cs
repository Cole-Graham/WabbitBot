using WabbitBot.Common.Models;

namespace WabbitBot.Core.Common.Models
{
    public enum GameSize
    {
        OneVOne,
        TwoVTwo,
        ThreeVThree,
        FourVFour
    }

    public class Game : BaseEntity
    {
        public string MatchId { get; set; } = string.Empty;
        public string MapId { get; set; } = string.Empty;
        public GameSize GameSize { get; set; }
        public List<string> Team1PlayerIds { get; set; } = new();
        public List<string> Team2PlayerIds { get; set; } = new();
        public string? WinnerId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public GameStatus Status { get; set; }
        public int GameNumber { get; set; } // Position in the match (1-based)

        public Game()
        {
            CreatedAt = DateTime.UtcNow;
            StartedAt = DateTime.UtcNow;
            Status = GameStatus.Created;
        }
    }

    public enum GameStatus
    {
        Created,
        InProgress,
        Completed,
        Cancelled,
        Forfeited
    }
}