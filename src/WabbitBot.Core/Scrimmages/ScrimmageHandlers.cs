using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Configuration;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;
using WabbitBot.Core.Scrimmages;

namespace WabbitBot.Core.Scrimmages
{
    public static partial class ScrimmageHandler
    {
        public static async Task<Result> HandleChallengeRequestedAsync(ChallengeRequested evt)
        {
            // Convert to entities
            var challengerTeamResult = await CoreService.Teams.GetByNameAsync(
                evt.ChallengerTeamName,
                DatabaseComponent.Repository
            );
            var opponentTeamResult = await CoreService.Teams.GetByNameAsync(
                evt.OpponentTeamName,
                DatabaseComponent.Repository
            );
            if (!challengerTeamResult.Success || challengerTeamResult.Data == null)
            {
                return Result.Failure("Challenger team not found");
            }
            if (!opponentTeamResult.Success || opponentTeamResult.Data == null)
            {
                return Result.Failure("Opponent team not found");
            }
            var ChallengerTeam = challengerTeamResult.Data;
            var OpponentTeam = opponentTeamResult.Data;
            await using var db = WabbitBotDbContextProvider.CreateDbContext();
            var mashinaUser = await db.Set<MashinaUser>()
                .Where(u => u.DiscordUserId == evt.IssuedByDiscordUserId)
                .FirstOrDefaultAsync();
            if (mashinaUser == null || mashinaUser.Player == null)
            {
                return Result.Failure("Player not found for Discord user");
            }
            var IssuedByPlayer = mashinaUser.Player;
            var selectedPlayersResult = new List<Player>();
            for (int i = 0; i < evt.SelectedPlayerNames.Length; i++)
            {
                var playerResult = await CoreService.Players.GetByNameAsync(
                    evt.SelectedPlayerNames[i],
                    DatabaseComponent.Repository
                );
                if (!playerResult.Success || playerResult.Data == null)
                {
                    return Result.Failure("Player not found");
                }
                selectedPlayersResult.Add(playerResult.Data);
            }
            if (selectedPlayersResult.Count != evt.SelectedPlayerNames.Length)
            {
                return Result.Failure("Player not found");
            }
            var SelectedPlayers = new Player[selectedPlayersResult.Count];
            for (int i = 0; i < selectedPlayersResult.Count; i++)
            {
                SelectedPlayers[i] = selectedPlayersResult[i];
            }

            var challengeResult = ScrimmageCore.Factory.CreateChallenge(
                ChallengerTeam,
                OpponentTeam,
                IssuedByPlayer,
                SelectedPlayers,
                (TeamSize)evt.TeamSize,
                evt.BestOf
            );
            if (!challengeResult.Success)
            {
                return Result.Failure("Failed to create challenge");
            }
            var challenge = challengeResult.Data;
            if (challenge == null)
            {
                return Result.Failure("Failed to create challenge");
            }

            var pubResult = await ScrimmageCore.PublishChallengeCreatedAsync(challenge.Id);
            if (!pubResult.Success)
            {
                return Result.Failure("Failed to publish challenge created");
            }

            return Result.CreateSuccess(challenge.Id.ToString());
        }

        public static async Task<Result> HandleChallengeAcceptedAsync(
            Guid ChallengeId,
            Guid OpponentTeamId,
            Guid[] OpponentSelectedPlayerIds,
            Guid AcceptedByPlayerId
        )
        {
            var getChallenge = await CoreService.ScrimmageChallenges.GetByIdAsync(
                ChallengeId,
                DatabaseComponent.Repository
            );
            if (!getChallenge.Success)
            {
                return Result.Failure("Failed to get challenge");
            }
            var Challenge = getChallenge.Data;
            if (Challenge == null)
            {
                return Result.Failure("Challenge not found");
            }
            var getOpponentTeam = await CoreService.Teams.GetByIdAsync(OpponentTeamId, DatabaseComponent.Repository);
            if (!getOpponentTeam.Success || getOpponentTeam.Data == null)
            {
                return Result.Failure("Opponent team not found");
            }
            var OpponentTeam = getOpponentTeam.Data;
            if (OpponentTeam == null)
            {
                return Result.Failure("Opponent team not found");
            }
            Challenge.OpponentTeam = OpponentTeam;

            var OpponentSelectedPlayers = new Player[OpponentSelectedPlayerIds.Length];
            for (int i = 0; i < OpponentSelectedPlayerIds.Length; i++)
            {
                var getPlayer = await CoreService.Players.GetByIdAsync(
                    OpponentSelectedPlayerIds[i],
                    DatabaseComponent.Repository
                );
                if (!getPlayer.Success || getPlayer.Data == null)
                {
                    return Result.Failure("Player not found");
                }
                OpponentSelectedPlayers[i] = getPlayer.Data;
            }
            if (OpponentSelectedPlayers.Length != OpponentSelectedPlayerIds.Length)
            {
                return Result.Failure(
                    $"{OpponentSelectedPlayerIds.Length - OpponentSelectedPlayers.Length} Player(s) not found."
                );
            }

            var getAcceptedByPlayer = await CoreService.Players.GetByIdAsync(
                AcceptedByPlayerId,
                DatabaseComponent.Repository
            );
            if (!getAcceptedByPlayer.Success)
            {
                return Result.Failure("Failed to get accepted by player");
            }
            var AcceptedByPlayer = getAcceptedByPlayer.Data;
            if (AcceptedByPlayer == null)
            {
                return Result.Failure("Accepted by player not found");
            }
            Challenge.OpponentTeamPlayers = [AcceptedByPlayer, .. OpponentSelectedPlayers];
            if (Challenge.ChallengerTeam == null)
            {
                return Result.Failure("Challenger team not found");
            }
            if (Challenge.OpponentTeam == null)
            {
                return Result.Failure("Opponent team not found");
            }
            if (Challenge.IssuedByPlayer == null)
            {
                return Result.Failure("Issued by player not found");
            }

            // Create scrimmage
            var getNewScrimmage = await ScrimmageCore.CreateScrimmageAsync(
                ChallengeId,
                Challenge,
                Challenge.ChallengerTeam,
                OpponentTeam,
                Challenge.ChallengerTeamPlayers,
                Challenge.OpponentTeamPlayers,
                Challenge.IssuedByPlayer,
                AcceptedByPlayer
            );
            if (!getNewScrimmage.Success)
            {
                return Result.Failure("Failed to create scrimmage");
            }
            var NewScrimmage = getNewScrimmage.Data;
            if (NewScrimmage == null)
            {
                return Result.Failure("Failed to create scrimmage, no data returned");
            }

            var pubResult = await ScrimmageCore.PublishScrimmageCreatedAsync(NewScrimmage.Id);
            if (!pubResult.Success)
            {
                return Result.Failure("Failed to publish scrimmage created");
            }

            return Result.CreateSuccess();
        }

        public static async Task<Result> HandleScrimmageCreatedAsync(ScrimmageCreated evt)
        {
            // Commented out pending generator publishers
            // var publishResult = await PublishMatchProvisioningRequestedAsync(evt.ScrimmageId);
            // if (!publishResult.Success)
            // {
            //     return Result.Failure("Failed to publish match provisioning requested");
            // }

            return Result.CreateSuccess();
        }
    }
}
