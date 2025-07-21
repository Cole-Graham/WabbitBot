using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.DiscBot.DiscBot.Commands;

/// <summary>
/// Pure business logic for scrimmage commands - no Discord dependencies
/// </summary>
public class ScrimmageCommands
{
    #region Business Logic Methods

    public async Task<ScrimmageResult> ChallengeAsync(string challengerTeamId, string opponentTeamId, GameSize gameSize)
    {
        try
        {
            // Validate teams exist
            if (string.IsNullOrEmpty(challengerTeamId) || string.IsNullOrEmpty(opponentTeamId))
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = "Both challenger and opponent team IDs are required"
                };
            }

            if (challengerTeamId == opponentTeamId)
            {
                return new ScrimmageResult
                {
                    Success = false,
                    ErrorMessage = "A team cannot challenge itself"
                };
            }

            // TODO: Validate teams exist in the system
            // This would typically involve checking against a team repository
            await Task.CompletedTask; // Placeholder for future async repository calls

            // Create the scrimmage
            var scrimmage = Scrimmage.Create(challengerTeamId, opponentTeamId, gameSize);

            return new ScrimmageResult
            {
                Success = true,
                Message = $"Scrimmage challenge created successfully. Challenge expires in 24 hours.",
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