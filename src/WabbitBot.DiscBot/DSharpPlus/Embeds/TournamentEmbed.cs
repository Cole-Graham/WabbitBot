using DSharpPlus.Entities;
using WabbitBot.Core.Matches;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

public abstract class TournamentEmbed : MatchEmbed
{
    protected string TournamentId { get; set; } = string.Empty;
    protected string GroupName { get; set; } = string.Empty;
    protected int CurrentRound { get; set; }
    protected int TotalRounds { get; set; }
    protected int GroupStageMatchNumber { get; set; }
    protected int TotalGroupStageMatches { get; set; }

    public void SetTournamentInfo(string tournamentId, string groupName, int currentRound, int totalRounds,
        int groupStageMatchNumber, int totalGroupStageMatches)
    {
        TournamentId = tournamentId;
        GroupName = groupName;
        CurrentRound = currentRound;
        TotalRounds = totalRounds;
        GroupStageMatchNumber = groupStageMatchNumber;
        TotalGroupStageMatches = totalGroupStageMatches;
        UpdateEmbed();
    }

    protected string GetTournamentTitle()
    {
        if (GroupStageMatchNumber > 0 && TotalGroupStageMatches > 0)
        {
            return $"Group Stage ({GroupName}): Match {GroupStageMatchNumber} of {TotalGroupStageMatches}";
        }
        else if (!string.IsNullOrEmpty(GroupName))
        {
            return $"Group Stage ({GroupName}): Round {CurrentRound} of {TotalRounds}";
        }
        else
        {
            return "Tournament Match";
        }
    }

    protected override void UpdateEmbed()
    {
        SetTitle(GetTournamentTitle());
        SetDescription(GetMatchProgressBar());
        SetColor(GetStageColor());

        // Add team information
        AddField("Team 1", GetTeamPlayers(1), true);
        AddField("Team 2", GetTeamPlayers(2), true);

        // Add stage-specific instructions
        AddField("📝 Instructions", GetStageInstructions(), false);
    }
}

public class TournamentMapBanEmbed : TournamentEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle($"{Title} - Map Ban Phase");
        SetDescription("Teams must ban maps in alternating order.");
    }
}

public class TournamentDeckSubmissionEmbed : TournamentEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle($"{Title} - Deck Submission");
        SetDescription("Teams must submit their decks for the match.");
    }
}

public class TournamentDeckRevisionEmbed : TournamentEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle($"{Title} - Deck Revision");
        SetDescription("Teams may revise their decks based on opponent submissions.");
    }
}

public class TournamentGameResultsEmbed : TournamentEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle($"{Title} - Game Results");
        SetDescription("Submit the results of the match.");
    }
}

public class TournamentMatchCompletedEmbed : TournamentEmbed
{
    protected override void UpdateEmbed()
    {
        base.UpdateEmbed();
        SetTitle($"{Title} - Completed");
        SetDescription($"Winner: {GetTeamName(Match.WinnerId!)}");

        // Note: Rating change information is not available on the Match class
        // It was moved to the Scrimmage class and would need to be passed separately
    }
}