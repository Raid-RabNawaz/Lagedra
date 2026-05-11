using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class DepositReturnJob(
    BillingDbContext dbContext,
    IMediator mediator,
    IClock clock,
    IPlatformSettingsService settings,
    ILogger<DepositReturnJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;
        var claimDeadlineDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.DamageClaimFilingDeadlineDays, 14, ct)
            .ConfigureAwait(false);

        var cutoff = clock.UtcNow.AddDays(-claimDeadlineDays);

        var closedAccounts = await dbContext.BillingAccounts
            .Where(b => b.Status == BillingAccountStatus.Closed
                && b.EndDate != null
                && b.EndDate.Value <= cutoff)
            .Select(b => b.DealId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (closedAccounts.Count == 0)
        {
            return;
        }

        var dealsWithConfirmedPayment = await dbContext.DealPaymentConfirmations
            .Where(c => closedAccounts.Contains(c.DealId)
                && c.Status == PaymentConfirmationStatus.Confirmed
                && c.StripePaymentStatus == "succeeded"
                && c.DepositAmountCents > 0)
            .Select(c => c.DealId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var dealsWithOpenClaims = await dbContext.DamageClaims
            .Where(c => dealsWithConfirmedPayment.Contains(c.DealId)
                && c.Status != DamageClaimStatus.Rejected
                && c.Status != DamageClaimStatus.Settled)
            .Select(c => c.DealId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var eligibleDeals = dealsWithConfirmedPayment
            .Except(dealsWithOpenClaims)
            .ToList();

        LogEligibleDeals(logger, eligibleDeals.Count);

        foreach (var dealId in eligibleDeals)
        {
            var result = await mediator
                .Send(new ReturnDepositCommand(dealId), ct)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                LogDepositReturnSkipped(logger, dealId, result.Error.Description);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "DepositReturnJob: {Count} deals eligible for deposit return")]
    private static partial void LogEligibleDeals(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "DepositReturnJob: Skipped deposit return for deal {DealId}: {Reason}")]
    private static partial void LogDepositReturnSkipped(ILogger logger, Guid dealId, string reason);
}
