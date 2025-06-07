using DSharpPlus.Entities;
using WabbitBot.Core.Matches;
using WabbitBot.DiscBot.DiscBot.Interfaces;
using WabbitBot.DiscBot.DSharpPlus.Embeds;

namespace WabbitBot.DiscBot.DSharpPlus.EmbedBuilders;

/// <summary>
/// Implementation of IMatchEmbedBuilder that creates DSharpPlus-specific embeds
/// </summary>
public class MatchEmbedBuilders : IMatchEmbedBuilder
{
    public Task<IMatchEmbed> BuildTournamentEmbed(Match match, TournamentInfo tournamentInfo)
    {
        MatchEmbed embed = match.Stage switch
        {
            MatchStage.MapBan => new TournamentMapBanEmbed(),
            MatchStage.DeckSubmission => new TournamentDeckSubmissionEmbed(),
            MatchStage.DeckRevision => new TournamentDeckRevisionEmbed(),
            MatchStage.GameResults => new TournamentGameResultsEmbed(),
            MatchStage.Completed => new TournamentMatchCompletedEmbed(),
            _ => throw new ArgumentException($"Invalid match stage: {match.Stage}")
        };

        embed.SetMatch(match);
        ((TournamentEmbed)embed).SetTournamentInfo(
            tournamentInfo.TournamentId,
            tournamentInfo.GroupName,
            tournamentInfo.CurrentRound,
            tournamentInfo.TotalRounds,
            tournamentInfo.GroupStageMatchNumber,
            tournamentInfo.TotalGroupStageMatches
        );
        return Task.FromResult<IMatchEmbed>(new DSharpPlusMatchEmbed(embed));
    }

    public Task<IMatchEmbed> BuildScrimmageEmbed(Match match)
    {
        MatchEmbed embed = match.Stage switch
        {
            MatchStage.MapBan => new ScrimmageMapBanEmbed(),
            MatchStage.DeckSubmission => new ScrimmageDeckSubmissionEmbed(),
            MatchStage.DeckRevision => new ScrimmageDeckRevisionEmbed(),
            MatchStage.GameResults => new ScrimmageGameResultsEmbed(),
            MatchStage.Completed => new ScrimmageMatchCompletedEmbed(),
            _ => throw new ArgumentException($"Invalid match stage: {match.Stage}")
        };

        embed.SetMatch(match);
        return Task.FromResult<IMatchEmbed>(new DSharpPlusMatchEmbed(embed));
    }
}

/// <summary>
/// DSharpPlus-specific implementation of IMatchEmbed
/// </summary>
internal class DSharpPlusMatchEmbed : IMatchEmbed
{
    private readonly MatchEmbed _embed;

    public DSharpPlusMatchEmbed(MatchEmbed embed)
    {
        _embed = embed;
    }

    public string Title => _embed.Title;
    public string Description => _embed.Description;
    public IReadOnlyList<IEmbedField> Fields => _embed.Fields;
    public string FooterText => _embed.FooterText;
    public string? FooterIconUrl => _embed.FooterIconUrl;
    public string? ThumbnailUrl => _embed.ThumbnailUrl;
    public string? ImageUrl => _embed.ImageUrl;
    public IEmbedAuthor? Author => _embed.Author;
}

/// <summary>
/// DSharpPlus-specific implementation of IEmbedField
/// </summary>
internal class DSharpPlusEmbedField : IEmbedField
{
    private readonly EmbedField _field;

    public DSharpPlusEmbedField(EmbedField field)
    {
        _field = field;
    }

    public string Name => _field.Name;
    public string Value => _field.Value;
    public bool IsInline => _field.IsInline;
}

/// <summary>
/// DSharpPlus-specific implementation of IEmbedAuthor
/// </summary>
internal class DSharpPlusEmbedAuthor : IEmbedAuthor
{
    private readonly EmbedAuthor _author;

    public DSharpPlusEmbedAuthor(EmbedAuthor author)
    {
        _author = author;
    }

    public string Name => _author.Name;
    public string? IconUrl => _author.IconUrl;
    public string? Url => _author.Url;
}