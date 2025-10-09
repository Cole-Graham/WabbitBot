using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Models.Scrimmage;
using WabbitBot.Core.Common.Services;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Configuration;
using System.Net.NetworkInformation;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Core.Common.Database;
using WabbitBot.Core.Scrimmages;


namespace WabbitBot.Core.Scrimmages
{
    public static partial class ScrimmageHandler
    {
        public static async Task<Result> HandleChallengeRequestedAsync(
            int TeamSize,
            string ChallengerTeamName,
            string OpponentTeamName,
            string[] SelectedPlayerNames,
            ulong IssuedByDiscordUserId,
            int BestOf)
        {
            // Convert to entities
            var challengerTeamResult = await CoreService.Teams
                .GetByNameAsync(ChallengerTeamName, DatabaseComponent.Repository);
            var opponentTeamResult = await CoreService.Teams
                .GetByNameAsync(OpponentTeamName, DatabaseComponent.Repository);
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
                .Where(u => u.DiscordUserId == IssuedByDiscordUserId)
                .FirstOrDefaultAsync();
            if (mashinaUser == null || mashinaUser.Player == null)
            {
                return Result.Failure("Player not found for Discord user");
            }
            var IssuedByPlayer = mashinaUser.Player;
            var selectedPlayersResult = new List<Player>();
            for (int i = 0; i < SelectedPlayerNames.Length; i++)
            {
                var playerResult = await CoreService.Players
                    .GetByNameAsync(SelectedPlayerNames[i], DatabaseComponent.Repository);
                if (!playerResult.Success || playerResult.Data == null)
                {
                    return Result.Failure("Player not found");
                }
                selectedPlayersResult.Add(playerResult.Data);
            }
            if (selectedPlayersResult.Count != SelectedPlayerNames.Length)
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
                (TeamSize)TeamSize,
                BestOf);
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
            Guid acceptedByPlayerId)
        {
            var challengeResult = await CoreService.ScrimmageChallenges.GetByIdAsync(ChallengeId, DatabaseComponent.Repository);
            if (!challengeResult.Success)
            {
                return Result.Failure("Failed to get challenge");
            }
            var challenge = challengeResult.Data;
            if (challenge == null)
            {
                return Result.Failure("Challenge not found");
            }
            var opponentTeamResult = await CoreService.Teams.GetByIdAsync(OpponentTeamId, DatabaseComponent.Repository);
            if (!opponentTeamResult.Success || opponentTeamResult.Data == null)
            {
                return Result.Failure("Opponent team not found");
            }
            var OpponentTeam = opponentTeamResult.Data;
            if (OpponentTeam == null)
            {
                return Result.Failure("Opponent team not found");
            }

            var opponentSelectedPlayersResult = new List<Player>();
            for (int i = 0; i < OpponentSelectedPlayerIds.Length; i++)
            {
                var playerResult = await CoreService.Players.GetByIdAsync(OpponentSelectedPlayerIds[i], DatabaseComponent.Repository);
                if (!playerResult.Success || playerResult.Data == null)
                {
                    return Result.Failure("Player not found");
                }
                opponentSelectedPlayersResult.Add(playerResult.Data);
            }
            if (opponentSelectedPlayersResult.Count != OpponentSelectedPlayerIds.Length)
            {
                return Result.Failure("Player not found");
            }
            var OpponentSelectedPlayers = opponentSelectedPlayersResult.ToArray();

            var acceptedByPlayerResult = await CoreService.Players.GetByIdAsync(acceptedByPlayerId, DatabaseComponent.Repository);
            if (!acceptedByPlayerResult.Success)
            {
                return Result.Failure("Failed to get accepted by player");
            }
            var AcceptedByPlayer = acceptedByPlayerResult.Data;
            if (AcceptedByPlayer == null)
            {
                return Result.Failure("Accepted by player not found");
            }

            var scrimmageCore = new ScrimmageCore();

            // Create scrimmage
            var scrimmageResult = await scrimmageCore.CreateScrimmageAsync(
                ChallengeId,
                challenge.ChallengerTeam!,
                OpponentTeam,
                challenge.Team1Players,
                [.. OpponentSelectedPlayers],
                challenge.IssuedByPlayer!,
                AcceptedByPlayer);
            if (!scrimmageResult.Success)
            {
                return Result.Failure("Failed to create scrimmage");
            }
            var scrimmage = scrimmageResult.Data;
            if (scrimmage == null)
            {
                return Result.Failure("Failed to create scrimmage");
            }

            var pubResult = await ScrimmageCore.PublishScrimmageCreatedAsync(scrimmage.Id);
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

        public static async Task<Result> HandleMatchProvisioningRequestedAsync(MatchProvisioningRequested evt)
        {
            // Get scrimmage channel from config
            var scrimmageChannelConfig = ConfigurationProvider
                .GetSection<ChannelsOptions>(ChannelsOptions.SectionName).ScrimmageChannel;
            if (scrimmageChannelConfig is null)
            {
                return Result.Failure("Scrimmage channel not found");
            }
            var scrimmageChannelId = scrimmageChannelConfig.Value;

            // Commented out pending generator publishers
            // var publishMatchResult = await PublishMatchProvisioningRequestedAsync(scrimmageChannelId, evt.ScrimmageId);
            // if (!publishMatchResult.Success)
            // {
            //     return Result.Failure("Failed to publish match provisioning requested");
            // }

            return Result.CreateSuccess();
        }
    }
}