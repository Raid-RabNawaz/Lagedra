using Lagedra.Infrastructure.Eventing;
using Lagedra.Infrastructure.Persistence;
using Quartz;

namespace Lagedra.Worker.Orchestration;

[DisallowConcurrentExecution]
internal sealed partial class OutboxDispatchOrchestrator(
    IServiceProvider serviceProvider,
    ILogger<OutboxDispatchOrchestrator> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;
        await using var scope = serviceProvider.CreateAsyncScope();

        var contexts = scope.ServiceProvider.GetServices<IOutboxContext>();
        var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

        var count = 0;
        foreach (var outboxContext in contexts)
        {
            await processor.ProcessAsync(outboxContext, ct).ConfigureAwait(false);
            count++;
        }

        LogOutboxDispatchCompleted(logger, count);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox dispatch completed across {ContextCount} module contexts")]
    private static partial void LogOutboxDispatchCompleted(ILogger logger, int contextCount);
}
