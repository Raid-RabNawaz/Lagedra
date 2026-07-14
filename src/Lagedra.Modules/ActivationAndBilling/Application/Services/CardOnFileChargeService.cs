using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
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
    IPartnerOrganizationBillingProfile partnerOrgBilling,
    IHostStripeAccountProvider hostStripeProvider,
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

        var customerId = await ResolvePayerCustomerIdAsync(application, cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(customerId))
        {
            LogMissingCustomer(logger, application.Id, application.TenantUserId);
            return new CardOnFileChargeResult(false, null, "missing-customer");
        }

        var totalAmountCents =
            firstMonthRentCents + depositAmountCents + insuranceFeeCents + serviceFeeCents;
        // Non-custodial (Option A): platform fee = tenant service fee + insurance
        // premium only. The monthly protocol fee is billed to the host via
        // subscription, never taken at checkout. Rent + deposit transfer to the host.
        var applicationFeeCents = insuranceFeeCents + serviceFeeCents;
        var idempotencyKey = $"oss-deal-{dealId:N}";
        var metadata = new Dictionary<string, string>
        {
            ["dealId"] = dealId.ToString(),
            ["tenantUserId"] = application.TenantUserId.ToString(),
            ["landlordUserId"] = application.LandlordUserId.ToString(),
            ["payerType"] = application.PayerType.ToString(),
            ["payoutModel"] = "stripe-connect",
            ["bookingFlow"] = "v2",
        };

        if (application.PartnerOrganizationId is { } partnerOrgId)
        {
            metadata["partnerOrganizationId"] = partnerOrgId.ToString();
        }

        if (application.PayerUserId is { } payerUserId)
        {
            metadata["payerUserId"] = payerUserId.ToString();
        }

        var hostStripe = await hostStripeProvider
            .GetByHostUserIdAsync(application.LandlordUserId, cancellationToken)
            .ConfigureAwait(false);

        if (hostStripe is null || !hostStripe.ChargesEnabled)
        {
            return new CardOnFileChargeResult(false, null, "host-not-onboarded");
        }

        StripePaymentIntentResult charge;
        try
        {
            charge = await stripeService.ChargeOffSessionDestinationAsync(
                customerId,
                application.StripePaymentMethodId,
                totalAmountCents,
                "usd",
                hostStripe.StripeAccountId,
                hostStripe.StripeAccountId,
                applicationFeeCents,
                metadata,
                statementDescriptorSuffix: null,
                idempotencyKey,
                cancellationToken).ConfigureAwait(false);
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

    private async Task<string?> ResolvePayerCustomerIdAsync(
        DealApplication application,
        CancellationToken cancellationToken)
    {
        if (application.PayerType == ApplicationPayerType.PartnerOrganization
            && application.PartnerOrganizationId is { } orgId)
        {
            return await partnerOrgBilling
                .GetStripeCustomerIdAsync(orgId, cancellationToken)
                .ConfigureAwait(false);
        }

        var profile = await userStripeProfile
            .GetAsync(application.TenantUserId, cancellationToken)
            .ConfigureAwait(false);

        return profile?.StripeCustomerId;
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
        Message = "Card-on-file: payer for application {ApplicationId} (tenant {TenantUserId}) has no cached Stripe customer; falling back to standard checkout")]
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
