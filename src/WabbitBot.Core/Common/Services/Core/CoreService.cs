using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.Common.ErrorHandling;
using WabbitBot.Core.Common.Database;

namespace WabbitBot.Core.Common.Services
{
    /// <summary>
    /// Main CoreService that handles all core entity operations
    /// Uses direct instantiation instead of dependency injection
    /// </summary>
    public partial class CoreService
    {
        // Event bus and error handling
        private readonly ICoreEventBus _eventBus;
        private readonly ICoreErrorHandler _errorHandler;
        private readonly WabbitBotDbContext _dbContext;

        public CoreService(
            ICoreEventBus eventBus,
            ICoreErrorHandler errorHandler,
            WabbitBotDbContext dbContext)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Initialize database services
            InitializeDatabaseServices();
        }


        #region legacy??
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initialize data services
            await InitializeDataServicesAsync();

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private Task InitializeDataServicesAsync()
        {
            // Database services are initialized in constructor
            // Additional async initialization can go here if needed

            // Services are now ready to use
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Cleanup resources
            await base.StopAsync(cancellationToken);
        }
        #endregion
    }
}