using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Common.Models;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Data;

namespace WabbitBot.Core.Leaderboards
{
    /// <summary>
    /// Handler for leaderboard-related events and requests.
    /// Leaderboards are now read-only views generated from Season data.
    /// </summary>
    [GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
    public partial class LeaderboardHandler : CoreHandler
    {
        private readonly LeaderboardService _leaderboardService;

        public static LeaderboardHandler Instance { get; } = new LeaderboardHandler();

        private LeaderboardHandler()
            : base(CoreEventBus.Instance)
        {
            _leaderboardService = new LeaderboardService();
        }

        public override Task InitializeAsync()
        {
            // Register auto-generated event subscriptions
            RegisterEventSubscriptions();
            return Task.CompletedTask;
        }

        [EventHandler(Priority = 1, EnableRetry = true, MaxRetryAttempts = 3)]
        private async Task HandleTeamRatingUpdatedEvent(TeamRatingUpdatedEvent evt)
        {
            // Convert string back to EvenTeamFormat enum
            if (!Enum.TryParse<EvenTeamFormat>(evt.EvenTeamFormat, out var evenTeamFormat))
            {
                Console.WriteLine($"Invalid EvenTeamFormat string in TeamRatingUpdatedEvent: {evt.EvenTeamFormat}");
                return;
            }

            await _leaderboardService.RefreshLeaderboardAsync(evenTeamFormat);
        }
    }
}
