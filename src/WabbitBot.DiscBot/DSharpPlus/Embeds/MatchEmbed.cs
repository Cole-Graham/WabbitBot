using DSharpPlus.Entities;
using WabbitBot.Core.Matches;
using WabbitBot.DiscBot.DiscBot.Interfaces;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

public abstract class MatchEmbed : BaseEmbed, IMatchEmbed
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
        string mapBanEmoji = Match.Stage > MatchStage.MapBan ? "✅" : Match.Stage == MatchStage.MapBan ? "▶️" : "⬜";
        string deckSubmitEmoji = Match.Stage > MatchStage.DeckSubmission ? "✅" : Match.Stage == MatchStage.DeckSubmission ? "▶️" : "⬜";
        string gameResultsEmoji = Match.Stage == MatchStage.GameResults ? "▶️" : Match.Stage > MatchStage.GameResults ? "✅" : "⬜";

        return $"{mapBanEmoji} Map Bans → {deckSubmitEmoji} Deck Submission → {gameResultsEmoji} Game Results";
    }

    protected DiscordColor GetStageColor()
    {
        return Match.Stage switch
        {
            MatchStage.MapBan => new DiscordColor(66, 134, 244),        // Blue
            MatchStage.DeckSubmission => new DiscordColor(255, 140, 0), // Orange
            MatchStage.DeckRevision => new DiscordColor(255, 215, 0),   // Gold
            MatchStage.GameResults => new DiscordColor(75, 181, 67),    // Green
            MatchStage.Completed => new DiscordColor(100, 100, 100),    // Gray
            _ => new DiscordColor(75, 181, 67)                          // Default green
        };
    }

    protected string GetStageInstructions()
    {
        return Match.Stage switch
        {
            MatchStage.MapBan => "Select maps to ban in order of priority.",
            MatchStage.DeckSubmission => "Submit your deck using `/match submit_deck`.",
            MatchStage.DeckRevision => "Please submit your revised deck using `/match submit_deck`.",
            MatchStage.GameResults => "Select the winner from the dropdown below.",
            MatchStage.Completed => $"Match completed! Winner: {GetTeamName(Match.WinnerId!)}",
            _ => string.Empty
        };
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