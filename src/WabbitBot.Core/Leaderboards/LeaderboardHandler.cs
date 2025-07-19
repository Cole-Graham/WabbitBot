using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Core.Leaderboards.Data.Interface;
using WabbitBot.Core.Scrimmages.ScrimmageRating;
using WabbitBot.Core.Common.Models;
using System.Linq;
using System.Collections.Generic;

namespace WabbitBot.Core.Leaderboards
{
    /// <summary>
    /// Handler for leaderboard-related events and requests.
    /// Leaderboards are now read-only views generated from Season data.
    /// </summary>
    public class LeaderboardHandler : CoreBaseHandler
    {
        private readonly ILeaderboardRepository _leaderboardRepo;
        private readonly SeasonRatingService _seasonRatingService;

        public LeaderboardHandler(
            ILeaderboardRepository leaderboardRepo,
            SeasonRatingService seasonRatingService)
            : base(CoreEventBus.Instance)
        {
            _leaderboardRepo = leaderboardRepo ?? throw new ArgumentNullException(nameof(leaderboardRepo));
            _seasonRatingService = seasonRatingService ?? throw new ArgumentNullException(nameof(seasonRatingService));
        }

        public override Task InitializeAsync()
        {
            // Subscribe to team rating update events to refresh leaderboards
            EventBus.Subscribe<TeamRatingUpdatedEvent>(async evt =>
            {
                try
                {
                    await RefreshLeaderboardAsync(evt.GameSize);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error refreshing leaderboard: {ex.Message}");
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Refreshes the leaderboard for a specific game size from Season data.
        /// </summary>
        private async Task RefreshLeaderboardAsync(GameSize gameSize)
        {
            try
            {
                // Get all team ratings from Season
                var teamRatings = await _seasonRatingService.GetAllTeamRatingsAsync(gameSize);

                // Create leaderboard entries
                var rankings = new Dictionary<string, LeaderboardEntry>();
                foreach (var (teamId, rating) in teamRatings)
                {
                    rankings[teamId] = new LeaderboardEntry
                    {
                        Name = teamId,
                        Rating = rating,
                        IsTeam = true,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                // Get or create leaderboard
                var leaderboards = await _leaderboardRepo.GetLeaderboardsByGameSizeAsync(gameSize);
                var leaderboard = leaderboards.FirstOrDefault();

                if (leaderboard == null)
                {
                    leaderboard = new Leaderboard();
                }

                // Update rankings
                leaderboard.Rankings[gameSize] = rankings;

                // Save to database
                if (leaderboard.Id == Guid.Empty)
                {
                    await _leaderboardRepo.AddAsync(leaderboard);
                }
                else
                {
                    await _leaderboardRepo.UpdateAsync(leaderboard);
                }

                Console.WriteLine($"Leaderboard refreshed for {gameSize}: {rankings.Count} teams");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing leaderboard for {gameSize}: {ex.Message}");
            }
        }

        /// <summary>
        /// Manually triggers a leaderboard refresh for all game sizes.
        /// </summary>
        public async Task RefreshAllLeaderboardsAsync()
        {
            foreach (GameSize gameSize in Enum.GetValues(typeof(GameSize)))
            {
                if (gameSize != GameSize.OneVOne) // Teams don't participate in 1v1
                {
                    await RefreshLeaderboardAsync(gameSize);
                }
            }
        }
    }
}
