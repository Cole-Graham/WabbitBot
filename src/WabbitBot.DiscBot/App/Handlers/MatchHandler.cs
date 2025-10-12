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
        public static async Task HandleScrimmageCreatedAsync(ScrimmageCreated evt)
        {
            // TODO: Implement scrimmage created handler in MatchHandler
            await Task.CompletedTask;
        }

        public static async Task HandleMatchProvisioningRequestedAsync(MatchProvisioningRequested evt)
        {
            // TODO: Implement match provisioning handler in MatchHandler
            await Task.CompletedTask;
        }
    }
}
