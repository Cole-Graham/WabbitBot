# Asset Integration Guide for Renderers

**Last Updated:** October 3, 2025  
**Status:** Step 3 (3g-3l) Complete

---

## Overview

This guide explains how to integrate asset handling (map thumbnails, division icons) into Discord renderers using the newly implemented asset management system.

## Architecture Flow

```
Renderer → AssetResolver → GlobalEventBus → Core FileSystemService
    ↓                                              ↓
    ├─ CDN URL available? ────────────────────────┘
    │  └─ Use directly in container
    │
    └─ CDN not available?
       └─ Get AttachmentHint
          └─ WithVisual() loads file
             └─ Attach to message
                └─ CdnCapture reports URL back to Core
```

## Step-by-Step Integration

### 1. Resolve Asset Using AssetResolver

```csharp
using WabbitBot.DiscBot.App.Services;

// Resolve map thumbnail
var (cdnUrl, attachmentHint) = await AssetResolver.ResolveMapThumbnailAsync("templegarden");

// Or resolve division icon
var (cdnUrl, attachmentHint) = await AssetResolver.ResolveDivisionIconAsync("bronze");
```

**Returns:**
- `cdnUrl` (string?): Discord CDN URL if previously uploaded
- `attachmentHint` (AttachmentHint?): Hint for local file if CDN not available
- `(null, null)`: Asset not found

### 2. Build Container with Resolved Asset

**Option A: CDN URL Available (Preferred)**

```csharp
if (cdnUrl is not null)
{
    // Use CDN URL directly in container text/components
    var text = $"Map: {mapName}\n![Thumbnail]({cdnUrl})";
    
    var components = new List<DiscordComponent>
    {
        new DiscordTextDisplayComponent(text),
        // ... other components
    };
    
    var container = new DiscordContainerComponent(components);
}
```

**Option B: Local File Upload (Fallback)**

```csharp
if (attachmentHint is not null)
{
    // Reference file via attachment:// scheme
    var text = $"Map: {mapName}\n![Thumbnail](attachment://{attachmentHint.CanonicalFileName})";
    
    var components = new List<DiscordComponent>
    {
        new DiscordTextDisplayComponent(text),
        // ... other components
    };
    
    var container = new DiscordContainerComponent(components);
}
```

### 3. Package in VisualBuildResult

```csharp
var visual = VisualBuildResult.FromContainer(
    container,
    attachment: attachmentHint); // Pass hint for file attachment
```

### 4. Send Message with WithVisual()

```csharp
var message = await channel.SendMessageAsync(
    await new DiscordMessageBuilder().WithVisual(visual));
```

**What WithVisual() Does:**
- Validates all URLs (prevents internal file paths)
- If `attachmentHint` is present:
  - Loads file from `AppContext.BaseDirectory/data/...`
  - Attaches to message
- Sends message to Discord

### 5. Capture CDN URL After Send

```csharp
// After message is sent, capture CDN URLs
await CdnCapture.CaptureFromMessageAsync(
    message,
    canonicalFileName: attachmentHint?.CanonicalFileName);
```

**What CdnCapture Does:**
- Extracts CDN URLs from message attachments
- Reports to Core via `FileCdnLinkReported` event
- Core caches URL for future use (prefer CDN next time)

---

## Complete Renderer Example

```csharp
using DSharpPlus;
using DSharpPlus.Entities;
using WabbitBot.Common.Models;
using WabbitBot.DiscBot.App.Events;
using WabbitBot.DiscBot.App.Services;
using WabbitBot.DiscBot.App.Services.DiscBot;
using WabbitBot.DiscBot.DSharpPlus.Utilities;

namespace WabbitBot.DiscBot.DSharpPlus.Renderers;

public class GameRenderer
{
    public void Initialize()
    {
        DiscBotService.EventBus.Subscribe<GameContainerRequested>(HandleGameContainerRequestedAsync);
    }

    private async Task<Result> HandleGameContainerRequestedAsync(GameContainerRequested evt)
    {
        try
        {
            var client = DiscordClientProvider.GetClient();
            
            // TODO: Get thread ID from match state tracker
            // var thread = await client.GetChannelAsync(evt.ThreadId);

            // Step 1: Resolve map thumbnail asset
            var (cdnUrl, attachmentHint) = await AssetResolver.ResolveMapThumbnailAsync(evt.ChosenMap);

            // Step 2: Build container text with asset reference
            string thumbnailMarkdown;
            if (cdnUrl is not null)
            {
                thumbnailMarkdown = $"![Map Thumbnail]({cdnUrl})";
            }
            else if (attachmentHint is not null)
            {
                thumbnailMarkdown = $"![Map Thumbnail](attachment://{attachmentHint.CanonicalFileName})";
            }
            else
            {
                thumbnailMarkdown = "_(Thumbnail unavailable)_";
            }

            var text = $@"
**Game {evt.GameNumber}**

Map: **{evt.ChosenMap}**
{thumbnailMarkdown}

Please play the game and upload the replay when finished.
";

            // Step 3: Build container components
            var uploadButton = new DiscordButtonComponent(
                DiscordButtonStyle.Primary,
                $"upload_replay_{evt.MatchId}_{evt.GameNumber}",
                "Upload Replay");

            var components = new List<DiscordComponent>
            {
                new DiscordTextDisplayComponent(text),
                uploadButton,
            };

            var container = new DiscordContainerComponent(components);

            // Step 4: Package in VisualBuildResult with optional attachment
            var visual = VisualBuildResult.FromContainer(container, attachment: attachmentHint);

            // Step 5: Send message (WithVisual handles file attachment)
            // var message = await thread.SendMessageAsync(
            //     await new DiscordMessageBuilder().WithVisual(visual));

            // Step 6: Capture CDN URL after send
            // await CdnCapture.CaptureFromMessageAsync(message, attachmentHint?.CanonicalFileName);

            return Result.CreateSuccess("Game container created");
        }
        catch (Exception ex)
        {
            await DiscBotService.ErrorHandler.CaptureAsync(
                ex,
                $"Failed to create game container for match {evt.MatchId}, game {evt.GameNumber}",
                nameof(HandleGameContainerRequestedAsync));
            return Result.Failure($"Failed to create game container: {ex.Message}");
        }
    }
}
```

---

## Best Practices

### 1. Asset Resolution

**DO:**
- ✅ Always await `AssetResolver` methods
- ✅ Handle both CDN URL and AttachmentHint cases
- ✅ Provide fallback text if asset not found
- ✅ Use specific methods (`ResolveMapThumbnailAsync`, `ResolveDivisionIconAsync`)

**DON'T:**
- ❌ Access Core's file paths directly
- ❌ Assume CDN URLs are always available
- ❌ Skip error handling
- ❌ Forget to capture CDN URLs after send

### 2. URL References

**DO:**
- ✅ Use `attachment://filename` for local files
- ✅ Use HTTPS CDN URLs directly
- ✅ Let `WithVisual()` handle validation

**DON'T:**
- ❌ Use internal file paths (`C:\...`, `/var/...`)
- ❌ Use HTTP (insecure)
- ❌ Use file:// URIs

### 3. CDN Capture

**DO:**
- ✅ Call `CdnCapture.CaptureFromMessageAsync()` after every send with attachments
- ✅ Pass canonical filename if known
- ✅ Await the capture call

**DON'T:**
- ❌ Skip CDN capture (Core won't cache URLs)
- ❌ Throw if capture fails (it's non-critical)

### 4. Error Handling

**DO:**
- ✅ Wrap asset resolution in try-catch
- ✅ Log errors via `DiscBotService.ErrorHandler`
- ✅ Provide fallback content if assets unavailable
- ✅ Return `Result.Failure()` for critical errors

**DON'T:**
- ❌ Let exceptions crash the renderer
- ❌ Display internal error details to Discord users
- ❌ Retry indefinitely (use timeout)

---

## Performance Considerations

### Asset Resolution Timeout

Default timeout is 5 seconds. Customize if needed:

```csharp
var (cdnUrl, hint) = await AssetResolver.ResolveAssetAsync(
    "mapthumbnail",
    "templegarden",
    timeout: TimeSpan.FromSeconds(10));
```

### File Size Limits

Discord has attachment size limits:
- **Free servers:** 25 MB
- **Boosted servers:** 50-100 MB

Map thumbnails and icons should be well under these limits.

### CDN Caching

Once a file is uploaded and CDN URL is captured:
- **First upload:** Full file upload (~1-2 seconds for images)
- **Subsequent uses:** Instant CDN URL reference

The system automatically prefers CDN URLs when available.

---

## Troubleshooting

### Asset Not Found

```
(null, null) returned from AssetResolver
```

**Causes:**
- File doesn't exist in Core's FileSystemService
- Asset ID is incorrect
- File system permissions issue

**Solution:**
- Verify asset exists in `data/maps/thumbnails/` or `data/divisions/icons/`
- Check spelling of asset ID
- Ensure Core has read access to files

### CDN URL Not Captured

```
AssetResolver still returns AttachmentHint after multiple uploads
```

**Causes:**
- `CdnCapture.CaptureFromMessageAsync()` not called
- Message send failed before CDN URL was assigned
- GlobalEventBus not routing events

**Solution:**
- Always call `CdnCapture` after successful send
- Check GlobalEventBus is initialized
- Verify `FileCdnLinkReported` is published

### URL Validation Errors

```
InvalidOperationException: Embed thumbnail URL appears to be an internal file path
```

**Causes:**
- Trying to use file paths directly
- Not using `attachment://` scheme

**Solution:**
- Use CDN URL if available
- Use `attachment://filename` for local files
- Let `WithVisual()` handle validation

---

## Architecture Notes

### Shared File System

Both Core and DiscBot share `AppContext.BaseDirectory`. This allows:
- Core manages files in `data/` subdirectories
- DiscBot reads files for upload
- No need for HTTP endpoints or byte[] transfers

### Event-Driven CDN Tracking

CDN URL capture is asynchronous and non-blocking:
- Renderer reports CDN URL via event
- Core updates cache in background
- No impact on message send performance

### Request-Response Pattern

`AssetResolver` uses `GlobalEventBus.RequestAsync`:
- Correlation ID tracking
- 5-second timeout
- Type-safe request/response

---

## API Reference

### AssetResolver

```csharp
Task<(string? cdnUrl, AttachmentHint? attachmentHint)> ResolveAssetAsync(string assetType, string assetId, TimeSpan? timeout = null)
Task<(string? cdnUrl, AttachmentHint? attachmentHint)> ResolveMapThumbnailAsync(string mapName)
Task<(string? cdnUrl, AttachmentHint? attachmentHint)> ResolveDivisionIconAsync(string divisionRank)
```

### CdnCapture

```csharp
Task CaptureFromMessageAsync(DiscordMessage message, string? canonicalFileName = null)
```

### WithVisual Extension

```csharp
Task<DiscordMessageBuilder> WithVisual(this DiscordMessageBuilder builder, VisualBuildResult visual)
```

### VisualBuildResult

```csharp
VisualBuildResult FromContainer(DiscordContainerComponent container, AttachmentHint? attachment = null)
VisualBuildResult FromEmbed(DiscordEmbed embed, AttachmentHint? attachment = null)
```

### AttachmentHint

```csharp
AttachmentHint ForImage(string canonicalFileName)
```

---

## Related Documentation

- **Architecture Plan:** `docs/.dev/architecture/refactoring/2025-October/plan.md`
- **Step 3 Log:** `docs/.dev/architecture/refactoring/2025-October/2025-October-logs/step-3-dsharpplus-layer.md`
- **Step 5k-5l Log:** `docs/.dev/architecture/refactoring/2025-October/2025-October-logs/step-5-wiring-and-startup-completion.md`
- **Step 7 Log:** `docs/.dev/architecture/refactoring/2025-October/2025-October-logs/step-7-error-handling-and-assets.md`
- **Asset Events:** `src/WabbitBot.Common/Events/AssetEvents.cs`
- **FileSystem Events:** `src/WabbitBot.Core/Common/Events/FileSystemEvents.cs`

