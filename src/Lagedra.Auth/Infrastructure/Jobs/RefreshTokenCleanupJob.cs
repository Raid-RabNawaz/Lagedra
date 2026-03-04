using Lagedra.Auth.Infrastructure.Repositories;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Auth.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class RefreshTokenCleanupJob(
    IRefreshTokenRepository repository,
    IClock clock,
    ILogger<RefreshTokenCleanupJob> logger)
    : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var olderThan = clock.UtcNow.AddDays(-30);
        var deleted = await repository.DeleteExpiredAsync(olderThan, context.CancellationToken).ConfigureAwait(false);

        LogCleanupCompleted(logger, deleted, olderThan);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "RefreshTokenCleanup: deleted {Count} expired/revoked tokens older than {OlderThan:O}")]
    private static partial void LogCleanupCompleted(ILogger logger, int count, DateTimeOffset olderThan);
}
