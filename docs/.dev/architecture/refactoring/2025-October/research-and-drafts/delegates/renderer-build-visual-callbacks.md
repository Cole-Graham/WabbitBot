## Renderer “build visual” callbacks (DiscBot DSharpPlus layer)

### Goal
- Centralize send/edit flows while letting callers supply a `Func<CancellationToken, Task<VisualBuildResult>>` that builds the content.
- Enforce URL policy and attachment handling via existing helpers.

### Existing primitives
`VisualBuildResult` DTO:

```csharp
// 1:31:src/WabbitBot.DiscBot/DSharpPlus/ComponentModels/VisualBuildResult.cs
using DSharpPlus.Entities;

namespace WabbitBot.DiscBot.DSharpPlus.ComponentModels;

/// <summary>
/// Result of building a visual component from a POCO model.
/// Supports both modern Container pattern and legacy Embed pattern.
/// </summary>
public record VisualBuildResult
{
    /// <summary>
    /// Modern container component (primary UI pattern).
    /// Supports rich layouts, interactive elements, and theming.
    /// Null if using Embed pattern instead.
    /// </summary>
    public DiscordContainerComponent? Container { get; init; }

    /// <summary>
    /// Legacy embed component (simple interaction responses only).
    /// Reserved for future simple displays per Discord best practices.
    /// Currently not in use - all displays use Container pattern.
    /// Null if using Container pattern instead.
    /// </summary>
    public DiscordEmbed? Embed { get; init; }

    /// <summary>
    /// Optional file attachment hint for assets (e.g., map thumbnails, deck images).
    /// When present, the Renderer should attach the file and reference it via attachment:// URL.
    /// </summary>
    public AttachmentHint? Attachment { get; init; }
```

Message builder extension with URL policy and attachment handling:

```csharp
    // 29:61:src/WabbitBot.DiscBot/DSharpPlus/ComponentModels/DiscordMessageBuilderExtensions.cs
    public static async Task<DiscordMessageBuilder> WithVisual(
        this DiscordMessageBuilder builder,
        VisualBuildResult visual)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(visual);

        // Step 5l: Validate URLs before adding to builder
        ValidateVisualUrls(visual);

        // Add the visual component (Container or Embed)
        if (visual.Container is not null)
        {
            builder.AddContainerComponent(visual.Container);
        }
        else if (visual.Embed is not null)
        {
            builder.AddEmbed(visual.Embed);
        }
        else
        {
            throw new InvalidOperationException(
                "VisualBuildResult must have either Container or Embed set, but both were null.");
        }

        // Step 3i: Handle attachments when CDN URL not available
        if (visual.Attachment is not null)
        {
            await AddAttachmentAsync(builder, visual.Attachment);
        }

        return builder;
    }
```

### Delegate-based helper
```csharp
public static class RendererOps
{
    public static async Task<Result> SendWithVisualAsync(
        Func<CancellationToken, Task<VisualBuildResult>> buildVisual,
        Func<DiscordMessageBuilder, Task> sendAsync,
        CancellationToken ct)
    {
        try
        {
            var visual = await buildVisual(ct);
            var builder = new DiscordMessageBuilder();
            await builder.WithVisual(visual);
            await sendAsync(builder);
            return Result.CreateSuccess();
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                "SendWithVisual failed",
                nameof(SendWithVisualAsync));
            return Result.Failure($"Send failed: {ex.Message}");
        }
    }
}
```

### Concrete usage from MatchRenderer
Replace inline container creation with a callback, keeping policy centralized:

```csharp
// 10:53:src/WabbitBot.DiscBot/DSharpPlus/Renderers/MatchRenderer.cs
public static class MatchRenderer
{
    public static async Task<Result> RenderMatchThreadAsync(
        DiscordClient client,
        DiscordChannel channel,
        Guid matchId)
    {
        try
        {
            var threadName = $"Match {matchId:N}";
            var thread = await channel.CreateThreadAsync(
                threadName,
                DiscordAutoArchiveDuration.Day,
                DiscordChannelType.PublicThread);

            await DiscBotService.PublishAsync(new MatchThreadCreated(
                matchId,
                thread.Id));

            return Result.CreateSuccess("Match thread created");
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                $"Failed to render match thread for {matchId}",
                nameof(RenderMatchThreadAsync));
            return Result.Failure($"Failed to create match thread: {ex.Message}");
        }
    }
```

Callback-based message send in the same renderer (conceptual):

```csharp
public static async Task<Result> RenderMatchContainerAsync(
    DiscordClient client,
    DiscordChannel thread,
    Guid matchId,
    CancellationToken ct = default)
{
    return await RendererOps.SendWithVisualAsync(
        async token =>
        {
            // Build container model and map assets via DiscBotService.AssetResolver if needed
            var components = new List<DiscordComponent>
            {
                new DiscordButtonComponent(DiscordButtonStyle.Primary, $"start_match_{matchId}", "Start Match"),
                new DiscordButtonComponent(DiscordButtonStyle.Danger, $"cancel_match_{matchId}", "Cancel Match"),
                new DiscordTextDisplayComponent($"**Match** {matchId}")
            };
            var container = new DiscordContainerComponent(components);
            return VisualBuildResult.FromContainer(container);
        },
        builder => thread.SendMessageAsync(builder),
        ct);
}
```

### Benefits
- **Single policy point**: URL validation, attachments, and error logging are centralized.
- **Composability**: Callers focus on building models; sending stays uniform.
- **Boundary safety**: Entire pattern stays under `src/WabbitBot.DiscBot/DSharpPlus/`.


