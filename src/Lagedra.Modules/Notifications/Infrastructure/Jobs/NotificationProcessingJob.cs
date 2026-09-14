using Lagedra.Modules.Notifications.Infrastructure.Dispatch;
using Quartz;

namespace Lagedra.Modules.Notifications.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed class NotificationProcessingJob(
    NotificationDispatchService dispatcher) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return dispatcher.ProcessBatchAsync(context.CancellationToken);
    }
}
