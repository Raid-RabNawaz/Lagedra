using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class RefundIncompleteScreeningOrdersJob(
    IMediator mediator,
    ILogger<RefundIncompleteScreeningOrdersJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result = await mediator
            .Send(new RefundIncompleteScreeningOrdersCommand(), context.CancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            LogRefunded(logger, result.Value);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Refunded {Count} incomplete screening orders")]
    private static partial void LogRefunded(ILogger logger, int count);
}
