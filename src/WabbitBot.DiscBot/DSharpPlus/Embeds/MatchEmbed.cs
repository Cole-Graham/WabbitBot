using DSharpPlus.Entities;
using WabbitBot.Core.Matches;
using WabbitBot.DiscBot.DSharpPlus.Generated;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

public abstract class MatchEmbed : BaseEmbed
{
    protected Match Match { get; private set; } = null!;
    private readonly List<IEmbedField> _fields = new();

    public void SetMatch(Match match)
    {
        Match = match;
        UpdateEmbed();
    }

    protected abstract void UpdateEmbed();

    protected string GetTeamName(string teamId) => teamId == Match.Team1Id ? "Team 1" : "Team 2";
    protected string GetTeamPlayers(int teamNumber) => string.Join(", ", teamNumber == 1 ? Match.Team1PlayerIds : Match.Team2PlayerIds);

    protected string GetMatchProgressBar()
    {
        return EmbedStyling.FormatMatchProgress(Match.CurrentState);
    }

    protected DiscordColor GetStageColor()
    {
        return EmbedStyling.GetMatchStateColor(Match.CurrentState);
    }

    protected string GetStageInstructions()
    {
        var baseInstructions = EmbedStyling.FormatStageInstructions(Match.GetCurrentActionNeeded());
        if (Match.CurrentState == MatchState.Completed && !string.IsNullOrEmpty(Match.WinnerId))
        {
            return $"{baseInstructions} Winner: {EmbedStyling.FormatTeamName(GetTeamName(Match.WinnerId))}";
        }
        return baseInstructions;
    }

    protected void SetTitle(string title) => Title = title;
    protected void SetDescription(string description) => Description = description;
    protected void SetColor(DiscordColor color) => Color = color;
    protected void AddField(string name, string value, bool inline = false)
    {
        _fields.Add(new EmbedField { Name = name, Value = value, IsInline = inline });
        Fields = _fields;
    }
    protected void SetFooter(string text, string? iconUrl = null)
    {
        FooterText = text;
        FooterIconUrl = iconUrl;
    }
    protected void SetThumbnail(string url) => ThumbnailUrl = url;
    protected void SetImage(string url) => ImageUrl = url;
    protected void SetAuthor(string name, string? iconUrl = null, string? url = null) =>
        Author = new EmbedAuthor { Name = name, IconUrl = iconUrl, Url = url };
}