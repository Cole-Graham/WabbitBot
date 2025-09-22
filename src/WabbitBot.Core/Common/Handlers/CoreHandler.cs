using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Common.BotCore
{
    /// <summary>
    /// Base class for all Core event handlers that provides common functionality.
    /// </summary>
    public abstract class CoreHandler
    {
        protected readonly ICoreEventBus EventBus;

        protected CoreHandler(ICoreEventBus eventBus)
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