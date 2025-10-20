using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using WabbitBot.Common.Data.Interfaces;
using WabbitBot.Common.Models;
using WabbitBot.Core.Common.Models.Common;
using WabbitBot.Core.Common.Services;
using WabbitBot.DiscBot.App.Providers;
using WabbitBot.DiscBot.App.Services.DiscBot;

namespace WabbitBot.DiscBot.App.Commands
{
    /// <summary>
    /// Discord slash commands for game-level operations (replay submission, etc.).
    /// Uses DSharpPlus.Commands only (not CommandsNext or SlashCommands).
    /// </summary>
    public partial class GameCommands { }
}
