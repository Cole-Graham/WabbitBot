using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using WabbitBot.Core.Common.Services;

namespace WabbitBot.DiscBot.Commands.Providers;

public class MapSizeChoiceProvider : IChoiceProvider, ISlashCommandOptionProvider
{
    private static readonly MapService MapService = MapService.Instance;

    public IEnumerable<CommandChoice> GetChoices()
    {
        return MapService.GetAvailableSizes()
            .Select(size => new CommandChoice(size, size))
            .Append(new CommandChoice("All", "all"));
    }

    public IEnumerable<DiscordApplicationCommandOptionChoice> GetSlashChoices()
    {
        return GetChoices()
            .Select(c => new DiscordApplicationCommandOptionChoice(c.Name, c.Value));
    }
}

public class MapNameAutoCompleteProvider : IAutoCompleteProvider, IAutoComplete
{
    private static readonly MapService MapService = MapService.Instance;

    public IEnumerable<CommandChoice> GetSuggestions(string userInput)
    {
        return MapService.GetMaps()
            .Where(m => m.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(m => new CommandChoice(m.Name, m.Name));
    }

    public IEnumerable<DiscordAutoCompleteChoice> GetAutoComplete(AutoCompleteContext ctx)
    {
        var userInput = ctx.FocusedOption.Value?.ToString() ?? "";
        return GetSuggestions(userInput)
            .Select(c => new DiscordAutoCompleteChoice(c.Name, c.Value));
    }
}