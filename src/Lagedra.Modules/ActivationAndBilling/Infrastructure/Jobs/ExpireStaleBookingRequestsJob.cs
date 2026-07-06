using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;

/// <summary>
/// Expires reservation requests the host never acted on. A request that's been
/// <see cref="DealApplicationStatus.Pending"/> longer than the configured window
/// (default 72h, matching the one-tap approval token TTL) is moved to
/// <see cref="DealApplicationStatus.Expired"/>. Each expiry raises
/// ApplicationExpiredEvent via the outbox so the tenant is notified and freed to
/// re-request or look elsewhere.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class ExpireStaleBookingRequestsJob(
    BillingDbContext dbContext,
    IClock clock,
    IPlatformSettingsService settings,
    ILogger<ExpireStaleBookingRequestsJob> logger) : IJob
{
    private const int DefaultExpiryHours = 72;

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;

        var expiryHours = await settings
            .GetLongAsync(PlatformSettingKeys.BookingRequestExpiryHours, DefaultExpiryHours, ct)
            .ConfigureAwait(false);
        if (expiryHours <= 0)
        {
            expiryHours = DefaultExpiryHours;
        }

        var cutoff = clock.UtcNow.AddHours(-expiryHours);

        var stale = await dbContext.DealApplications
            .Where(a => a.Status == DealApplicationStatus.Pending && a.SubmittedAt < cutoff)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (stale.Count == 0)
        {
            return;
        }

        foreach (var application in stale)
        {
            application.MarkExpired();
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        LogExpired(logger, stale.Count, expiryHours);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Expired {Count} booking request(s) pending longer than {Hours}h")]
    private static partial void LogExpired(ILogger logger, int count, long hours);
}
