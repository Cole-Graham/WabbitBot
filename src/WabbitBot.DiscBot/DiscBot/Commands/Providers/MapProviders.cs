using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using WabbitBot.Core.Common.Handlers;

namespace WabbitBot.DiscBot.DiscBot.Commands.Providers;

public class MapSizeChoiceProvider : IAutoCompleteProvider
{
    private static readonly MapHandler MapHandler = MapHandler.Instance;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var userInput = ctx.UserInput ?? "";
        var sizes = MapHandler.GetAvailableSizes().Append("All");
        return await Task.FromResult(sizes
            .Where(s => s.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .Select(s => new DiscordAutoCompleteChoice(s, s)));
    }
}

public class MapNameAutoCompleteProvider : IAutoCompleteProvider
{
    private static readonly MapHandler MapHandler = MapHandler.Instance;

    public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
    {
        var userInput = ctx.UserInput ?? "";
        return await Task.FromResult(MapHandler.GetMaps()
            .Where(m => m.Name.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .Select(m => new DiscordAutoCompleteChoice(m.Name, m.Name)));
    }
}