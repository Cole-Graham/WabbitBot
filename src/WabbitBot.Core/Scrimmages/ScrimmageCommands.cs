using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data;
using WabbitBot.Common.Data.Service;
using WabbitBot.Common.Data.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Scrimmages;

/// <summary>
/// Pure business logic for scrimmage commands - no Discord dependencies
/// </summary>
[WabbitCommand("Scrimmage")]
public partial class ScrimmageCommands
{
    private readonly ICoreEventBus _eventBus;

    #region Private Fields - Clean Architecture

    private static readonly DatabaseService<Scrimmage> _scrimmageData = new();
    private static readonly DatabaseService<Team> _teamData = new();

    #endregion

    #region Business Logic Methods

    public class ScrimmageChallengeRequest
    {
        public string ChallengerTeamName { get; set; } = string.Empty;
        public string OpponentTeamName { get; set; } = string.Empty;
        public TeamSize TeamSize { get; set; }
        // Teams fetched via Npgsql earlier—passed in for validation (or fetch inside async rule)
        public Team? ChallengerTeam { get; set; }
        public Team? OpponentTeam { get; set; }
    }

    public class ScrimmageChallengeValidator : AbstractValidator<ScrimmageChallengeRequest>
    {
        public ScrimmageChallengeValidator()
        {
            // Rule for team names: Reuse your CoreValidation via custom validator
            // RuleFor(x => x.ChallengerTeamName)
            //     .NotEmpty().WithMessage("Challenger team name is required")
            //.Must(BeValidTeamName).WithMessage("Invalid challenger team name");  // Wrap CoreValidation.ValidateString

            // RuleFor(x => x.OpponentTeamName)
            //     .NotEmpty().WithMessage("Opponent team name is required")
            //     .Must(BeValidTeamName).WithMessage("Invalid opponent team name");

            // Cross-property: Not self-challenge
            RuleFor(x => x)
                .Must(NotBeSelfChallenge)
                .WithMessage("A team cannot challenge itself")
                .WithName("SelfChallenge");  // Groups error under one key if needed

            // Async rule: Teams exist (injects your repo/service for Npgsql fetch if needed)
            RuleFor(x => x.ChallengerTeam)
                .NotNull()
                .WithMessage(x => $"Challenger team '{x.ChallengerTeamName}' not found");

            RuleFor(x => x.OpponentTeam)
                .NotNull()
                .WithMessage(x => $"Opponent team '{x.OpponentTeamName}' not found");

            // Game sizes: Cross-validation with TeamSize
            RuleFor(x => x)
                .Must(TeamsMatchTeamSize)
                .WithMessage(x =>
                {
                    // TODO: Dynamic message based on mismatch
                    return string.Empty;
                })
                .When(x => x.ChallengerTeam != null && x.OpponentTeam != null);  // Only if teams loaded
        }

        // Custom validators (extract your existing logic here—keeps it procedural if you like)
        //private static bool BeValidTeamName(string name) => CoreValidation.ValidateString(
        //    name, "TeamName", required: true).Success;
        //}

        private static bool NotBeSelfChallenge(
            ScrimmageChallengeRequest request) => request.ChallengerTeamName != request.OpponentTeamName;

        private static bool TeamsMatchTeamSize(ScrimmageChallengeRequest request)
        {
            return request.ChallengerTeam?.TeamSize == request.TeamSize &&
                request.OpponentTeam?.TeamSize == request.TeamSize;
        }
    }

    public async Task<ScrimmageResult> ChallengeAsync(string challengerTeamName, string opponentTeamName, TeamSize TeamSize)
    {
        try
        {
            // Get team information first
            var challengerTeam = await _teamData.GetByNameAsync(challengerTeamName, DatabaseComponent.Repository);
            var opponentTeam = await _teamData.GetByNameAsync(opponentTeamName, DatabaseComponent.Repository);

            // Perform all validation checks with actual team data
            // var validationResult = await ScrimmageCommandsValidation.ValidateScrimmageChallenge(
            //     challengerTeamName, opponentTeamName, TeamSize, challengerTeam, opponentTeam);
            // TODO: Implement validation
            var validationResult = new ValidationResult();
            if (!validationResult.IsValid)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = validationResult.Errors.FirstOrDefault()?.ErrorMessage
                };
            }

            // Create the scrimmage using the validated team data
            var scrimmage = new Scrimmage
            {
                Id = Guid.NewGuid(),
                Team1Id = challengerTeam!.Data!.Id,
                Team2Id = opponentTeam!.Data!.Id,
                TeamSize = TeamSize,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save the scrimmage using the repository
            var savedScrimmage = await _scrimmageData.CreateAsync(scrimmage, DatabaseComponent.Repository);
            if (!savedScrimmage.Success)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = savedScrimmage.ErrorMessage
                };
            }

            return new ScrimmageResult
            {
                Success = true,
                Message = $"Scrimmage challenge created successfully between {challengerTeamName} and {opponentTeamName}",
                Scrimmage = savedScrimmage.Data,
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


    // public async Task<ScrimmageResult> SubmitMapBansAsync(string scrimmageId, string teamId, List<string> mapBans)
    // {
    //     try
    //     {
    //         // Get scrimmage using repository's hybrid approach
    //         var scrimmage = await _scrimmageData.GetByIdAsync(scrimmageId, DatabaseComponent.Repository);
    //         if (scrimmage == null)
    //             return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage not found" };

    //         // TODO: Check right state to submit map bans
    //         var scrimmageState = ScrimmageCore.StateMachine.CanTransition(scrimmage, ScrimmageCore.ScrimmageStatus.InProgress);

    //         // Check if scrimmage is in the right state
    //         if (!scrimmageState)
    //             return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage is not in progress" };

    //         // Get the associated match
    //         if (scrimmage.Match == null)
    //             return new ScrimmageResult { Success = false, ErrorMessage = "Match not found" };

    //         // SubmitMapBans() moved to MatchService - will be handled by ScrimmageService
    //         // TODO: Update to use MatchService.SubmitMapBansAsync()

    //         // Persist changes using repository's hybrid approach
    //         await _scrimmageData.UpdateAsync(scrimmage, DatabaseComponent.Repository);

    //         return new ScrimmageResult
    //         {
    //             Success = true,
    //             Message = "Map bans submitted successfully"
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         return new ScrimmageResult
    //         {
    //             Success = false,
    //             ErrorMessage = $"Error submitting map bans: {ex.Message}"
    //         };
    //     }
    // }

    // public async Task<ScrimmageResult> SubmitDeckCodeAsync(string scrimmageId, string teamId, string deckCode)
    // {
    //     try
    //     {
    //         // Get scrimmage using repository's hybrid approach
    //         var scrimmage = await _scrimmageData.GetByIdAsync(scrimmageId, DatabaseComponent.Repository);
    //         if (scrimmage == null)
    //             return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage not found" };

    //         // Check if scrimmage is in the right state
    //         if (!ScrimmageCore.StateMachine.CanTransition(scrimmage, ScrimmageCore.ScrimmageStatus.InProgress))
    //             return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage is not in progress" };

    //         // Get the associated match
    //         if (scrimmage.Match == null)
    //             return new ScrimmageResult { Success = false, ErrorMessage = "Match not found" };

    //         // SubmitDeckCode() moved to MatchService - will be handled by ScrimmageService
    //         // TODO: Update to use MatchService.SubmitDeckCodeAsync()

    //         // Persist changes using repository's hybrid approach
    //         await _scrimmageData.UpdateAsync(scrimmage, DatabaseComponent.Repository);

    //         return new ScrimmageResult
    //         {
    //             Success = true,
    //             Message = "Deck code submitted successfully"
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         return new ScrimmageResult
    //         {
    //             Success = false,
    //             ErrorMessage = $"Error submitting deck code: {ex.Message}"
    //         };
    //     }
    // }

    // public async Task<ScrimmageResult> ReportGameResultAsync(string scrimmageId, string winnerId)
    // {
    //     try
    //     {
    //         // Get scrimmage using repository's hybrid approach
    //         var scrimmage = await _scrimmageData.GetByIdAsync(scrimmageId, DatabaseComponent.Repository);
    //         if (scrimmage == null)
    //             return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage not found" };

    //         // Check if scrimmage is in the right state
    //         if (!ScrimmageCore.StateMachine.CanTransition(scrimmage, ScrimmageCore.ScrimmageStatus.InProgress))
    //             return new ScrimmageResult { Success = false, ErrorMessage = "Scrimmage is not in progress" };

    //         // Get the associated match
    //         if (scrimmage.Match == null)
    //             return new ScrimmageResult { Success = false, ErrorMessage = "Match not found" };

    //         // ReportGameResult() moved to MatchService - will be handled by ScrimmageService
    //         // TODO: Update to use MatchService.ReportGameResultAsync()

    //         // TODO: Set up event subscription to get match state
    //         // var matchState = await _eventBus.RequestAsync

    //         // Check if the match is completed and update scrimmage status if needed
    //         // if (matchState == MatchCore.MatchStatus.Completed)
    //         // {
    //         //     // Complete the scrimmage using existing method
    //         //     await scrimmage.CompleteAsync(winnerId);
    //         // }

    //         // Persist changes using repository's hybrid approach
    //         await _scrimmageData.UpdateAsync(scrimmage, DatabaseComponent.Repository);

    //         return new ScrimmageResult
    //         {
    //             Success = true,
    //             Message = "Game result reported successfully"
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         return new ScrimmageResult
    //         {
    //             Success = false,
    //             ErrorMessage = $"Error reporting game result: {ex.Message}"
    //         };
    //     }
    // }

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