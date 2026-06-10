using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Lagedra.Modules.ActivationAndBilling.Domain.ValueObjects;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Services;

/// <summary>
/// Phase 16.9 — encapsulates the off-session charge that runs the
/// instant a host approves an application that was submitted with a
/// saved Stripe payment-method (apply-dialog SetupIntent flow). Used by
/// both <c>ApproveDealApplicationCommand</c> and
/// <c>SubmitApplicationCommand</c> (instant-book branch) so the actual
/// Stripe call + DealPaymentConfirmation lifecycle stays in one place.
/// </summary>
public interface ICardOnFileChargeService
{
    Task<CardOnFileChargeResult> TryChargeAsync(
        DealApplication application,
        Guid dealId,
        long firstMonthRentCents,
        long depositAmountCents,
        long insuranceFeeCents,
        long monthlyProtocolFeeCents,
        long serviceFeeCents,
        CancellationToken cancellationToken);
}

public sealed record CardOnFileChargeResult(
    bool Charged,
    string? PaymentIntentId,
    string? FailureReason);

public sealed partial class CardOnFileChargeService(
    BillingDbContext dbContext,
    IStripeService stripeService,
    IUserStripeProfileService userStripeProfile,
    IHostStripeAccountProvider hostStripeProvider,
    IHostPaymentDetailsProvider hostPaymentDetailsProvider,
    IClock clock,
    ILogger<CardOnFileChargeService> logger)
    : ICardOnFileChargeService
{
    public async Task<CardOnFileChargeResult> TryChargeAsync(
        DealApplication application,
        Guid dealId,
        long firstMonthRentCents,
        long depositAmountCents,
        long insuranceFeeCents,
        long monthlyProtocolFeeCents,
        long serviceFeeCents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(application);

        if (string.IsNullOrEmpty(application.StripePaymentMethodId))
        {
            return new CardOnFileChargeResult(false, null, "no-payment-method");
        }

        var profile = await userStripeProfile
            .GetAsync(application.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null || string.IsNullOrEmpty(profile.StripeCustomerId))
        {
            // The PM was attached to *some* customer when the SetupIntent
            // ran; if the cached id is gone (e.g. rare DB rollback) skip
            // off-session and let the standard checkout flow handle it.
            LogMissingCustomer(logger, application.Id, application.TenantUserId);
            return new CardOnFileChargeResult(false, null, "missing-customer");
        }

        var totalAmountCents =
            firstMonthRentCents + depositAmountCents + insuranceFeeCents + serviceFeeCents;
        var applicationFeeCents = insuranceFeeCents + monthlyProtocolFeeCents + serviceFeeCents;
        var idempotencyKey = $"oss-deal-{dealId:N}";
        var metadata = new Dictionary<string, string>
        {
            ["dealId"] = dealId.ToString(),
            ["tenantUserId"] = application.TenantUserId.ToString(),
            ["landlordUserId"] = application.LandlordUserId.ToString(),
            ["payoutModel"] = "card-on-file",
            ["bookingFlow"] = "v2",
        };

        StripePaymentIntentResult charge;
        try
        {
            var directPayout = await hostPaymentDetailsProvider
                .GetDecryptedPaymentDetailsAsync(application.LandlordUserId, cancellationToken)
                .ConfigureAwait(false);

            if (directPayout is not null)
            {
                charge = await stripeService.ChargeOffSessionPlatformAsync(
                    profile.StripeCustomerId,
                    application.StripePaymentMethodId,
                    totalAmountCents,
                    "usd",
                    metadata,
                    idempotencyKey,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var hostStripe = await hostStripeProvider
                    .GetByHostUserIdAsync(application.LandlordUserId, cancellationToken)
                    .ConfigureAwait(false);

                if (hostStripe is null || !hostStripe.ChargesEnabled)
                {
                    return new CardOnFileChargeResult(false, null, "host-not-onboarded");
                }

                charge = await stripeService.ChargeOffSessionDestinationAsync(
                    profile.StripeCustomerId,
                    application.StripePaymentMethodId,
                    totalAmountCents,
                    "usd",
                    hostStripe.StripeAccountId,
                    applicationFeeCents,
                    metadata,
                    idempotencyKey,
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Stripe.StripeException ex)
        {
            // Don't bubble — the host's approve action should still
            // succeed. The tenant will see the standard checkout panel
            // on /checkout when the off-session attempt fails (e.g.
            // requires_action / authentication required, declined, etc.).
            LogChargeException(logger, application.Id, ex.Message);
            return new CardOnFileChargeResult(false, null, "stripe-exception");
        }

        if (charge.Status is not ("succeeded" or "processing" or "requires_capture"))
        {
            LogChargeUnsuccessful(logger, application.Id, charge.PaymentIntentId, charge.Status);
            return new CardOnFileChargeResult(false, charge.PaymentIntentId, charge.Status);
        }

        await PersistConfirmationAsync(
            dealId,
            firstMonthRentCents,
            depositAmountCents,
            insuranceFeeCents,
            monthlyProtocolFeeCents,
            serviceFeeCents,
            totalAmountCents,
            charge.PaymentIntentId,
            cancellationToken).ConfigureAwait(false);

        return new CardOnFileChargeResult(true, charge.PaymentIntentId, null);
    }

    private async Task PersistConfirmationAsync(
        Guid dealId,
        long firstMonthRentCents,
        long depositAmountCents,
        long insuranceFeeCents,
        long monthlyProtocolFeeCents,
        long serviceFeeCents,
        long _totalAmountCents,
        string paymentIntentId,
        CancellationToken cancellationToken)
    {
        // Idempotent: a downstream TruthSurfaceConfirmedEvent handler
        // creates a confirmation row when the snapshot seals. The
        // card-on-file flow always settles *before* the snapshot seals,
        // so the row is virtually always missing here — but we still
        // do the FirstOrDefault so a manual re-run / retry stays safe.
        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == dealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            var financials = DealFinancials.Create(
                firstMonthRentCents,
                depositAmountCents,
                insuranceFeeCents,
                monthlyProtocolFeeCents,
                serviceFeeCents);
            confirmation = DealPaymentConfirmation.Create(dealId, financials, clock);
            dbContext.DealPaymentConfirmations.Add(confirmation);
        }

        confirmation.SetStripePaymentIntent(paymentIntentId, "succeeded", clock);
        // ConfirmByStripe flips Status → Confirmed, marks host-confirmed
        // and host-paid-platform, and raises PaymentConfirmedEvent
        // which triggers the downstream deal activation pipeline.
        confirmation.ConfirmByStripe(clock);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Card-on-file: tenant {TenantUserId} on application {ApplicationId} has no cached Stripe customer; falling back to standard checkout")]
    private static partial void LogMissingCustomer(
        ILogger logger, Guid applicationId, Guid tenantUserId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Card-on-file: Stripe charge threw for application {ApplicationId}: {Message}")]
    private static partial void LogChargeException(
        ILogger logger, Guid applicationId, string message);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Card-on-file: PI {PaymentIntentId} for application {ApplicationId} returned non-success status {Status}")]
    private static partial void LogChargeUnsuccessful(
        ILogger logger, Guid applicationId, string? paymentIntentId, string status);
}
