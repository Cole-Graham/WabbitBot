using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.Attributes;

namespace WabbitBot.Core.Scrimmages
{
    /// <summary>
    /// Handler for scrimmage-related events and requests.
    /// </summary>
    [GenerateEventSubscriptions(EnableMetrics = true, EnableErrorHandling = true, EnableLogging = true)]
    public partial class ScrimmageHandler : CoreHandler
    {
        private readonly ScrimmageService _scrimmageService;

        public static ScrimmageHandler Instance { get; } = new();

        private ScrimmageHandler()
            : base(CoreEventBus.Instance)
        {
            _scrimmageService = new ScrimmageService();
        }

        public override Task InitializeAsync()
        {
            // Register auto-generated event subscriptions
            RegisterEventSubscriptions();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles scrimmage accepted events
        /// </summary>
        public async Task HandleScrimmageAcceptedAsync(ScrimmageAcceptedEvent @event)
        {
            try
            {
                // Update the scrimmage status in the state machine
                if (Guid.TryParse(@event.ScrimmageId, out var scrimmageId))
                {
                    // The scrimmage service already updated the state, but we can add additional logic here
                    // For example: notify Discord, update leaderboards, etc.

                    // Publish event to Global Event Bus for Discord integration
                    // The Discord project will subscribe to this event and handle UI updates
                    await EventBus.PublishAsync(@event);
                }
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex);
            }
        }

        /// <summary>
        /// Handles scrimmage declined events
        /// </summary>
        public async Task HandleScrimmageDeclinedAsync(ScrimmageDeclinedEvent @event)
        {
            try
            {
                // Update the scrimmage status in the state machine
                if (Guid.TryParse(@event.ScrimmageId, out var scrimmageId))
                {
                    // The scrimmage service already updated the state, but we can add additional logic here
                    // For example: notify Discord, cleanup resources, etc.

                    // Publish event to Global Event Bus for Discord integration
                    // The Discord project will subscribe to this event and handle UI updates
                    await EventBus.PublishAsync(@event);
                }
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex);
            }
        }

        /// <summary>
        /// Handles scrimmage completed events
        /// </summary>
        public async Task HandleScrimmageCompletedAsync(ScrimmageCompletedEvent @event)
        {
            try
            {
                // Fetch scrimmage data from repository for rating updates
                var scrimmage = await WabbitBot.Core.Common.Data.DataServiceManager.ScrimmageRepository.GetByIdAsync(@event.ScrimmageId);
                if (scrimmage != null)
                {
                    // Publish event to Global Event Bus for Discord integration
                    // The Discord project will subscribe to this event and handle UI updates
                    await EventBus.PublishAsync(@event);

                    // Publish event to rating system for processing - follows simple ID pattern
                    // Rating handlers will fetch all necessary data from repositories
                    await EventBus.PublishAsync(new ScrimmageRatingUpdateEvent
                    {
                        ScrimmageId = @event.ScrimmageId,
                        Timestamp = @event.Timestamp
                    });
                }
            }
            catch (Exception ex)
            {
                await CoreErrorHandler.Instance.HandleErrorAsync(ex);
            }
        }
    }
}
