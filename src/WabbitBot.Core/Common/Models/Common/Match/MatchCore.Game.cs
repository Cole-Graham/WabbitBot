

namespace WabbitBot.Core.Common.Models
{
    public partial class MatchCore
    {
        public static partial class Factory
        {
            // ------------------------ Factory & Initialization ---------------------------
            public static Game CreateGame(Guid matchId, Guid mapId, TeamSize teamSize, int gameNumber)
            {
                var game = new Game
                {
                    MatchId = matchId,
                    MapId = mapId,
                    TeamSize = teamSize,
                    GameNumber = gameNumber,
                };
                InitializeGameState(game);
                return game;
            }

            private static void InitializeGameState(Game game)
            {
                var initialState = new GameStateSnapshot
                {
                    GameId = game.Id,
                    MatchId = game.MatchId,
                    MapId = game.MapId,
                    TeamSize = game.TeamSize,
                    Team1PlayerIds = game.Team1PlayerIds,
                    Team2PlayerIds = game.Team2PlayerIds,
                    GameNumber = game.GameNumber,
                    Timestamp = DateTime.UtcNow,
                };
                game.StateHistory.Add(initialState);
            }

            public static GameStateSnapshot CreateGameStateSnapshotFromOther(GameStateSnapshot other)
            {
                // Manual copy to avoid reference issues
                return new GameStateSnapshot
                {
                    GameId = other.GameId,
                    Timestamp = DateTime.UtcNow,
                    PlayerId = other.PlayerId,
                    PlayerName = other.PlayerName,
                    AdditionalData = new Dictionary<string, object>(other.AdditionalData),
                    StartedAt = other.StartedAt,
                    CompletedAt = other.CompletedAt,
                    CancelledAt = other.CancelledAt,
                    ForfeitedAt = other.ForfeitedAt,
                    WinnerId = other.WinnerId,
                    CancelledByUserId = other.CancelledByUserId,
                    ForfeitedByUserId = other.ForfeitedByUserId,
                    ForfeitedTeamId = other.ForfeitedTeamId,
                    CancellationReason = other.CancellationReason,
                    ForfeitReason = other.ForfeitReason,
                    Team1DeckCode = other.Team1DeckCode,
                    Team2DeckCode = other.Team2DeckCode,
                    Team1DeckSubmittedAt = other.Team1DeckSubmittedAt,
                    Team2DeckSubmittedAt = other.Team2DeckSubmittedAt,
                    Team1DeckConfirmed = other.Team1DeckConfirmed,
                    Team2DeckConfirmed = other.Team2DeckConfirmed,
                    Team1DeckConfirmedAt = other.Team1DeckConfirmedAt,
                    Team2DeckConfirmedAt = other.Team2DeckConfirmedAt,
                    MatchId = other.MatchId,
                    MapId = other.MapId,
                    TeamSize = other.TeamSize,
                    Team1PlayerIds = new List<Guid>(other.Team1PlayerIds),
                    Team2PlayerIds = new List<Guid>(other.Team2PlayerIds),
                    GameNumber = other.GameNumber,
                };
            }
        }

        public partial class Accessors
        {
            // ------------------------ State & Logic Accessors ----------------------------
            public static GameStateSnapshot GetCurrentSnapshot(Game game)
            {
                return game.StateHistory.LastOrDefault() ?? new GameStateSnapshot();
            }

            public static GameStatus GetCurrentStatus(Game game)
            {
                var snapshot = GetCurrentSnapshot(game);
                if (snapshot.CompletedAt.HasValue) return GameStatus.Completed;
                if (snapshot.CancelledAt.HasValue) return GameStatus.Cancelled;
                if (snapshot.ForfeitedAt.HasValue) return GameStatus.Forfeited;
                if (snapshot.StartedAt.HasValue) return GameStatus.InProgress;
                return GameStatus.Created;
            }

            public static string GetRequiredAction(Game game)
            {
                return GetCurrentStatus(game) switch
                {
                    GameStatus.Created => "Waiting for deck submissions",
                    GameStatus.InProgress => "Game in progress",
                    GameStatus.Completed => "Game completed",
                    GameStatus.Cancelled => "Game cancelled",
                    GameStatus.Forfeited => "Game forfeited",
                    _ => "Unknown state",
                };
            }

            public static bool AreDeckCodesSubmitted(Game game)
            {
                var snapshot = GetCurrentSnapshot(game);
                return !string.IsNullOrEmpty(snapshot.Team1DeckCode) && !string.IsNullOrEmpty(snapshot.Team2DeckCode);
            }

            public static bool AreDeckCodesConfirmed(Game game)
            {
                var snapshot = GetCurrentSnapshot(game);
                return snapshot.Team1DeckConfirmed && snapshot.Team2DeckConfirmed;
            }

            public static bool IsReadyToStart(Game game)
            {
                return AreDeckCodesSubmitted(game) && AreDeckCodesConfirmed(game);
            }
        }

        public partial class State
        {
            public class GameState
            {

                // ------------------------ State Machine Definition ---------------------------
                private static readonly Dictionary<GameStatus, List<GameStatus>> _validTransitions = new()
                {
                    [GameStatus.Created] = new() { GameStatus.InProgress, GameStatus.Cancelled },
                    [GameStatus.InProgress] = new() { GameStatus.Completed, GameStatus.Cancelled, GameStatus.Forfeited },
                    [GameStatus.Completed] = new(), // Terminal state
                    [GameStatus.Cancelled] = new(), // Terminal state
                    [GameStatus.Forfeited] = new(),  // Terminal state
                };

                // ------------------------ State Machine Logic --------------------------------
                public static bool CanTransition(Game game, GameStatus to)
                {
                    var from = Accessors.GetCurrentStatus(game);
                    return _validTransitions.ContainsKey(from) && _validTransitions[from].Contains(to);
                }

                public static List<GameStatus> GetValidTransitions(Game game)
                {
                    var from = Accessors.GetCurrentStatus(game);
                    return _validTransitions.ContainsKey(from)
                        ? new List<GameStatus>(_validTransitions[from])
                        : new List<GameStatus>();
                }

                public static bool TryTransition(Game game, GameStatus toState, Guid userId, string playerName, string? reason = null)
                {
                    if (!CanTransition(game, toState))
                        return false;

                    var currentSnapshot = Accessors.GetCurrentSnapshot(game);
                    var newSnapshot = Factory.CreateGameStateSnapshotFromOther(currentSnapshot);

                    switch (toState)
                    {
                        case GameStatus.InProgress:
                            newSnapshot.StartedAt = DateTime.UtcNow;
                            break;
                        case GameStatus.Completed:
                            newSnapshot.CompletedAt = DateTime.UtcNow;
                            break;
                        case GameStatus.Cancelled:
                            newSnapshot.CancelledAt = DateTime.UtcNow;
                            newSnapshot.CancelledByUserId = userId;
                            newSnapshot.CancellationReason = reason;
                            break;
                        case GameStatus.Forfeited:
                            newSnapshot.ForfeitedAt = DateTime.UtcNow;
                            newSnapshot.ForfeitedByUserId = userId;
                            newSnapshot.ForfeitReason = reason;
                            break;
                    }

                    newSnapshot.PlayerId = userId;
                    newSnapshot.PlayerName = playerName;
                    game.StateHistory.Add(newSnapshot);

                    return true;
                }
            }
        }

        public partial class Validation
        {
            // ------------------------ Validation Logic -----------------------------------
            // No validation needed for TeamSize as it's a simple enum
            // Future: Add business rule validations here
        }
    }
}
