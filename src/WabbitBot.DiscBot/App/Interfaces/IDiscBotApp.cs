namespace WabbitBot.DiscBot.App;

/// <summary>
/// Marker interface for all DiscBot application flows.
/// Apps are library-agnostic and communicate only via events through DiscBotService.EventBus.
/// Apps must not call DSharpPlus or perform database operations directly.
/// </summary>
public interface IDiscBotApp
{
}

