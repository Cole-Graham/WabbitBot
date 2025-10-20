using WabbitBot.Common.Events;

namespace WabbitBot.Core.Common.Services;

/// <summary>
/// Event handlers for FileSystemService
/// </summary>
public partial class FileSystemService
{
    /// <summary>
    /// Handles FileCdnLinkReported events by recording CDN metadata for future use.
    /// This allows the system to reuse Discord CDN URLs instead of re-uploading files.
    /// </summary>
    public static async Task HandleFileCdnLinkReportedAsync(FileCdnLinkReported evt)
    {
        try
        {
            // Get the FileSystemService instance
            var fileSystemService = CoreService.FileSystem;

            // Record the CDN metadata
            fileSystemService.RecordCdnMetadata(evt.CanonicalFileName, evt.CdnUrl, evt.SourceMessageId, evt.ChannelId);

            // No need to await anything - this is a fire-and-forget operation
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            // Log error but don't throw - CDN caching is non-critical
            await CoreService.ErrorHandler.CaptureAsync(
                ex,
                $"Failed to record CDN metadata for {evt.CanonicalFileName}",
                nameof(HandleFileCdnLinkReportedAsync)
            );
        }
    }
}
