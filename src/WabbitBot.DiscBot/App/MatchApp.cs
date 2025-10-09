using System.Security.Cryptography.X509Certificates;
using DSharpPlus.Commands.ArgumentModifiers;
using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Interfaces;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App
{
    public partial class MatchApp : IMatchApp
    {
        // Create Match threads
        public static async Task<Result> CreateMatchThreadsAsync(ulong scrimmageChannelId, Guid matchId)
        {
            var client = DiscordClientProvider.GetClient();
            var scrimmageChannel = await client.GetChannelAsync(scrimmageChannelId);
            var matchResult = await CoreService.Matches.GetByIdAsync(matchId, DatabaseComponent.Repository);
            if (!matchResult.Success)
            {
                return Result.Failure("Match not found");
            }
            if (matchResult.Data == null)
            {
                return Result.Failure("Match not found");
            }
            var match = matchResult.Data!;

            // Query to get all MashinaUsers for match.Team1PlayerIds and match.Team2PlayerIds
            // Figure out how complex queries are supposed to work, for now just do it in memory.
            var team1Mentions = new List<string>();
            foreach (var playerId in match.Team1PlayerIds)
            {
                var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                if (playerResult.Success && playerResult.Data?.MashinaUser?.DiscordMention is not null)
                {
                    team1Mentions.Add(playerResult.Data.MashinaUser.DiscordMention);
                }
            }
            var team2Mentions = new List<string>();
            foreach (var playerId in match.Team2PlayerIds)
            {
                var playerResult = await CoreService.Players.GetByIdAsync(playerId, DatabaseComponent.Repository);
                if (playerResult.Success && playerResult.Data?.MashinaUser?.DiscordMention is not null)
                {
                    team2Mentions.Add(playerResult.Data.MashinaUser.DiscordMention);
                }
            }

            // Create standalone threads without starter messages
            var team1Thread = await scrimmageChannel.CreateThreadAsync(
                $"{match.Team1.Name} vs. {match.Team2.Name}",
                DiscordAutoArchiveDuration.Day,
                DiscordChannelType.PrivateThread);

            var team2Thread = await scrimmageChannel.CreateThreadAsync(
                $"{match.Team2.Name} vs. {match.Team1.Name}",
                DiscordAutoArchiveDuration.Day,
                DiscordChannelType.PrivateThread);

            // Build messages
            var team1Message = new DiscordMessageBuilder()
                .WithContent($"{match.Team1.Name}: " + string.Join(", ", team1Mentions));
            var team2Message = new DiscordMessageBuilder()
                .WithContent($"{match.Team2.Name}: " + string.Join(", ", team2Mentions));

            // Send messages to threads
            await team1Thread.SendMessageAsync(team1Message);
            await team2Thread.SendMessageAsync(team2Message);

            return Result.CreateSuccess();
        }

    }
}
