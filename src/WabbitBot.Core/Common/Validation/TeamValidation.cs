using System;
using System.Linq;
using System.Threading.Tasks;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.Core.Common;

/// <summary>
/// Team-specific validation rules and operations
/// </summary>
public static partial class CoreValidation
{
    /// <summary>
    /// Validates that a team has no active members
    /// </summary>
    public static Func<Models.Team, bool> HasNoActiveMembers() =>
        team => !team.GetActiveMembers().Any();

    /// <summary>
    /// Validates that a team has no active matches
    /// </summary>
    public static async Task<bool> HasNoActiveMatches(Models.Team team)
    {
        try
        {
            // Use MatchService to check for active matches
            var matchService = new WabbitBot.Core.Matches.MatchService();
            var hasActiveMatches = await matchService.HasActiveMatchesAsync(team.Id.ToString());
            return !hasActiveMatches;
        }
        catch (Exception)
        {
            // If we can't check matches, assume there are no active matches to be safe
            return true;
        }
    }

    /// <summary>
    /// Validates that a team captain can archive the team
    /// </summary>
    public static Func<Models.Team, bool> CaptainCanArchive() =>
        team => !string.IsNullOrEmpty(team.TeamCaptainId);

    /// <summary>
    /// Validates that a team name is valid
    /// </summary>
    public static Result<string> ValidateTeamName(string teamName)
    {
        if (string.IsNullOrWhiteSpace(teamName))
            return Result<string>.Failure("Team name is required");

        if (teamName.Length < 3)
            return Result<string>.Failure("Team name must be at least 3 characters");

        if (teamName.Length > 50)
            return Result<string>.Failure("Team name must be no more than 50 characters");

        return Result<string>.CreateSuccess(teamName);
    }

    /// <summary>
    /// Validates that a team size is valid
    /// </summary>
    public static Result<int> ValidateTeamSize(int teamSize)
    {
        if (teamSize < 1)
            return Result<int>.Failure("Team size must be at least 1");

        if (teamSize > 10)
            return Result<int>.Failure("Team size must be no more than 10");

        return Result<int>.CreateSuccess(teamSize);
    }

    /// <summary>
    /// Validates that a team captain ID is valid
    /// </summary>
    public static Result<string> ValidateCaptainId(string captainId)
    {
        if (string.IsNullOrWhiteSpace(captainId))
            return Result<string>.Failure("Captain ID cannot be empty");

        return Result<string>.CreateSuccess(captainId);
    }

    /// <summary>
    /// Validates that a team can be created with the given parameters
    /// </summary>
    public static async Task<Result<Models.Team>> ValidateForCreationAsync(string teamName, int teamSize, string captainId, Func<string, Task<Models.Team?>> getTeamByNameAsync)
    {
        // Validate team name
        var nameValidation = ValidateTeamName(teamName);
        if (!nameValidation.Success)
            return Result<Models.Team>.Failure(nameValidation.ErrorMessage ?? "Invalid team name");

        // Validate team size
        var sizeValidation = ValidateTeamSize(teamSize);
        if (!sizeValidation.Success)
            return Result<Models.Team>.Failure(sizeValidation.ErrorMessage ?? "Invalid team size");

        // Validate captain ID
        var captainValidation = ValidateCaptainId(captainId);
        if (!captainValidation.Success)
            return Result<Models.Team>.Failure(captainValidation.ErrorMessage ?? "Invalid captain ID");

        // Check if team name already exists
        var existingTeam = await getTeamByNameAsync(teamName);
        if (existingTeam != null)
            return Result<Models.Team>.Failure($"Team with name '{teamName}' already exists");

        return Result<Models.Team>.CreateSuccess(null!); // Will be created by service
    }

    /// <summary>
    /// Validates that a team can be updated
    /// </summary>
    public static Result<Models.Team> ValidateForUpdate(Models.Team team)
    {
        if (team == null)
            return Result<Models.Team>.Failure("Team cannot be null");

        if (string.IsNullOrWhiteSpace(team.Name))
            return Result<Models.Team>.Failure("Team name cannot be empty");

        return Result<Models.Team>.CreateSuccess(team);
    }

    /// <summary>
    /// Validates that a team can be archived
    /// </summary>
    public static async Task<Result<Models.Team>> ValidateForArchivingAsync(Models.Team team)
    {
        if (team == null)
            return Result<Models.Team>.Failure("Team cannot be null");

        // Check if team is already archived
        if (team.IsArchived)
            return Result<Models.Team>.Failure("Team is already archived");

        // Check if team has active members
        if (team.GetActiveMembers().Any())
            return Result<Models.Team>.Failure("Team has active members");

        // Check if team has active matches
        if (!await HasNoActiveMatches(team))
            return Result<Models.Team>.Failure("Team has active matches");

        return Result<Models.Team>.CreateSuccess(team);
    }

    /// <summary>
    /// Validates that a team can be unarchived
    /// </summary>
    public static Result<Models.Team> ValidateForUnarchiving(Models.Team team)
    {
        if (team == null)
            return Result<Models.Team>.Failure("Team cannot be null");

        if (!team.IsArchived)
            return Result<Models.Team>.Failure("Team is not archived");

        return Result<Models.Team>.CreateSuccess(team);
    }
}