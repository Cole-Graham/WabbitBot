using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace WabbitBot.SourceGenerators.Generators.Embed;

[Generator]
public class EmbedStylingGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new EmbedStylingSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Get the syntax receiver
        if (context.SyntaxReceiver is not EmbedStylingSyntaxReceiver receiver)
            return;

        // Only generate if we found classes with GenerateEmbedStyling attributes
        if (!receiver.EmbedStylingClasses.Any())
            return;

        // Generate the styling utilities code
        var sourceBuilder = new StringBuilder(@"
using DSharpPlus.Entities;
using WabbitBot.Core.Matches;
using WabbitBot.Core.Scrimmages;
using WabbitBot.Core.Common.Models;

namespace WabbitBot.DiscBot.DSharpPlus.Generated;

/// <summary>
/// Generated styling utilities for Discord embeds. This class is generated at compile time.
/// Provides consistent styling, colors, and formatting across all embeds.
/// </summary>
public static class EmbedStyling
{
    #region Universal Styling Utilities
    
    /// <summary>
    /// Gets the default embed color for general use
    /// </summary>
    public static DiscordColor GetDefaultColor() => new DiscordColor(66, 134, 244); // Blue

    /// <summary>
    /// Gets the success color for positive actions
    /// </summary>
    public static DiscordColor GetSuccessColor() => new DiscordColor(75, 181, 67); // Green

    /// <summary>
    /// Gets the warning color for pending actions
    /// </summary>
    public static DiscordColor GetWarningColor() => new DiscordColor(255, 140, 0); // Orange

    /// <summary>
    /// Gets the error color for negative actions
    /// </summary>
    public static DiscordColor GetErrorColor() => new DiscordColor(237, 66, 69); // Red

    /// <summary>
    /// Gets the info color for neutral information
    /// </summary>
    public static DiscordColor GetInfoColor() => new DiscordColor(66, 134, 244); // Blue

    /// <summary>
    /// Gets the gold color for special events and completions
    /// </summary>
    public static DiscordColor GetGoldColor() => new DiscordColor(255, 215, 0); // Gold

    /// <summary>
    /// Gets the gray color for neutral or disabled states
    /// </summary>
    public static DiscordColor GetGrayColor() => new DiscordColor(100, 100, 100); // Gray

    /// <summary>
    /// Formats a team name with consistent styling
    /// </summary>
    public static string FormatTeamName(string teamName) => $""**{teamName}**"";

    /// <summary>
    /// Formats a timestamp for Discord relative time display
    /// </summary>
    public static string FormatTimestamp(DateTime timestamp) => $""<t:{((DateTimeOffset)timestamp).ToUnixTimeSeconds()}:R"";

    /// <summary>
    /// Formats a timestamp for Discord absolute time display
    /// </summary>
    public static string FormatAbsoluteTimestamp(DateTime timestamp) => $""<t:{((DateTimeOffset)timestamp).ToUnixTimeSeconds()}:F"";

    /// <summary>
    /// Formats a rating change with appropriate color and sign (float version)
    /// </summary>
    public static string FormatRatingChange(float ratingChange)
    {
        var sign = ratingChange >= 0 ? ""+"" : """";
        var color = ratingChange >= 0 ? ""🟢"" : ""🔴"";
        return $""{color} {sign}{ratingChange:F1}"";
    }

    /// <summary>
    /// Formats game size for display
    /// </summary>
    public static string FormatEvenTeamFormat(EvenTeamFormat evenTeamFormat) => $""**{evenTeamFormat}**"";

    #endregion

    #region Scrimmage-Specific Utilities

    /// <summary>
    /// Gets the appropriate emoji for scrimmage status
    /// </summary>
    public static string GetScrimmageStatusEmoji(ScrimmageStatus status) => status switch
    {
        ScrimmageStatus.Created => ""⏳"",
        ScrimmageStatus.Accepted => ""✅"",
        ScrimmageStatus.InProgress => ""🎮"",
        ScrimmageStatus.Completed => ""🏆"",
        ScrimmageStatus.Declined => ""❌"",
        ScrimmageStatus.Cancelled => ""⏰"",
        ScrimmageStatus.Forfeited => ""🚫"",
        _ => ""❓""
    };

    /// <summary>
    /// Gets the appropriate color for scrimmage status
    /// </summary>
    public static DiscordColor GetScrimmageStatusColor(ScrimmageStatus status) => status switch
    {
        ScrimmageStatus.Created => GetWarningColor(),
        ScrimmageStatus.Accepted => GetInfoColor(),
        ScrimmageStatus.InProgress => GetDefaultColor(),
        ScrimmageStatus.Completed => GetSuccessColor(),
        ScrimmageStatus.Declined => GetErrorColor(),
        ScrimmageStatus.Cancelled => GetGrayColor(),
        ScrimmageStatus.Forfeited => GetErrorColor(),
        _ => GetGrayColor()
    };

    #endregion

    #region Match-Specific Utilities

    /// <summary>
    /// Gets the color for a match state
    /// </summary>
    public static DiscordColor GetMatchStateColor(MatchState state) => state switch
    {
        MatchState.Created => GetGrayColor(),
        MatchState.InProgress => GetInfoColor(),
        MatchState.Completed => GetSuccessColor(),
        MatchState.Cancelled => GetErrorColor(),
        MatchState.Forfeited => GetWarningColor(),
        _ => GetGrayColor()
    };

    /// <summary>
    /// Gets the color for a match action phase
    /// </summary>
    public static DiscordColor GetMatchActionColor(string action) => action switch
    {
        var a when a.Contains(""Map banning"") => GetInfoColor(),
        var a when a.Contains(""Deck submission"") => GetDefaultColor(),
        var a when a.Contains(""Deck revision"") => GetWarningColor(),
        var a when a.Contains(""Game results"") => GetDefaultColor(),
        var a when a.Contains(""completed"") => GetSuccessColor(),
        var a when a.Contains(""cancelled"") => GetErrorColor(),
        var a when a.Contains(""forfeited"") => GetWarningColor(),
        _ => GetGrayColor()
    };

    /// <summary>
    /// Formats match progress for display (single parameter version)
    /// </summary>
    public static string FormatMatchProgress(MatchState state) => state switch
    {
        MatchState.Created => ""Match created - waiting to start"",
        MatchState.InProgress => ""Match in progress"",
        MatchState.Completed => ""Match completed"",
        MatchState.Cancelled => ""Match cancelled"",
        MatchState.Forfeited => ""Match forfeited"",
        _ => ""Unknown state""
    };

    /// <summary>
    /// Formats match progress for display (two parameter version)
    /// </summary>
    public static string FormatMatchProgress(int currentGame, int totalGames) => $""Game {currentGame}/{totalGames}"";

    /// <summary>
    /// Formats stage instructions for display based on action
    /// </summary>
    public static string FormatStageInstructions(string action) => action switch
    {
        var a when a.Contains(""Map banning"") => ""Teams are banning maps..."",
        var a when a.Contains(""Deck submission"") => ""Teams are submitting decks..."",
        var a when a.Contains(""Deck revision"") => ""Teams are revising decks..."",
        var a when a.Contains(""Game results"") => ""Match in progress..."",
        var a when a.Contains(""completed"") => ""Match completed!"",
        var a when a.Contains(""cancelled"") => ""Match cancelled"",
        var a when a.Contains(""forfeited"") => ""Match forfeited"",
        _ => ""Unknown stage""
    };

    /// <summary>
    /// Formats score for display (single parameter version)
    /// </summary>
    public static string FormatScore(int score) => $""**{score}**"";

    /// <summary>
    /// Formats score for display (two parameter version)
    /// </summary>
    public static string FormatScore(int score1, int score2) => $""**{score1} - {score2}**"";

    /// <summary>
    /// Formats a rating change with appropriate color and sign (double version)
    /// </summary>
    public static string FormatRatingChange(double ratingChange)
    {
        var sign = ratingChange >= 0 ? ""+"" : """";
        var color = ratingChange >= 0 ? ""🟢"" : ""🔴"";
        return $""{color} {sign}{ratingChange:F1}"";
    }

    #endregion
}
");

        // Use a unique filename to avoid conflicts during multiple invocations
        var fileName = "EmbedStyling.g.cs";
        var sourceText = sourceBuilder.ToString();

        // Check if this source has already been added to avoid duplicates
        if (!context.Compilation.SyntaxTrees.Any(tree => tree.FilePath.EndsWith(fileName)))
        {
            context.AddSource(fileName, sourceText);
        }
    }
}

public class EmbedStylingSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> EmbedStylingClasses { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classDeclaration)
        {
            // Only add classes that have GenerateEmbedStyling attributes
            if (HasGenerateEmbedStylingAttribute(classDeclaration))
            {
                EmbedStylingClasses.Add(classDeclaration);
            }
        }
    }

    private bool HasGenerateEmbedStylingAttribute(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.AttributeLists
            .SelectMany(attrList => attrList.Attributes)
            .Any(attr =>
            {
                var attrName = attr.Name.ToString();
                return attrName.Contains("GenerateEmbedStyling") ||
                       attrName.EndsWith("GenerateEmbedStyling") ||
                       attrName.EndsWith("GenerateEmbedStylingAttribute");
            });
    }
}