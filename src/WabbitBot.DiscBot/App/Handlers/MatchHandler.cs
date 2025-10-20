using System.Text.RegularExpressions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using WabbitBot.Common.Attributes;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Events.Core;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Events;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Handlers;
using WabbitBot.DiscBot.App.Renderers;
using WabbitBot.DiscBot.App.Services.DiscBot;

/// <summary>
/// Handles button and component interactions for match flows.
/// Publishes DiscBot-local interaction events to the event bus.
/// Also handles match-related "Requested" events and calls appropriate Renderer methods.
/// </summary>
namespace WabbitBot.DiscBot.App.Handlers
{
    /// <summary>
    /// Handles button and component interactions for match flows.
    /// Publishes DiscBot-local interaction events to the event bus.
    /// Also handles match-related "Requested" events and calls appropriate Renderer methods.
    /// </summary>
    public partial class MatchHandler
    {
        public static async Task HandleScrimmageMatchCompletedAsync(ScrimmageMatchCompleted evt)
        {
            try
            {
                // Fetch the match
                var matchResult = await CoreService.Matches.GetByIdAsync(evt.MatchId, DatabaseComponent.Repository);
                if (!matchResult.Success || matchResult.Data is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException($"Failed to get match: {matchResult.ErrorMessage}"),
                        "Failed to get match",
                        nameof(HandleScrimmageMatchCompletedAsync)
                    );
                    return;
                }
                var match = matchResult.Data;

                // Validate that threads exist
                if (match.Team1ThreadId is null || match.Team2ThreadId is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Match threads not found"),
                        "Match threads not found",
                        nameof(HandleScrimmageMatchCompletedAsync)
                    );
                    return;
                }

                // Get the Discord client and threads
                var client = DiscBotService.Client;
                if (client is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Discord client not initialized"),
                        "Discord client not initialized",
                        nameof(HandleScrimmageMatchCompletedAsync)
                    );
                    return;
                }

                // Get the threads
                var team1Thread = await client.GetChannelAsync(match.Team1ThreadId.Value) as DiscordThreadChannel;
                var team2Thread = await client.GetChannelAsync(match.Team2ThreadId.Value) as DiscordThreadChannel;

                if (team1Thread is null || team2Thread is null)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException("Failed to retrieve Discord thread channels"),
                        "Failed to retrieve Discord thread channels",
                        nameof(HandleScrimmageMatchCompletedAsync)
                    );
                    return;
                }

                // Render the match complete containers
                var renderResult = await MatchRenderer.RenderMatchCompleteContainerAsync(
                    evt.MatchId,
                    evt.WinnerTeamId,
                    team1Thread,
                    team2Thread
                );

                if (!renderResult.Success)
                {
                    await DiscBotService.ErrorHandler.CaptureAsync(
                        new InvalidOperationException(
                            $"Failed to render match complete container: {renderResult.ErrorMessage}"
                        ),
                        "Failed to render match complete container",
                        nameof(HandleScrimmageMatchCompletedAsync)
                    );
                }
            }
            catch (Exception ex)
            {
                await DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to handle ScrimmageMatchCompleted event: {ex.Message}",
                    nameof(HandleScrimmageMatchCompletedAsync)
                );
            }
        }
    }
}
