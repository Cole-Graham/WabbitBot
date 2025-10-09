// using WabbitBot.Core.Scrimmages;
// using WabbitBot.Core.Common.Models.Common;
// using WabbitBot.Core.Common.Models.Scrimmage;
// using WabbitBot.Core.Common.Services;
// using WabbitBot.Common.Attributes;
// using WabbitBot.Common.Data;
// using WabbitBot.Common.Data.Service;
// using WabbitBot.Common.Data.Interfaces;
// using FluentValidation;
// using FluentValidation.Results;

// namespace WabbitBot.Core.Scrimmages;

// /// <summary>
// /// Pure business logic for scrimmage commands - no Discord dependencies
// /// </summary>
// [WabbitCommand("Scrimmage")]
// public partial class ScrimmageCommands
// {

//     #region Private Fields - Clean Architecture

//     private static readonly DatabaseService<Scrimmage> _scrimmageData = new();
//     private static readonly DatabaseService<Team> _teamData = new();

//     #endregion

//     #region Business Logic Methods

//     public class ScrimmageChallengeRequest
//     {
//         public string ChallengerTeamName { get; set; } = string.Empty;
//         public string OpponentTeamName { get; set; } = string.Empty;
//         public TeamSize TeamSize { get; set; }
//         // Teams fetched via Npgsql earlier—passed in for validation (or fetch inside async rule)
//         public Team? ChallengerTeam { get; set; }
//         public Team? OpponentTeam { get; set; }
//     }

//         // Custom validators (extract your existing logic here—keeps it procedural if you like)
//         //private static bool BeValidTeamName(string name) => CoreValidation.ValidateString(
//         //    name, "TeamName", required: true).Success;
//         //}

//         private static bool NotBeSelfChallenge(
//             ScrimmageChallengeRequest request) => request.ChallengerTeamName != request.OpponentTeamName;

//         private static bool TeamsMatchTeamSize(ScrimmageChallengeRequest request)
//         {
//             return request.ChallengerTeam?.TeamSize == request.TeamSize &&
//                 request.OpponentTeam?.TeamSize == request.TeamSize;
//         }
//     }

//     public async Task<Result> ChallengeAsync(string challengerTeamName, string opponentTeamName, TeamSize TeamSize)
//     {
//         try
//         {
//             // Get team information first
//             var challengerTeam = await _teamData.GetByNameAsync(challengerTeamName, DatabaseComponent.Repository);
//             var opponentTeam = await _teamData.GetByNameAsync(opponentTeamName, DatabaseComponent.Repository);

//             // Perform all validation checks with actual team data
//             // var validationResult = await ScrimmageCommandsValidation.ValidateScrimmageChallenge(
//             //     challengerTeamName, opponentTeamName, TeamSize, challengerTeam, opponentTeam);
//             // TODO: Implement validation
//             var validationResult = new ValidationResult();
//             if (!validationResult.IsValid)
//             {
//                 return new ScrimmageResult
//                 {
//                     Success = false,
//                     ErrorMessage = validationResult.Errors.FirstOrDefault()?.ErrorMessage
//                 };
//             }

//             // Create the scrimmage using the validated team data
//             var scrimmage = new Scrimmage
//             {
//                 Id = Guid.NewGuid(),
//                 Team1Id = challengerTeam!.Data!.Id,
//                 Team2Id = opponentTeam!.Data!.Id,
//                 TeamSize = TeamSize,
//                 CreatedAt = DateTime.UtcNow,
//                 UpdatedAt = DateTime.UtcNow
//             };

//             // Save the scrimmage using the repository
//             var savedScrimmage = await _scrimmageData.CreateAsync(scrimmage, DatabaseComponent.Repository);
//             if (!savedScrimmage.Success)
//             {
//                 return new ScrimmageResult
//                 {
//                     Success = false,
//                     ErrorMessage = savedScrimmage.ErrorMessage
//                 };
//             }

//             return new ScrimmageResult
//             {
//                 Success = true,
//                 Message = $"Scrimmage challenge created successfully between {challengerTeamName} and {opponentTeamName}",
//                 Scrimmage = savedScrimmage.Data,
//                 ChallengerTeamName = challengerTeamName,
//                 OpponentTeamName = opponentTeamName
//             };
//         }
//         catch (Exception ex)
//         {
//             return new ScrimmageResult
//             {
//                 Success = false,
//                 ErrorMessage = $"Error creating scrimmage challenge: {ex.Message}"
//             };
//         }
//     }
//     #endregion
// }