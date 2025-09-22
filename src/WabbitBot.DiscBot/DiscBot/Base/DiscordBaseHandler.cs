using WabbitBot.DiscBot.DiscBot.Events;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.DiscBot.DiscBot.Base
{
    /// <summary>
    /// Base class for all Discord event handlers that provides common functionality.
    /// </summary>
    public abstract class DiscordBaseHandler
    {
        protected readonly IDiscordEventBus EventBus;

        protected DiscordBaseHandler(IDiscordEventBus eventBus)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// Initializes the handler and sets up event subscriptions.
        /// Override this method to add specific event subscriptions.
        /// </summary>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
