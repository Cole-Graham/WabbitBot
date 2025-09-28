
using System.Threading.Tasks;

namespace WabbitBot.Common.ErrorService;

public partial class ErrorService
{
    // In a real implementation, this would use a logging library like Serilog or NLog.
    private async partial Task LogAsync(ErrorContext context)
    {
        Console.WriteLine($"[LOG] [{context.Severity}] Operation '{context.OperationName}' failed: {context.Message}");
        if (context.Exception != null)
        {
            Console.WriteLine(context.Exception);
        }
        await Task.CompletedTask;
    }
}
