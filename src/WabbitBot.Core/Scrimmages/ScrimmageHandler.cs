using System;
using System.Threading.Tasks;
using WabbitBot.Core.Common.BotCore;
using WabbitBot.Common.Events.EventInterfaces;

namespace WabbitBot.Core.Scrimmages
{
    /// <summary>
    /// Handler for scrimmage-related events and requests.
    /// </summary>
    public class ScrimmageHandler : CoreBaseHandler
    {
        public ScrimmageHandler()
            : base(CoreEventBus.Instance)
        {
        }

        public override Task InitializeAsync()
        {
            // Future scrimmage event subscriptions will be added here
            return Task.CompletedTask;
        }
    }
}
