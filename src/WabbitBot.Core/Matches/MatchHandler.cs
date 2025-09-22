using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Attributes;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Matches.Data.Interface;
using WabbitBot.Core.Common.Data.Interface;

namespace WabbitBot.Core.Matches
{
    /// <summary>
    /// Handler for match-related events and requests.
    /// </summary>
    [GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
    public partial class MatchHandler : CoreHandler
    {
        private readonly MatchService _matchService;

        public static MatchHandler Instance { get; } = new();

        private MatchHandler()
            : base(CoreEventBus.Instance)
        {
            _matchService = new MatchService();
        }

        private IGameRepository GameRepository =>
            WabbitBot.Core.Common.Data.DataServiceManager.GameRepository;

        private IGameCache GameCache =>
            WabbitBot.Core.Common.Data.DataServiceManager.GameCache;

        private IMatchRepository MatchRepository =>
            WabbitBot.Core.Common.Data.DataServiceManager.MatchRepository;

        private IMatchCache MatchCache =>
            WabbitBot.Core.Common.Data.DataServiceManager.MatchCache;

        public override Task InitializeAsync()
        {
            // Register auto-generated event subscriptions
            RegisterEventSubscriptions();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles game created events from GameService and updates match state
        /// </summary>
        public async Task HandleGameCreatedAsync(GameCreatedEvent evt)
        {
            try
            {
                // Get the full game object since the event only contains the ID
                var game = await GameRepository.GetByIdAsync(evt.GameId) ??
                          await GameCache.GetAsync(evt.GameId);
                if (game is null)
                {
                    await CoreErrorHandler.Instance.HandleErrorAsync(new InvalidOperationException($"Game not found: {evt.GameId}"));
                    return;
                }

                // Delegate business logic to MatchService
                await _matchService.UpdateMatchWithGameAsync(game);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleGameCreatedAsync");
            }
        }

        /// <summary>
        /// Handles match created events
        /// </summary>
        public async Task HandleMatchCreatedAsync(MatchCreatedEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleMatchCreatedAsync");
            }
        }

        /// <summary>
        /// Handles match started events
        /// </summary>
        public async Task HandleMatchStartedAsync(MatchStartedEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleMatchStartedAsync");
            }
        }

        /// <summary>
        /// Handles match completed events
        /// </summary>
        public async Task HandleMatchCompletedAsync(MatchCompletedEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleMatchCompletedAsync");
            }
        }

        /// <summary>
        /// Handles match cancelled events
        /// </summary>
        public async Task HandleMatchCancelledAsync(MatchCancelledEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleMatchCancelledAsync");
            }
        }

        /// <summary>
        /// Handles match forfeited events
        /// </summary>
        public async Task HandleMatchForfeitedAsync(MatchForfeitedEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleMatchForfeitedAsync");
            }
        }

        /// <summary>
        /// Handles match player joined events
        /// </summary>
        public async Task HandleMatchPlayerJoinedAsync(MatchPlayerJoinedEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleMatchPlayerJoinedAsync");
            }
        }

        /// <summary>
        /// Handles match player left events
        /// </summary>
        public async Task HandleMatchPlayerLeftAsync(MatchPlayerLeftEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleMatchPlayerLeftAsync");
            }
        }

        /// <summary>
        /// Handles game started events
        /// </summary>
        public async Task HandleGameStartedAsync(WabbitBot.Core.Common.Events.GameStartedEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleGameStartedAsync");
            }
        }


        /// <summary>
        /// Handles game cancelled events
        /// </summary>
        public async Task HandleGameCancelledAsync(WabbitBot.Core.Common.Events.GameCancelledEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleGameCancelledAsync");
            }
        }

        /// <summary>
        /// Handles game forfeited events
        /// </summary>
        public async Task HandleGameForfeitedAsync(WabbitBot.Core.Common.Events.GameForfeitedEvent @event)
        {
            try
            {
                // The match service already handled the business logic, just forward the event
                // Publish event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleGameForfeitedAsync");
            }
        }

        /// <summary>
        /// Handles game completed events and checks if match victory conditions are met
        /// </summary>
        public async Task HandleGameCompletedAsync(WabbitBot.Core.Common.Events.GameCompletedEvent @event)
        {
            try
            {
                // Get the full game object to access MatchId
                var game = await GameRepository.GetByIdAsync(@event.GameId) ??
                          await GameCache.GetAsync(@event.GameId);
                if (game is null)
                {
                    Console.WriteLine($"Game not found: {@event.GameId}");
                    return;
                }

                // Delegate business logic to MatchService
                await _matchService.CheckMatchVictoryConditionsAsync(game.MatchId, @event.CompletedByUserId, @event.CompletedByPlayerName);

                // Forward the game completed event to Global Event Bus for Discord integration
                await EventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex, "HandleGameCompletedAsync");
            }
        }
    }
}