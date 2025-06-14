using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Models;

namespace WabbitBot.Core.Matches
{
    public class Match : BaseEntity
    {
        public string Team1Id { get; set; } = string.Empty;
        public string Team2Id { get; set; } = string.Empty;
        public List<string> Team1PlayerIds { get; set; } = new();
        public List<string> Team2PlayerIds { get; set; } = new();
        public GameSize GameSize { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? WinnerId { get; set; }
        public MatchStatus Status { get; set; }
        public MatchStage Stage { get; set; }
        public string? ParentId { get; set; } // ID of parent Scrimmage or Tournament
        public string? ParentType { get; set; } // "Scrimmage" or "Tournament"
        public List<Game> Games { get; set; } = new();
        public int BestOf { get; set; } = 1; // Number of games to win the match
        public bool PlayToCompletion { get; set; } // Whether all games must be played even after winner is determined

        public Match()
        {
            CreatedAt = DateTime.UtcNow;
            Status = MatchStatus.Created;
            Stage = MatchStage.MapBan;
        }

        public bool IsTeamMatch => GameSize != GameSize.OneVOne;

        public static Match Create(string team1Id, string team2Id, GameSize gameSize, string? parentId = null, string? parentType = null, int bestOf = 1, bool playToCompletion = false)
        {
            var match = new Match
            {
                Team1Id = team1Id,
                Team2Id = team2Id,
                GameSize = gameSize,
                Status = MatchStatus.Created,
                Stage = MatchStage.MapBan,
                ParentId = parentId,
                ParentType = parentType,
                BestOf = bestOf,
                PlayToCompletion = playToCompletion
            };

            // Event will be published by source generator
            return match;
        }

        public void Start()
        {
            if (Status != MatchStatus.Created)
                throw new InvalidOperationException("Match can only be started when in Created state");

            StartedAt = DateTime.UtcNow;
            Status = MatchStatus.InProgress;

            // Create the first game
            var game = new Game
            {
                MatchId = Id.ToString(),
                GameSize = GameSize,
                Team1PlayerIds = Team1PlayerIds,
                Team2PlayerIds = Team2PlayerIds,
                GameNumber = 1
            };
            Games.Add(game);

            // Event will be published by source generator
        }

        public void Complete(string winnerId)
        {
            if (Status != MatchStatus.InProgress)
                throw new InvalidOperationException("Match can only be completed when in Progress state");

            if (winnerId != Team1Id && winnerId != Team2Id)
                throw new ArgumentException("Winner must be one of the participating teams");

            // If PlayToCompletion is false and we have enough games to determine a winner, complete the match
            if (!PlayToCompletion)
            {
                var team1Wins = Games.Count(g => g.WinnerId == Team1Id);
                var team2Wins = Games.Count(g => g.WinnerId == Team2Id);
                var gamesToWin = (BestOf + 1) / 2; // Ceiling division by 2

                if (team1Wins >= gamesToWin || team2Wins >= gamesToWin)
                {
                    CompletedAt = DateTime.UtcNow;
                    WinnerId = winnerId;
                    Status = MatchStatus.Completed;

                    // Complete the current game
                    var currentGame = Games.Last();
                    currentGame.CompletedAt = DateTime.UtcNow;
                    currentGame.WinnerId = winnerId;
                    currentGame.Status = GameStatus.Completed;

                    // Event will be published by source generator
                    return;
                }
            }

            // If we need to play more games, start the next one
            if (Games.Count < BestOf)
            {
                StartNextGame();
            }
            else
            {
                // All games have been played
                CompletedAt = DateTime.UtcNow;
                WinnerId = winnerId;
                Status = MatchStatus.Completed;

                // Complete the current game
                var currentGame = Games.Last();
                currentGame.CompletedAt = DateTime.UtcNow;
                currentGame.WinnerId = winnerId;
                currentGame.Status = GameStatus.Completed;

                // Event will be published by source generator
            }
        }

        public void Cancel(string reason, string cancelledBy)
        {
            if (Status == MatchStatus.Completed)
                throw new InvalidOperationException("Cannot cancel a completed match");

            Status = MatchStatus.Cancelled;

            // Cancel the current game if any
            var currentGame = Games.LastOrDefault();
            if (currentGame != null)
            {
                currentGame.Status = GameStatus.Cancelled;
            }

            // Event will be published by source generator
        }

        public void Forfeit(string forfeitedTeamId, string reason)
        {
            if (Status != MatchStatus.InProgress)
                throw new InvalidOperationException("Match can only be forfeited when in Progress state");

            if (forfeitedTeamId != Team1Id && forfeitedTeamId != Team2Id)
                throw new ArgumentException("Forfeited team must be one of the participating teams");

            Status = MatchStatus.Forfeited;
            WinnerId = forfeitedTeamId == Team1Id ? Team2Id : Team1Id;

            // Forfeit the current game
            var currentGame = Games.Last();
            currentGame.Status = GameStatus.Forfeited;
            currentGame.WinnerId = WinnerId;
            currentGame.CompletedAt = DateTime.UtcNow;

            // Event will be published by source generator
        }

        public void AddPlayer(string playerId, int teamNumber)
        {
            if (Status != MatchStatus.Created)
                throw new InvalidOperationException("Players can only be added when match is in Created state");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException("Team number must be 1 or 2");

            var teamList = teamNumber == 1 ? Team1PlayerIds : Team2PlayerIds;
            if (!teamList.Contains(playerId))
            {
                teamList.Add(playerId);
                // Event will be published by source generator
            }
        }

        public void RemovePlayer(string playerId, int teamNumber)
        {
            if (Status != MatchStatus.Created)
                throw new InvalidOperationException("Players can only be removed when match is in Created state");

            if (teamNumber != 1 && teamNumber != 2)
                throw new ArgumentException("Team number must be 1 or 2");

            var teamList = teamNumber == 1 ? Team1PlayerIds : Team2PlayerIds;
            if (teamList.Contains(playerId))
            {
                teamList.Remove(playerId);
                // Event will be published by source generator
            }
        }

        public void StartNextGame()
        {
            if (Status != MatchStatus.InProgress)
                throw new InvalidOperationException("Next game can only be started when match is in Progress");

            var currentGame = Games.Last();
            if (currentGame.Status != GameStatus.Completed)
                throw new InvalidOperationException("Current game must be completed before starting next game");

            var game = new Game
            {
                MatchId = Id.ToString(),
                GameSize = GameSize,
                Team1PlayerIds = Team1PlayerIds,
                Team2PlayerIds = Team2PlayerIds,
                GameNumber = Games.Count + 1
            };
            Games.Add(game);

            // Event will be published by source generator
        }
    }

    public enum MatchStatus
    {
        Created,
        InProgress,
        Completed,
        Cancelled,
        Forfeited
    }

    public enum MatchStage
    {
        MapBan,
        DeckSubmission,
        DeckRevision,
        GameResults,
        Completed
    }
}
