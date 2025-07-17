using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;

namespace WabbitBot.Core.Common.BotCore
{
    /// <summary>
    /// Base class for all Core background services that provides common functionality.
    /// </summary>
    public abstract class CoreBaseService
    {
        protected readonly ICoreEventBus EventBus;
        protected readonly ICoreErrorHandler ErrorHandler;

        protected CoreBaseService(ICoreEventBus eventBus, ICoreErrorHandler errorHandler)
        {
            EventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            ErrorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        /// <summary>
        /// Initializes the service and sets up event subscriptions.
        /// Override this method to add specific event subscriptions.
        /// </summary>
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
