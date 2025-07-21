using WabbitBot.Core.Matches;
using WabbitBot.Core.Scrimmages;

namespace WabbitBot.DiscBot.DiscBot.Interfaces;

public interface IMatchEmbedBuilder
{
    /// <summary>
    /// Builds a tournament match embed with additional tournament context
    /// </summary>
    Task<IMatchEmbed> BuildTournamentEmbed(Match match, TournamentInfo info);

    /// <summary>
    /// Builds a scrimmage match embed
    /// </summary>
    Task<IMatchEmbed> BuildScrimmageEmbed(Match match, Scrimmage scrimmage);
}

public interface IMatchEmbed
{
    string Title { get; }
    string Description { get; }
    IReadOnlyList<IEmbedField> Fields { get; }
    string? FooterText { get; }
    string? FooterIconUrl { get; }
    string? ThumbnailUrl { get; }
    string? ImageUrl { get; }
    IEmbedAuthor? Author { get; }
}

public interface IEmbedField
{
    string Name { get; }
    string Value { get; }
    bool IsInline { get; }
}

public interface IEmbedAuthor
{
    string Name { get; }
    string? IconUrl { get; }
    string? Url { get; }
}

public record TournamentInfo(
    string TournamentId,
    string GroupName,
    int CurrentRound,
    int TotalRounds,
    int GroupStageMatchNumber,
    int TotalGroupStageMatches
);