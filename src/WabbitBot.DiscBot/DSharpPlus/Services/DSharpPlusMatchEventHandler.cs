using System;
using System.Threading.Tasks;
using WabbitBot.DiscBot.DiscBot.Events;
using WabbitBot.DiscBot.DiscBot.ErrorHandling;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Matches.Data;

namespace WabbitBot.DiscBot.DSharpPlus.Services
{
    /// <summary>
    /// DSharpPlus-specific implementation of Discord match operations
    /// </summary>
    public class DSharpPlusMatchEventHandler : IMatchDiscordOperations
    {
        private readonly MatchService _matchService;

        public DSharpPlusMatchEventHandler(MatchService matchService)
        {
            _matchService = matchService ?? throw new ArgumentNullException(nameof(matchService));
        }

        /// <summary>
        /// Updates a match with Discord thread information
        /// </summary>
        public async Task UpdateMatchDiscordInfoAsync(string matchId, ulong channelId, ulong team1ThreadId, ulong team2ThreadId)
        {
            try
            {
                if (!Guid.TryParse(matchId, out var matchGuid))
                {
                    throw new ArgumentException($"Invalid match ID format: {matchId}");
                }

                // Get the match from the service
                var match = _matchService.GetMatch(matchGuid);
                if (match == null)
                {
                    throw new InvalidOperationException($"Match not found: {matchId}");
                }

                // Update the Discord thread information
                match.ChannelId = channelId;
                match.Team1ThreadId = team1ThreadId;
                match.Team2ThreadId = team2ThreadId;

                // Save the updated match
                _matchService.UpdateMatch(match);

                Console.WriteLine($"[Discord] Updated match {matchId} with Discord info - Channel: {channelId}, Team1 Thread: {team1ThreadId}, Team2 Thread: {team2ThreadId}");
            }
            catch (Exception ex)
            {
                await DiscordErrorHandler.Instance.HandleErrorAsync(ex, $"Failed to update match Discord info for match {matchId}");
                throw;
            }
        }
    }
}
