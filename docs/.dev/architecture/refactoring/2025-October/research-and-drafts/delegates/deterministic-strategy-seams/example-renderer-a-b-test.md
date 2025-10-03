## Example: Renderer A/B toggle for CDN vs Attachment preference

### Current building blocks
`AssetResolver` returns either a CDN URL or an `AttachmentHint` for upload fallback:

```csharp
// 8:26:src/WabbitBot.DiscBot/App/Services/DiscBot/DiscBotService.AssetResolver.cs
public static async Task<(string? cdnUrl, AttachmentHint? attachmentHint)> ResolveAssetAsync(
    string assetType,
    string assetId,
    TimeSpan? timeout = null)
{
    // ... request AssetResolveRequested and await AssetResolved ...
}
```

`DiscordMessageBuilderExtensions.WithVisual` enforces URL policy and attaches files when needed:

```csharp
// 29:61:src/WabbitBot.DiscBot/DSharpPlus/ComponentModels/DiscordMessageBuilderExtensions.cs
public static async Task<DiscordMessageBuilder> WithVisual(
    this DiscordMessageBuilder builder,
    VisualBuildResult visual)
{
    // validates URLs and attaches files when AttachmentHint present
}
```

### Delegate seam
Allow callers to decide which variant to prefer (A vs B) without changing renderer structure.

```csharp
public static class AssetChoice
{
    // A: Prefer CDN when available
    public static (string? cdnUrl, AttachmentHint? attachment) PreferCdn(
        (string? cdnUrl, AttachmentHint? attachment) resolved) => resolved;

    // B: Force attachment upload (useful for testing attachment rendering or CDN policy)
    public static (string? cdnUrl, AttachmentHint? attachment) PreferAttachment(
        (string? cdnUrl, AttachmentHint? attachment) resolved)
        => (null, resolved.attachment);
}

public static class VisualBuilders
{
    public static async Task<VisualBuildResult> BuildMapPanelAsync(
        string mapName,
        Func<(string? cdnUrl, AttachmentHint? attachment), (string? cdnUrl, AttachmentHint? attachment)> choose,
        CancellationToken ct)
    {
        var resolved = await DiscBotService.AssetResolver.ResolveMapThumbnailAsync(mapName);
        var choice = choose(resolved);

        var components = new List<DiscordComponent>
        {
            new DiscordTextDisplayComponent($"Map: {mapName}"),
        };

        // CDN URL would typically be placed inside an embed image or container component metadata
        var container = new DiscordContainerComponent(components);
        var attachment = choice.attachment;
        return VisualBuildResult.FromContainer(container, attachment);
    }
}
```

### Concrete A/B usage in a renderer

```csharp
public static Task<Result> RenderMapPanelAsync(
    DiscordChannel channel,
    string mapName,
    bool preferCdn,
    CancellationToken ct)
{
    return RendererOps.SendWithVisualAsync(
        buildVisual: token => VisualBuilders.BuildMapPanelAsync(
            mapName,
            preferCdn ? AssetChoice.PreferCdn : AssetChoice.PreferAttachment,
            token),
        sendAsync: builder => channel.SendMessageAsync(builder),
        ct: ct);
}
```

### Why this helps
- Enables A/B testing of CDN vs attachment flows without touching global config.
- Keeps all logic inside the DSharpPlus layer, honoring boundary rules.
- Works seamlessly with `WithVisual` URL policy enforcement and attachment handling.


