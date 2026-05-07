using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

public sealed partial class OnTruthSurfaceConfirmedCreatePaymentConfirmationHandler(
    BillingDbContext dbContext,
    IClock clock,
    IPlatformSettingsService settings,
    ILogger<OnTruthSurfaceConfirmedCreatePaymentConfirmationHandler> logger)
    : IDomainEventHandler<TruthSurfaceConfirmedEvent>
{
    public async Task Handle(TruthSurfaceConfirmedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        // Always link the application to the snapshot so arbitration has a
        // direct FK from the booking to its sealed Truth Surface.
        var application = await dbContext.DealApplications
            .FirstOrDefaultAsync(a => a.DealId == domainEvent.DealId, ct)
            .ConfigureAwait(false);

        if (application is not null && application.TruthSurfaceSnapshotId != domainEvent.SnapshotId)
        {
            application.LinkTruthSurface(domainEvent.SnapshotId);
        }

        var existing = await dbContext.DealPaymentConfirmations
            .AsNoTracking()
            .AnyAsync(c => c.DealId == domainEvent.DealId, ct)
            .ConfigureAwait(false);

        if (existing)
        {
            LogAlreadyExists(logger, domainEvent.DealId);
            // Still need to flush the application FK update if any.
            if (application is not null)
            {
                await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            return;
        }

        var monthlyFee = await settings.GetLongAsync(PlatformSettingKeys.ProtocolFeeMonthly, 7900, ct).ConfigureAwait(false);
        var pilotDiscount = await settings.GetLongAsync(PlatformSettingKeys.ProtocolFeePilotDiscount, 3900, ct).ConfigureAwait(false);
        var isPilot = await settings.GetBoolAsync(PlatformSettingKeys.ProtocolFeePilotActive, false, ct).ConfigureAwait(false);
        var protocolFee = isPilot ? monthlyFee - pilotDiscount : monthlyFee;

        var financials = DealFinancials.Create(
            application?.FirstMonthRentCents ?? 1,
            application?.DepositAmountCents ?? 0,
            application?.InsuranceFeeCents ?? 0,
            protocolFee);

        var graceDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.PaymentGracePeriodDays, 3, ct).ConfigureAwait(false);

        var confirmation = DealPaymentConfirmation.Create(
            domainEvent.DealId, financials, clock, graceDays, domainEvent.SnapshotId);
        dbContext.DealPaymentConfirmations.Add(confirmation);

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        LogCreated(logger, domainEvent.DealId, confirmation.GracePeriodExpiresAt);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Created payment confirmation for deal {DealId}, grace period expires at {ExpiresAt}")]
    private static partial void LogCreated(ILogger logger, Guid dealId, DateTime expiresAt);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Payment confirmation already exists for deal {DealId}, skipping creation")]
    private static partial void LogAlreadyExists(ILogger logger, Guid dealId);
}
