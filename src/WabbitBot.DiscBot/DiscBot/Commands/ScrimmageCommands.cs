using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;
using WabbitBot.DiscBot.DSharpPlus;

namespace WabbitBot.DiscBot.DiscBot.Commands;

/// <summary>
/// Pure business logic for scrimmage commands - no Discord dependencies
/// </summary>
public class ScrimmageCommands
{
    #region Business Logic Methods

    public async Task<ScrimmageResult> ChallengeAsync(string challengerTeamName, string opponentTeamName, GameSize gameSize)
    {
        try
        {
            // Validate team names are provided
            if (string.IsNullOrEmpty(challengerTeamName) || string.IsNullOrEmpty(opponentTeamName))
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = "Both challenger and opponent team names are required"
                };
            }

            if (challengerTeamName == opponentTeamName)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = "A team cannot challenge itself"
                };
            }

            // Get team information from the lookup service
            var challengerTeam = await TeamLookupService.GetByNameAsync(challengerTeamName);
            var opponentTeam = await TeamLookupService.GetByNameAsync(opponentTeamName);

            // Validate teams exist
            if (challengerTeam is null)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = $"Challenger team '{challengerTeamName}' not found"
                };
            }

            if (opponentTeam is null)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = $"Opponent team '{opponentTeamName}' not found"
                };
            }

            // Validate teams have the same game size
            if (challengerTeam.TeamSize != gameSize)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = $"Challenger team '{challengerTeamName}' is a {WabbitBot.DiscBot.DSharpPlus.Commands.Helpers.GetGameSizeDisplay(challengerTeam.TeamSize)} team, but you selected {WabbitBot.DiscBot.DSharpPlus.Commands.Helpers.GetGameSizeDisplay(gameSize)}"
                };
            }

            if (opponentTeam.TeamSize != gameSize)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = $"Opponent team '{opponentTeamName}' is a {WabbitBot.DiscBot.DSharpPlus.Commands.Helpers.GetGameSizeDisplay(opponentTeam.TeamSize)} team, but you selected {WabbitBot.DiscBot.DSharpPlus.Commands.Helpers.GetGameSizeDisplay(gameSize)}"
                };
            }

            // Validate teams are not archived
            if (challengerTeam.IsArchived)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = $"Challenger team '{challengerTeamName}' is archived and cannot participate in scrimmages"
                };
            }

            if (opponentTeam.IsArchived)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = $"Opponent team '{opponentTeamName}' is archived and cannot participate in scrimmages"
                };
            }

            // Create the scrimmage using team IDs
            var scrimmage = Scrimmage.Create(challengerTeam.Id.ToString(), opponentTeam.Id.ToString(), gameSize);

            return new ScrimmageResult
            {
                Success = true,
                Message = $"Scrimmage challenge created successfully between {challengerTeamName} and {opponentTeamName}. Challenge expires in 24 hours.",
                Scrimmage = scrimmage
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

    #endregion



    #region Result Classes

    public class ScrimmageResult
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public string? ErrorMessage { get; init; }
        public Scrimmage? Scrimmage { get; init; }
    }

    #endregion
}