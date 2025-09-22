using WabbitBot.Core.Common.Models;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Common;

namespace WabbitBot.Core.Scrimmages.Validation;

/// <summary>
/// Validation logic for scrimmage commands
/// </summary>
public static class ScrimmageCommandsValidation
{
    /// <summary>
    /// Validates team names are provided and not empty
    /// </summary>
    public static Result<(string challenger, string opponent)> ValidateTeamNames(string challengerTeamName, string opponentTeamName)
    {
        var challengerValidation = CoreValidation.ValidateString(challengerTeamName, "Challenger team name", required: true);
        if (!challengerValidation.Success)
            return Result<(string, string)>.Failure(challengerValidation.ErrorMessage ?? "Invalid challenger team name");

        var opponentValidation = CoreValidation.ValidateString(opponentTeamName, "Opponent team name", required: true);
        if (!opponentValidation.Success)
            return Result<(string, string)>.Failure(opponentValidation.ErrorMessage ?? "Invalid opponent team name");

        return Result<(string, string)>.CreateSuccess((challengerTeamName, opponentTeamName));
    }

    /// <summary>
    /// Validates that a team is not challenging itself
    /// </summary>
    public static Result<(string challenger, string opponent)> ValidateNotSelfChallenge(string challengerTeamName, string opponentTeamName)
    {
        if (challengerTeamName == opponentTeamName)
        {
            return Result<(string, string)>.Failure("A team cannot challenge itself");
        }

        return Result<(string, string)>.CreateSuccess((challengerTeamName, opponentTeamName));
    }

    /// <summary>
    /// Validates that teams exist using Common validation methods
    /// </summary>
    public static async Task<Result<(Team challenger, Team opponent)>> ValidateTeamsExistAsync(Team? challengerTeam, Team? opponentTeam, string challengerTeamName, string opponentTeamName)
    {
        if (challengerTeam is null)
        {
            return await Task.FromResult(Result<(Team, Team)>.Failure($"Challenger team '{challengerTeamName}' not found"));
        }

        if (opponentTeam is null)
        {
            return await Task.FromResult(Result<(Team, Team)>.Failure($"Opponent team '{opponentTeamName}' not found"));
        }

        return await Task.FromResult(Result<(Team, Team)>.CreateSuccess((challengerTeam, opponentTeam)));
    }

    /// <summary>
    /// Validates that teams have the correct game size
    /// </summary>
    public static Result<(Team challenger, Team opponent, EvenTeamFormat evenTeamFormat)> ValidateTeamEvenTeamFormats(Team challengerTeam, Team opponentTeam, string challengerTeamName, string opponentTeamName, EvenTeamFormat evenTeamFormat)
    {
        if (challengerTeam.TeamSize != evenTeamFormat)
        {
            return Result<(Team, Team, EvenTeamFormat)>.Failure($"Challenger team '{challengerTeamName}' is a {Game.Helpers.GetEvenTeamFormatDisplay(challengerTeam.TeamSize)} team, but you selected {Game.Helpers.GetEvenTeamFormatDisplay(evenTeamFormat)}");
        }

        if (opponentTeam.TeamSize != evenTeamFormat)
        {
            return Result<(Team, Team, EvenTeamFormat)>.Failure($"Opponent team '{opponentTeamName}' is a {Game.Helpers.GetEvenTeamFormatDisplay(opponentTeam.TeamSize)} team, but you selected {Game.Helpers.GetEvenTeamFormatDisplay(evenTeamFormat)}");
        }

        return Result<(Team, Team, EvenTeamFormat)>.CreateSuccess((challengerTeam, opponentTeam, evenTeamFormat));
    }

    /// <summary>
    /// Validates that teams are not archived using Common ArchiveValidation
    /// </summary>
    public static Result<(Team challenger, Team opponent)> ValidateTeamsNotArchived(Team challengerTeam, Team opponentTeam, string challengerTeamName, string opponentTeamName)
    {
        if (challengerTeam.IsArchived)
        {
            return Result<(Team, Team)>.Failure($"Challenger team '{challengerTeamName}' is archived and cannot participate in scrimmages");
        }

        if (opponentTeam.IsArchived)
        {
            return Result<(Team, Team)>.Failure($"Opponent team '{opponentTeamName}' is archived and cannot participate in scrimmages");
        }

        return Result<(Team, Team)>.CreateSuccess((challengerTeam, opponentTeam));
    }

    /// <summary>
    /// Performs all validation checks for a scrimmage challenge using Common validation methods
    /// </summary>
    public static async Task<Result<ScrimmageValidationData>> ValidateScrimmageChallengeAsync(
        string challengerTeamName, string opponentTeamName, EvenTeamFormat evenTeamFormat, Team? challengerTeam, Team? opponentTeam)
    {
        // Validate team names
        var teamNamesResult = ValidateTeamNames(challengerTeamName, opponentTeamName);
        if (!teamNamesResult.Success)
            return Result<ScrimmageValidationData>.Failure(teamNamesResult.ErrorMessage ?? "Team name validation failed");

        // Validate not self-challenge
        var selfChallengeResult = ValidateNotSelfChallenge(challengerTeamName, opponentTeamName);
        if (!selfChallengeResult.Success)
            return Result<ScrimmageValidationData>.Failure(selfChallengeResult.ErrorMessage ?? "Self-challenge validation failed");

        // Validate teams exist
        var teamsExistResult = await ValidateTeamsExistAsync(challengerTeam, opponentTeam, challengerTeamName, opponentTeamName);
        if (!teamsExistResult.Success)
            return Result<ScrimmageValidationData>.Failure(teamsExistResult.ErrorMessage ?? "Team existence validation failed");

        var (challenger, opponent) = teamsExistResult.Data;

        // Validate game sizes
        var evenTeamFormatResult = ValidateTeamEvenTeamFormats(challenger, opponent, challengerTeamName, opponentTeamName, evenTeamFormat);
        if (!evenTeamFormatResult.Success)
            return Result<ScrimmageValidationData>.Failure(evenTeamFormatResult.ErrorMessage ?? "Game size validation failed");

        // Validate teams are not archived
        var archiveResult = ValidateTeamsNotArchived(challenger, opponent, challengerTeamName, opponentTeamName);
        if (!archiveResult.Success)
            return Result<ScrimmageValidationData>.Failure(archiveResult.ErrorMessage ?? "Archive validation failed");

        return Result<ScrimmageValidationData>.CreateSuccess(new ScrimmageValidationData
        {
            ChallengerTeam = challenger,
            OpponentTeam = opponent,
            EvenTeamFormat = evenTeamFormat,
            ChallengerTeamName = challengerTeamName,
            OpponentTeamName = opponentTeamName
        });
    }

    /// <summary>
    /// Legacy validation method for backward compatibility
    /// </summary>
    public static async Task<ValidationResult> ValidateScrimmageChallenge(
        string challengerTeamName, string opponentTeamName, EvenTeamFormat evenTeamFormat, Team? challengerTeam, Team? opponentTeam)
    {
        var result = await ValidateScrimmageChallengeAsync(challengerTeamName, opponentTeamName, evenTeamFormat, challengerTeam, opponentTeam);
        return result.Success ? ValidationResult.Success : new ValidationResult { IsValid = false, ErrorMessage = result.ErrorMessage };
    }
}

/// <summary>
/// Validated data for scrimmage challenges
/// </summary>
public class ScrimmageValidationData
{
    public Team ChallengerTeam { get; set; } = null!;
    public Team OpponentTeam { get; set; } = null!;
    public EvenTeamFormat EvenTeamFormat { get; set; }
    public string ChallengerTeamName { get; set; } = string.Empty;
    public string OpponentTeamName { get; set; } = string.Empty;
}

/// <summary>
/// Result of a validation operation (legacy - use Result<T> instead)
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static ValidationResult Success => new() { IsValid = true };
}
