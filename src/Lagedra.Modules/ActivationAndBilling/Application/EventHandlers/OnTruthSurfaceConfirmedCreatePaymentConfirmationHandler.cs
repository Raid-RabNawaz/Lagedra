using Lagedra.Modules.ActivationAndBilling.Application.Services;
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
    ICardOnFileChargeService cardOnFileChargeService,
    IFeatureFlags featureFlags,
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

        var protocolFee = await ResolveMonthlyProtocolFeeAsync(ct).ConfigureAwait(false);

        var rentBaseCents = application?.FirstMonthRentCents ?? 1;
        var serviceFee = await ResolveServiceFeeAsync(rentBaseCents, ct).ConfigureAwait(false);

        if (!existing)
        {
            var financials = DealFinancials.Create(
                rentBaseCents,
                application?.DepositAmountCents ?? 0,
                application?.InsuranceFeeCents ?? 0,
                protocolFee,
                serviceFee);

            var graceDays = (int)await settings
                .GetLongAsync(PlatformSettingKeys.PaymentGracePeriodDays, 3, ct).ConfigureAwait(false);

            var confirmation = DealPaymentConfirmation.Create(
                domainEvent.DealId, financials, clock, graceDays, domainEvent.SnapshotId);
            dbContext.DealPaymentConfirmations.Add(confirmation);

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

            LogCreated(logger, domainEvent.DealId, confirmation.GracePeriodExpiresAt);
        }
        else
        {
            LogAlreadyExists(logger, domainEvent.DealId);
            if (application is not null)
            {
                await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }

        // Phase 16.9 — now that the snapshot is sealed (both parties
        // confirmed), settle the booking off-session if the tenant
        // captured a card during apply. This is the *only* path where
        // a Confirmed DealPaymentConfirmation can be produced under V2:
        // we will never charge before the Truth Surface is the binding
        // record of the deal. CardOnFileChargeService is idempotent —
        // it picks up the row we just created (or the pre-existing one).
        if (featureFlags.BookingFlowV2Enabled
            && application is { DealId: { } chargeDealId }
            && !string.IsNullOrEmpty(application.StripePaymentMethodId))
        {
            var chargeResult = await cardOnFileChargeService.TryChargeAsync(
                application,
                chargeDealId,
                application.FirstMonthRentCents ?? 0,
                application.DepositAmountCents ?? 0,
                application.InsuranceFeeCents ?? 0,
                protocolFee,
                serviceFee,
                ct).ConfigureAwait(false);

            if (!chargeResult.Charged)
            {
                LogCardOnFileFallback(
                    logger, application.Id, chargeResult.FailureReason ?? "unknown");
            }
        }
    }

    private async Task<long> ResolveMonthlyProtocolFeeAsync(CancellationToken ct)
    {
        var monthly = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeeMonthly, 7900, ct)
            .ConfigureAwait(false);
        var pilotDiscount = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeePilotDiscount, 3900, ct)
            .ConfigureAwait(false);
        var isPilot = await settings
            .GetBoolAsync(PlatformSettingKeys.ProtocolFeePilotActive, false, ct)
            .ConfigureAwait(false);
        return isPilot ? monthly - pilotDiscount : monthly;
    }

    private async Task<long> ResolveServiceFeeAsync(long rentBaseCents, CancellationToken ct)
    {
        var useFlat = await settings
            .GetBoolAsync(PlatformSettingKeys.TenantServiceFeeUseFlat, false, ct).ConfigureAwait(false);
        var flatCents = await settings
            .GetLongAsync(PlatformSettingKeys.TenantServiceFeeFlatCents, 0, ct).ConfigureAwait(false);
        var bps = await settings
            .GetLongAsync(PlatformSettingKeys.TenantServiceFeeBps, 0, ct).ConfigureAwait(false);

        return TenantServiceFee.Compute(rentBaseCents, useFlat, flatCents, bps);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Created payment confirmation for deal {DealId}, grace period expires at {ExpiresAt}")]
    private static partial void LogCreated(ILogger logger, Guid dealId, DateTime expiresAt);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Payment confirmation already exists for deal {DealId}, skipping creation")]
    private static partial void LogAlreadyExists(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Card-on-file charge skipped for application {ApplicationId}: {Reason}; tenant will use standard checkout")]
    private static partial void LogCardOnFileFallback(
        ILogger logger, Guid applicationId, string reason);
}
