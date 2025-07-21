using DSharpPlus.Entities;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Scrimmages;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

public abstract class ScrimmageEmbed : MatchEmbed
{
    protected Scrimmage Scrimmage { get; private set; } = null!;

    public void SetScrimmage(Scrimmage scrimmage)
    {
        Scrimmage = scrimmage;
        UpdateEmbed();
    }

    protected override void UpdateEmbed()
    {
        SetTitle("Scrimmage Match");
        SetDescription(GetMatchProgressBar());
        SetColor(GetStageColor());

        // Add team information
        AddField("Team 1", GetTeamPlayers(1), true);
        AddField("Team 2", GetTeamPlayers(2), true);

        // Add stage-specific instructions
        AddField("📝 Instructions", GetStageInstructions(), false);
    }
}

public class ScrimmageMapBanEmbed : ScrimmageEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle("Scrimmage - Map Ban Phase");
        SetDescription("Teams must ban maps in alternating order.");
    }
}

public class ScrimmageDeckSubmissionEmbed : ScrimmageEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle("Scrimmage - Deck Submission");
        SetDescription("Teams must submit their decks for the match.");
    }
}

public class ScrimmageDeckRevisionEmbed : ScrimmageEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle("Scrimmage - Deck Revision");
        SetDescription("Teams may revise their decks based on opponent submissions.");
    }
}

public class ScrimmageGameResultsEmbed : ScrimmageEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle("Scrimmage - Game Results");
        SetDescription("Submit the results of the match.");
    }
}

public class ScrimmageMatchCompletedEmbed : ScrimmageEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle("Scrimmage - Completed");
        SetDescription($"Winner: {GetTeamName(Match.WinnerId!)}");

        // Add rating change information from scrimmage
        if (Scrimmage.Status == ScrimmageStatus.Completed)
        {
            AddField("Team 1 Rating Change", $"{Scrimmage.Team1RatingChange:+#0.0;-#0.0;+0.0}", true);
            AddField("Team 2 Rating Change", $"{Scrimmage.Team2RatingChange:+#0.0;-#0.0;+0.0}", true);
        }
    }
}