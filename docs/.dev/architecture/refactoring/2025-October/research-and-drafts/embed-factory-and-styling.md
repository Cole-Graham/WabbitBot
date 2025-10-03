### Analysis: Old Embed Factory and Styling Attributes

- Attribute definitions exist for both factory and styling:
```csharp
using System;

namespace WabbitBot.Common.Attributes
{
    #region Embed
    /// <summary>
    /// Marks a class for embed factory generation. Classes marked with this attribute
    /// will have factory methods generated to create instances of the embed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateEmbedFactoryAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks a class for embed styling utilities generation. Classes marked with this attribute
    /// will have styling utilities generated to provide consistent styling across all embeds.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateEmbedStylingAttribute : Attribute
    {
    }
    #endregion
}
```

- Base embed uses the styling attribute, and provides a consistent ToEmbedBuilder path:
```csharp
using DSharpPlus.Entities;
using WabbitBot.Common.Attributes;

namespace WabbitBot.DiscBot.DSharpPlus.Embeds;

/// <summary>
/// Base class for all embeds in the bot. Provides common properties and functionality
/// for consistent styling and behavior across different types of embeds.
/// </summary>
[GenerateEmbedStyling]
public abstract class BaseEmbed
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DiscordColor Color { get; set; } = DiscordColor.Blue;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string FooterText { get; set; } = string.Empty;
    public string? FooterIconUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ImageUrl { get; set; }
    public IEmbedAuthor? Author { get; set; }
    public IReadOnlyList<IEmbedField> Fields { get; protected set; } = new List<IEmbedField>();

    public virtual DiscordEmbedBuilder ToEmbedBuilder()
    {
        var builder = new DiscordEmbedBuilder()
            .WithTitle(Title)
            .WithDescription(Description)
            .WithColor(Color)
            .WithTimestamp(Timestamp);

        if (!string.IsNullOrEmpty(FooterText))
        {
            builder.WithFooter(FooterText, FooterIconUrl);
        }

        if (!string.IsNullOrEmpty(ThumbnailUrl))
        {
            builder.WithThumbnail(ThumbnailUrl);
        }

        if (!string.IsNullOrEmpty(ImageUrl))
        {
            builder.WithImageUrl(ImageUrl);
        }

        if (Author != null)
        {
            builder.WithAuthor(Author.Name, Author.IconUrl, Author.Url);
        }

        foreach (var field in Fields)
        {
            builder.AddField(field.Name, field.Value, field.IsInline);
        }

        return builder;
    }
}
```

- The factory generator discovers `[GenerateEmbedFactory]` classes (inheriting the old `BaseEmbed`) and emits a shared factory source:
```csharp
/// <summary>
/// Generates embed factory code for classes marked with [GenerateEmbedFactory].
/// Creates a static factory for instantiating and managing Discord embeds.
/// </summary>
[Generator]
public class EmbedFactoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var isDiscBotProject = context.CompilationProvider.IsDiscBot();
        // Pipeline: Filter classes with [GenerateEmbedFactory] attribute that inherit BaseEmbed
        var embedClasses = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: (node, _) => node.HasAttribute("GenerateEmbedFactory"),
                transform: (ctx, ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(ctx.Node) as INamedTypeSymbol;
                    if (classSymbol != null &&
                        classSymbol.BaseType?.ToDisplayString() == "WabbitBot.DiscBot.DSharpPlus.Embeds.BaseEmbed")
                    {
                        return classSymbol.Name;
                    }
                    return null;
                })
            .Where(name => name != null)
            .Collect();

        // Generate factory class
        var factorySource = embedClasses.Select((classes, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return EmbedTemplates.GenerateFactory(classes.Where(c => c != null).Cast<string>().ToList());
        });

        context.RegisterSourceOutput(factorySource.Combine(isDiscBotProject), (spc, tuple) =>
        {
            if (!tuple.Right)
                return;
            spc.AddSource("EmbedFactories.g.cs", tuple.Left);
        });
    }
}
```

- Styling was a manual static utility, not actually generated (no styling generator found). It centralizes colors, display labels, and formatting helpers:
```csharp
namespace WabbitBot.DiscBot.DSharpPlus;

/// <summary>
/// Static styling utilities for Discord embeds.
/// Provides consistent styling, colors, and formatting across all embeds.
/// </summary>
public static class EmbedStyling
{
    // Universal Styling Utilities
    public static DiscordColor GetDefaultColor() => new DiscordColor(66, 134, 244);
    public static DiscordColor GetSuccessColor() => new DiscordColor(76, 175, 80);
    public static DiscordColor GetWarningColor() => new DiscordColor(255, 152, 0);
    public static DiscordColor GetErrorColor() => new DiscordColor(244, 67, 54);
    public static DiscordColor GetInfoColor() => new DiscordColor(33, 150, 243);

    // Match-Specific Styling (excerpt)
    public static string GetMatchStatusDisplayName(MatchStatus status) => status switch
    {
        MatchStatus.Created => "⏳ Created",
        MatchStatus.InProgress => "⚡ In Progress",
        MatchStatus.Completed => "✅ Completed",
        MatchStatus.Cancelled => "🚫 Cancelled",
        MatchStatus.Forfeited => "🏳️ Forfeited",
        _ => "❓ Unknown",
    };
}
```

Observations
- The factory generator is coherent: it scans for `[GenerateEmbedFactory]` classes inheriting `BaseEmbed` and emits a central `EmbedFactories.g.cs`. The template isn’t shown, but intent is clear.
- `[GenerateEmbedStyling]` is applied to `BaseEmbed`, but there is no generator for styling. Styling was implemented manually in `EmbedStyling.cs`.
- The old `BaseEmbed` lives under deprecated DSharpPlus structure and mixes concerns (model + direct `DiscordEmbedBuilder` projection + URL fields). It also expects direct URLs, which conflicts with our new embed asset policy (CDN or attachment:// only, never internal paths).

### Recommendation

1) Keep the idea of a generated factory, but adapt it to the new layering
- Place embed models and factories under `src/WabbitBot.DiscBot/DSharpPlus/Embeds`.
- Use `[GenerateEmbedFactory]` on embed model classes that represent the content/view model for a specific message context (e.g., `MatchEmbedModel`, `MapEmbedModel`).
- Generator output: a single `EmbedFactories` class exposing typed `Create*` methods returning `DiscordEmbedBuilder` (or a small struct with `DiscordEmbedBuilder` + attachment hints).
- Do not require inheritance from the old `BaseEmbed`. Instead, make the generator accept POCOs with recognizable properties or a minimal marker interface to avoid rigid hierarchies.

2) Prefer manual centralized styling over a styling generator
- Keep a single manual `EmbedStyling` static utility under `DSharpPlus/Embeds/` for colors, labels, and formatting. This keeps intent obvious and avoids over-generating code that rarely changes.
- Retire `[GenerateEmbedStyling]`. If metadata is desired, introduce small opt-in attributes like `[EmbedTheme(Color="Info")]` that the factory generator can consult, rather than generating a separate styling class.

3) Align factories with asset policy and new event-driven flow
- Generated factory methods must not take raw internal file paths. Instead accept an image descriptor: either a public `cdnUrl` or an `AttachmentSpec` indicating the file name expected to be attached by the Renderer.
- Renderer responsibilities:
  - If `cdnUrl` is available (Core resolved), set embed image to that URL.
  - Otherwise attach the file and set image to `attachment://<canonicalFileName>`.
- This separation keeps factories pure (no IO), and Renderers own Discord-specific attachment behavior.

4) Simplify base abstractions
- Do not reintroduce a `BaseEmbed` inheritance chain. Use plain models + generator. Shared helpers remain in `EmbedStyling` and small utility functions.
- If a base is helpful, make it an interface (e.g., `IEmbedModel`) with no behavior.

5) Source generator adjustments
- Update `EmbedFactoryGenerator` to:
  - Target DiscBot project as today.
  - Recognize `[GenerateEmbedFactory]` on classes in `DSharpPlus/Embeds`, without forcing inheritance on the deprecated `BaseEmbed`.
  - Allow optional per-embed metadata via attributes (e.g., theme hint) and incorporate `EmbedStyling` helpers.
  - Optionally emit overloads that accept either simple primitives or pre-built DTOs.

### How it works end-to-end (proposed)

- App Flow publishes a UI request (e.g., `MatchEmbedRequested`).
- DSharpPlus Renderer handles it:
  - Queries Core via request–response for asset resolution; receives `cdnUrl` or a `canonicalFileName` and `relativePathUnderAppBase` for upload.
  - If no `cdnUrl`, reads the whitelisted path to stream and prepares an attachment.
  - Calls generated factory: `EmbedFactories.CreateMatchEmbed(model, styleHint)` to obtain a `DiscordEmbedBuilder`.
  - If uploading, sets `builder.ImageUrl = "attachment://" + canonicalFileName` and includes the attachment in the message.
  - Sends the message.

Benefits
- No DI; all logic lives inside DiscBot’s DSharpPlus layer as required.
- Clear separation of concerns: models (data), factories (projection), Renderers (Discord API), styling (manual utility).
- Compliant embed asset behavior (no internal paths), and compatible with our event-driven Core ingestion/resolve.

### Keep vs. Change Summary
- Keep: `[GenerateEmbedFactory]` concept and a single generated `EmbedFactories` entry point.
- Change: Do not depend on the deprecated `BaseEmbed` inheritance; stop using `[GenerateEmbedStyling]` (no generator exists). Keep styling manual.
- Add: Attribute-based hints for styling themes if needed; factory signatures designed around `cdnUrl` or `attachment://` patterns.
