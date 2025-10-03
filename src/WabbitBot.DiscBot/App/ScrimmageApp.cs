using WabbitBot.Common.Attributes;
using WabbitBot.Common.Events.EventInterfaces;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App;

/// <summary>
/// This app is library-agnostic and communicates only via events.
/// </summary>
[EventGenerator(GenerateSubscribers = true, DefaultBus = EventBusType.DiscBot, TriggerMode = "OptIn")]
public partial class ScrimmageApp : IScrimmageApp
{

}

