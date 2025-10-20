using DSharpPlus.Entities;
using WabbitBot.Common.Events;

namespace WabbitBot.DiscBot.App.Utilities
{
    /// <summary>
    /// Utility for capturing Discord CDN URLs from sent messages and reporting them to Core.
    /// </summary>
    public static partial class CdnCapture
    {
        /// <summary>
        /// Extracts CDN URLs from a Discord message and reports them to Core via GlobalEventBus.
        /// This allows Core's FileSystemService to cache CDN URLs for future use.
        /// </summary>
        /// <param name="message">The Discord message that was sent</param>
        /// <param name="canonicalFileName">The canonical filename that was attached (if known)</param>
        public static async Task CaptureFromMessageAsync(DiscordMessage message, string? canonicalFileName = null)
        {
            if (message is null)
                return;

            try
            {
                var globalBus = GlobalEventBusProvider.GetGlobalEventBus();

                // Capture CDN URLs from attachments
                if (message.Attachments?.Any() ?? false)
                {
                    foreach (var attachment in message.Attachments)
                    {
                        // Use provided canonical filename or extract from attachment filename
                        var fileName = canonicalFileName ?? attachment.FileName;

                        // Skip if filename or URL is null/empty
                        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(attachment.Url))
                        {
                            continue;
                        }

                        // Publish CDN link reported event
                        await globalBus.PublishAsync(
                            new FileCdnLinkReported(
                                CanonicalFileName: fileName,
                                CdnUrl: attachment.Url,
                                SourceMessageId: message.Id,
                                ChannelId: message.ChannelId
                            )
                        );
                    }
                }

                // Capture CDN URLs from embeds (thumbnail, image, author icon, footer icon)
                if (message.Embeds?.Any() ?? false)
                {
                    foreach (var embed in message.Embeds)
                    {
                        // Thumbnail
                        var thumbnailUrl = embed.Thumbnail?.Url?.ToString();
                        if (!string.IsNullOrEmpty(thumbnailUrl))
                        {
                            await ReportCdnUrlAsync(globalBus, thumbnailUrl, message.Id, message.ChannelId);
                        }

                        // Image
                        var imageUrl = embed.Image?.Url?.ToString();
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            await ReportCdnUrlAsync(globalBus, imageUrl, message.Id, message.ChannelId);
                        }

                        // Author icon
                        var authorIconUrl = embed.Author?.IconUrl?.ToString();
                        if (!string.IsNullOrEmpty(authorIconUrl))
                        {
                            await ReportCdnUrlAsync(globalBus, authorIconUrl, message.Id, message.ChannelId);
                        }

                        // Footer icon
                        var footerIconUrl = embed.Footer?.IconUrl?.ToString();
                        if (!string.IsNullOrEmpty(footerIconUrl))
                        {
                            await ReportCdnUrlAsync(globalBus, footerIconUrl, message.Id, message.ChannelId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - CDN capture is non-critical
                await App.Services.DiscBot.DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to capture CDN URLs from message {message.Id}",
                    nameof(CaptureFromMessageAsync)
                );
            }
        }

        /// <summary>
        /// Reports a single CDN URL to Core.
        /// Attempts to extract canonical filename from the URL.
        /// </summary>
        private static async Task ReportCdnUrlAsync(
            IGlobalEventBus globalBus,
            string cdnUrl,
            ulong messageId,
            ulong channelId
        )
        {
            try
            {
                // Extract filename from CDN URL
                var uri = new Uri(cdnUrl);
                var fileName = Path.GetFileName(uri.LocalPath);

                if (string.IsNullOrEmpty(fileName))
                    return;

                await globalBus.PublishAsync(
                    new FileCdnLinkReported(
                        CanonicalFileName: fileName,
                        CdnUrl: cdnUrl,
                        SourceMessageId: messageId,
                        ChannelId: channelId
                    )
                );
            }
            catch (Exception ex)
            {
                // Log error but don't throw - CDN capture is best-effort
                await App.Services.DiscBot.DiscBotService.ErrorHandler.CaptureAsync(
                    ex,
                    $"Failed to capture CDN URL {cdnUrl}",
                    nameof(ReportCdnUrlAsync)
                );
            }
        }
    }
}
