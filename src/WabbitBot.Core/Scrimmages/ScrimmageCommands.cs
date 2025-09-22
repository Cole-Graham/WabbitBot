using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Scrimmages.Validation;
using WabbitBot.Core.Scrimmages.Data;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data;

namespace WabbitBot.Core.Scrimmages;

/// <summary>
/// Pure business logic for scrimmage commands - no Discord dependencies
/// </summary>
[WabbitCommand("Scrimmage")]
public partial class ScrimmageCommands
{
    #region Private Fields - Clean Architecture

    private static readonly ScrimmageStateMachine _stateMachine = ScrimmageStateMachine.GetInstance();
    private static readonly ScrimmageRepository _scrimmageRepository = new(DatabaseConnectionProvider.GetConnectionAsync().Result, _stateMachine);

    #endregion

    #region Business Logic Methods

    public async Task<ScrimmageResult> ChallengeAsync(string challengerTeamName, string opponentTeamName, EvenTeamFormat evenTeamFormat)
    {
        try
        {
            // Get team information first
            var teamService = new WabbitBot.Core.Common.Services.TeamService();
            var challengerTeam = await teamService.GetByNameAsync(challengerTeamName);
            var opponentTeam = await teamService.GetByNameAsync(opponentTeamName);

            // Perform all validation checks with actual team data
            var validationResult = await ScrimmageCommandsValidation.ValidateScrimmageChallenge(challengerTeamName, opponentTeamName, evenTeamFormat, challengerTeam, opponentTeam);
            if (!validationResult.IsValid)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = validationResult.ErrorMessage
                };
            }

            // Create the scrimmage using the validated team data
            var scrimmage = new Scrimmage
            {
                Id = Guid.NewGuid(),
                Team1Id = challengerTeam!.Id.ToString(),
                Team2Id = opponentTeam!.Id.ToString(),
                EvenTeamFormat = evenTeamFormat,
                Status = ScrimmageStatus.Created,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save the scrimmage using the repository
            var savedScrimmage = await _scrimmageRepository.CreateScrimmageHybridAsync(scrimmage);
            if (savedScrimmage == null)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create scrimmage in database"
                };
            }

            return new ScrimmageResult
            {
                Success = true,
                Message = $"Scrimmage challenge created successfully between {challengerTeamName} and {opponentTeamName}",
                Scrimmage = savedScrimmage,
                ChallengerTeamName = challengerTeamName,
                OpponentTeamName = opponentTeamName
            };
        }
        catch (Exception ex)
        {
            return new ScrimmageResult
            {
                Success = false,
                ErrorMessage = $"Error creating scrimmage challenge: {ex.Message}"
            };
        }
    }


    public async Task<ScrimmageResult> SubmitMapBansAsync(string scrimmageId, string teamId, List<string> mapBans)
    {
        try
        {
            // Get scrimmage using repository's hybrid approach
            var scrimmage = await _scrimmageRepository.GetScrimmageHybridAsync(scrimmageId);
            if (scrimmage == null)
                return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage not found" };

            // Check if scrimmage is in the right state
            if (scrimmage.Status != ScrimmageStatus.InProgress)
                return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage is not in progress" };

            // Get the associated match
            if (scrimmage.Match == null)
                return new ScrimmageResult { Success = false, ErrorMessage = "Match not found" };

            // SubmitMapBans() moved to MatchService - will be handled by ScrimmageService
            // TODO: Update to use MatchService.SubmitMapBansAsync()

            // Persist changes using repository's hybrid approach
            await _scrimmageRepository.UpdateScrimmageHybridAsync(scrimmage);

            return new ScrimmageResult
            {
                Success = true,
                Message = "Map bans submitted successfully"
            };
        }
        catch (Exception ex)
        {
            return new ScrimmageResult
            {
                Success = false,
                ErrorMessage = $"Error submitting map bans: {ex.Message}"
            };
        }
    }

    public async Task<ScrimmageResult> SubmitDeckCodeAsync(string scrimmageId, string teamId, string deckCode)
    {
        try
        {
            // Get scrimmage using repository's hybrid approach
            var scrimmage = await _scrimmageRepository.GetScrimmageHybridAsync(scrimmageId);
            if (scrimmage == null)
                return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage not found" };

            // Check if scrimmage is in the right state
            if (scrimmage.Status != ScrimmageStatus.InProgress)
                return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage is not in progress" };

            // Get the associated match
            if (scrimmage.Match == null)
                return new ScrimmageResult { Success = false, ErrorMessage = "Match not found" };

            // SubmitDeckCode() moved to MatchService - will be handled by ScrimmageService
            // TODO: Update to use MatchService.SubmitDeckCodeAsync()

            // Persist changes using repository's hybrid approach
            await _scrimmageRepository.UpdateScrimmageHybridAsync(scrimmage);

            return new ScrimmageResult
            {
                Success = true,
                Message = "Deck code submitted successfully"
            };
        }
        catch (Exception ex)
        {
            return new ScrimmageResult
            {
                Success = false,
                ErrorMessage = $"Error submitting deck code: {ex.Message}"
            };
        }
    }

    public async Task<ScrimmageResult> ReportGameResultAsync(string scrimmageId, string winnerId)
    {
        try
        {
            // Get scrimmage using repository's hybrid approach
            var scrimmage = await _scrimmageRepository.GetScrimmageHybridAsync(scrimmageId);
            if (scrimmage == null)
                return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage not found" };

            // Check if scrimmage is in the right state
            if (scrimmage.Status != ScrimmageStatus.InProgress)
                return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage is not in progress" };

            // Get the associated match
            if (scrimmage.Match == null)
                return new ScrimmageResult { Success = false, ErrorMessage = "Match not found" };

            // ReportGameResult() moved to MatchService - will be handled by ScrimmageService
            // TODO: Update to use MatchService.ReportGameResultAsync()

            // Check if the match is completed and update scrimmage status if needed
            if (scrimmage.Match.CurrentState == MatchState.Completed)
            {
                // Complete the scrimmage using existing method
                await scrimmage.CompleteAsync(winnerId);
            }

            // Persist changes using repository's hybrid approach
            await _scrimmageRepository.UpdateScrimmageHybridAsync(scrimmage);

            return new ScrimmageResult
            {
                Success = true,
                Message = "Game result reported successfully"
            };
        }
        catch (Exception ex)
        {
            return new ScrimmageResult
            {
                Success = false,
                ErrorMessage = $"Error reporting game result: {ex.Message}"
            };
        }
    }

    #endregion



    #region Result Classes

    public class ScrimmageResult
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public string? ErrorMessage { get; init; }
        public Scrimmage? Scrimmage { get; init; }
        public string? ChallengerTeamName { get; init; }
        public string? OpponentTeamName { get; init; }
    }

    #endregion
}