using System;
using System.Threading.Tasks;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;

namespace WabbitBot.Core.Scrimmages
{
    /// <summary>
    /// Service for handling scrimmage-related business logic.
    /// </summary>
    public class ScrimmageService
    {
        private readonly ICoreEventBus _eventBus;
        private readonly ICoreErrorHandler _errorHandler;

        public ScrimmageService(
            ICoreEventBus eventBus,
            ICoreErrorHandler errorHandler)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        // Future scrimmage-related methods will be added here
    }
}