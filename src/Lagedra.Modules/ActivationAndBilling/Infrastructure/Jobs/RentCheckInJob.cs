using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;

/// <summary>
/// Nightly sweep that opens a rent check-in for every active deal whose
/// monthly rent anniversary has arrived (months 2+ are paid to the host
/// directly, so this attestation is the platform's only visibility into
/// whether rent flows). Idempotent via the unique (DealId, PeriodStart)
/// index — a period is only ever asked about once.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class RentCheckInJob(
    BillingDbContext dbContext,
    IMediator mediator,
    IClock clock,
    ILogger<RentCheckInJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;

        var today = DateOnly.FromDateTime(clock.UtcNow.Date);

        var activeDeals = await (
                from account in dbContext.BillingAccounts.AsNoTracking()
                join application in dbContext.DealApplications.AsNoTracking()
                    on account.DealId equals application.DealId
                where account.Status == BillingAccountStatus.Active
                select new
                {
                    account.DealId,
                    application.LandlordUserId,
                    application.RequestedCheckIn,
                    application.RequestedCheckOut,
                })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var opened = new List<RentCheckIn>();

        foreach (var deal in activeDeals)
        {
            var duePeriods = RentPeriodCalculator.DuePeriods(
                deal.RequestedCheckIn, deal.RequestedCheckOut, today);
            if (duePeriods.Count == 0)
            {
                continue;
            }

            var existingStarts = await dbContext.RentCheckIns
                .AsNoTracking()
                .Where(r => r.DealId == deal.DealId)
                .Select(r => r.PeriodStart)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var period in duePeriods)
            {
                if (existingStarts.Contains(period.Start))
                {
                    continue;
                }

                var checkIn = RentCheckIn.Create(
                    deal.DealId, deal.LandlordUserId, period.Start, period.End, clock);
                dbContext.RentCheckIns.Add(checkIn);
                opened.Add(checkIn);
            }
        }

        if (opened.Count == 0)
        {
            return;
        }

        // Persist first: the unique index guarantees a period is never asked
        // about twice, so a notification failure below can't cause re-asks.
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        LogCheckInsOpened(logger, opened.Count);

        foreach (var checkIn in opened)
        {
            await NotifyHostAsync(checkIn, ct).ConfigureAwait(false);
        }
    }

    private async Task NotifyHostAsync(RentCheckIn checkIn, CancellationToken ct)
    {
        var periodLabel =
            $"{checkIn.PeriodStart:MMM d} – {checkIn.PeriodEnd:MMM d, yyyy}";

        try
        {
            await mediator.Send(new NotifyUserCommand(
                checkIn.LandlordUserId,
                "rent_checkin_due",
                "Rent check-in",
                $"Did you receive the rent for {periodLabel}? Confirm it on your deal's billing page.",
                new()
                {
                    ["dealId"] = checkIn.DealId.ToString(),
                    ["periodLabel"] = periodLabel,
                },
                [NotificationChannel.Email, NotificationChannel.InApp],
                checkIn.DealId,
                "Deal"), ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // best-effort: the check-in row exists; the UI still surfaces it
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogNotifyFailed(logger, checkIn.DealId, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Opened {Count} rent check-in(s) for active deals")]
    private static partial void LogCheckInsOpened(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to notify host about a rent check-in for deal {DealId}")]
    private static partial void LogNotifyFailed(ILogger logger, Guid dealId, Exception ex);
}
