using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.ErrorHandling;

namespace WabbitBot.Core.Common;

/// <summary>
/// Game-specific validation rules
/// </summary>
public static partial class CoreValidation
{
    /// <summary>
    /// Validates game creation parameters
    /// </summary>
    public static Task<Result> ValidateForCreation(string matchId, string mapId, EvenTeamFormat evenTeamFormat,
        List<string> team1PlayerIds, List<string> team2PlayerIds, int gameNumber)
    {
        try
        {
            // Validate match ID
            if (string.IsNullOrWhiteSpace(matchId))
                return Task.FromResult(Result.Failure("Match ID cannot be null or empty"));

            // Validate map ID
            if (string.IsNullOrWhiteSpace(mapId))
                return Task.FromResult(Result.Failure("Map ID cannot be null or empty"));

            // Validate game size
            if (!Game.Validation.IsValidEvenTeamFormat(evenTeamFormat))
                return Task.FromResult(Result.Failure("Invalid game size"));

            // Validate team player IDs
            if (team1PlayerIds == null || !team1PlayerIds.Any())
                return Task.FromResult(Result.Failure("Team 1 player IDs cannot be null or empty"));

            if (team2PlayerIds == null || !team2PlayerIds.Any())
                return Task.FromResult(Result.Failure("Team 2 player IDs cannot be null or empty"));

            // Validate no duplicate players between teams
            var commonPlayers = team1PlayerIds.Intersect(team2PlayerIds).ToList();
            if (commonPlayers.Any())
                return Task.FromResult(Result.Failure($"Players cannot be on both teams: {string.Join(", ", commonPlayers)}"));

            // Validate game number
            if (gameNumber <= 0)
                return Task.FromResult(Result.Failure("Game number must be greater than 0"));

            // Validate team sizes match game size
            var expectedTeamSize = GetExpectedTeamSize(evenTeamFormat);
            if (team1PlayerIds.Count != expectedTeamSize)
                return Task.FromResult(Result.Failure($"Team 1 must have exactly {expectedTeamSize} players for {Game.Helpers.GetEvenTeamFormatDisplay(evenTeamFormat)}"));

            if (team2PlayerIds.Count != expectedTeamSize)
                return Task.FromResult(Result.Failure($"Team 2 must have exactly {expectedTeamSize} players for {Game.Helpers.GetEvenTeamFormatDisplay(evenTeamFormat)}"));

            return Task.FromResult(Result.CreateSuccess());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Validation error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Validates game update parameters
    /// </summary>
    public static Result ValidateForUpdate(Game game)
    {
        try
        {
            if (game == null)
                return Result.Failure("Game cannot be null");

            // Validate basic properties
            if (string.IsNullOrWhiteSpace(game.MatchId))
                return Result.Failure("Match ID cannot be null or empty");

            if (string.IsNullOrWhiteSpace(game.MapId))
                return Result.Failure("Map ID cannot be null or empty");

            if (!Game.Validation.IsValidEvenTeamFormat(game.EvenTeamFormat))
                return Result.Failure("Invalid game size");

            // Validate team player IDs
            if (game.Team1PlayerIds == null || !game.Team1PlayerIds.Any())
                return Result.Failure("Team 1 player IDs cannot be null or empty");

            if (game.Team2PlayerIds == null || !game.Team2PlayerIds.Any())
                return Result.Failure("Team 2 player IDs cannot be null or empty");

            // Validate no duplicate players between teams
            var commonPlayers = game.Team1PlayerIds.Intersect(game.Team2PlayerIds).ToList();
            if (commonPlayers.Any())
                return Result.Failure($"Players cannot be on both teams: {string.Join(", ", commonPlayers)}");

            // Validate team sizes match game size
            var expectedTeamSize = GetExpectedTeamSize(game.EvenTeamFormat);
            if (game.Team1PlayerIds.Count != expectedTeamSize)
                return Result.Failure($"Team 1 must have exactly {expectedTeamSize} players for {Game.Helpers.GetEvenTeamFormatDisplay(game.EvenTeamFormat)}");

            if (game.Team2PlayerIds.Count != expectedTeamSize)
                return Result.Failure($"Team 2 must have exactly {expectedTeamSize} players for {Game.Helpers.GetEvenTeamFormatDisplay(game.EvenTeamFormat)}");

            // Validate game number
            if (game.GameNumber <= 0)
                return Result.Failure("Game number must be greater than 0");

            // Validate status transitions
            if (game.Status == GameStatus.Completed && string.IsNullOrWhiteSpace(game.WinnerId))
                return Result.Failure("Completed games must have a winner");

            if (game.Status == GameStatus.Completed && game.CompletedAt == null)
                return Result.Failure("Completed games must have a completion time");

            return Result.CreateSuccess();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates game archiving parameters
    /// </summary>
    public static Task<Result> ValidateForArchivingAsync(Game game)
    {
        try
        {
            if (game == null)
                return Task.FromResult(Result.Failure("Game cannot be null"));

            // Only allow archiving completed or cancelled games
            if (game.Status != GameStatus.Completed && game.Status != GameStatus.Cancelled)
                return Task.FromResult(Result.Failure("Only completed or cancelled games can be archived"));

            return Task.FromResult(Result.CreateSuccess());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Validation error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets the expected team size for a given game size
    /// </summary>
    private static int GetExpectedTeamSize(EvenTeamFormat evenTeamFormat)
    {
        return evenTeamFormat switch
        {
            EvenTeamFormat.OneVOne => 1,
            EvenTeamFormat.TwoVTwo => 2,
            EvenTeamFormat.ThreeVThree => 3,
            EvenTeamFormat.FourVFour => 4,
            _ => 1
        };
    }
}
