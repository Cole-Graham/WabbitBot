using System.Threading.Tasks;

namespace WabbitBot.Common.ErrorService;

public partial class ErrorService
{
    // In a real implementation, this could send an email, a Discord message, or a Slack notification.
    private async partial Task NotifyAsync(ErrorContext context)
    {
        if (context.Severity >= ErrorSeverity.Error)
        {
            Console.WriteLine(
                $"[NOTIFICATION] Notifying admin of {context.Severity} error in {context.OperationName}."
            );
        }
        await Task.CompletedTask;
    }
}
