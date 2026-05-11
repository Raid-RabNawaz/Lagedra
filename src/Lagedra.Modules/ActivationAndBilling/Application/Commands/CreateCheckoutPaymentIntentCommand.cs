using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record CreateCheckoutPaymentIntentCommand(
    Guid DealId,
    Guid TenantUserId) : IRequest<Result<CheckoutDto>>;

public sealed class CreateCheckoutPaymentIntentCommandHandler(
    BillingDbContext dbContext,
    IStripeService stripeService,
    IHostStripeAccountProvider hostStripeProvider,
    IHostPaymentDetailsProvider hostPaymentDetailsProvider,
    IClock clock)
    : IRequestHandler<CreateCheckoutPaymentIntentCommand, Result<CheckoutDto>>
{
    public async Task<Result<CheckoutDto>> Handle(
        CreateCheckoutPaymentIntentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<CheckoutDto>.Failure(
                new Error("Checkout.NoApplication", "No approved application found for this deal."));
        }

        if (application.TenantUserId != request.TenantUserId)
        {
            return Result<CheckoutDto>.Failure(
                new Error("Checkout.Forbidden",
                    "Only the deal's tenant can initiate checkout."));
        }

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result<CheckoutDto>.Failure(
                new Error("Checkout.NoPaymentConfirmation",
                    "No payment confirmation record exists for this deal. Both parties must sign the Truth Surface first."));
        }

        if (confirmation.Status == PaymentConfirmationStatus.Confirmed)
        {
            return Result<CheckoutDto>.Failure(
                new Error("Checkout.AlreadyPaid", "This deal has already been paid for."));
        }

        if (!string.IsNullOrEmpty(confirmation.StripePaymentIntentId))
        {
            var existing = await stripeService
                .GetPaymentIntentAsync(confirmation.StripePaymentIntentId, cancellationToken)
                .ConfigureAwait(false);

            if (existing.Status is "succeeded")
            {
                return Result<CheckoutDto>.Failure(
                    new Error("Checkout.AlreadyPaid", "This deal has already been paid for."));
            }

            if (existing.Status is "requires_payment_method" or "requires_confirmation" or "requires_action")
            {
                return Result<CheckoutDto>.Success(BuildDto(existing, confirmation));
            }
        }

        var totalAmountCents = confirmation.TotalTenantPaymentCents;
        var applicationFeeCents = confirmation.InsuranceFeeCents + confirmation.MonthlyProtocolFeeCents;

        var idempotencyKey = $"pi-deal-{request.DealId}";
        var metadata = new Dictionary<string, string>
        {
            ["dealId"] = request.DealId.ToString(),
            ["tenantUserId"] = request.TenantUserId.ToString(),
            ["landlordUserId"] = application.LandlordUserId.ToString()
        };

        // Use a dedicated timeout instead of the HTTP request's cancellation token.
        // The client may disconnect (Axios timeout) before Stripe responds; we still
        // want the PaymentIntent created and persisted so a page refresh picks it up.
        using var stripeCts = new CancellationTokenSource(TimeSpan.FromSeconds(55));

        // Prefer the Airbnb-style direct-payout flow: if the host has saved payout
        // instructions, charge fully to the platform Stripe account. The host is paid
        // out-of-band based on those instructions.
        var directPayoutDetails = await hostPaymentDetailsProvider
            .GetDecryptedPaymentDetailsAsync(application.LandlordUserId, stripeCts.Token)
            .ConfigureAwait(false);

        StripePaymentIntentResult pi;
        if (directPayoutDetails is not null)
        {
            metadata["payoutModel"] = "direct";
            pi = await stripeService.CreatePlatformPaymentIntentAsync(
                totalAmountCents,
                "usd",
                metadata,
                idempotencyKey,
                stripeCts.Token).ConfigureAwait(false);
        }
        else
        {
            // Backward-compatible fallback: hosts who previously onboarded via Stripe Connect.
            var hostStripe = await hostStripeProvider
                .GetByHostUserIdAsync(application.LandlordUserId, cancellationToken)
                .ConfigureAwait(false);

            if (hostStripe is null || !hostStripe.ChargesEnabled)
            {
                return Result<CheckoutDto>.Failure(
                    new Error("Checkout.HostNotOnboarded",
                        "The host has not added payout details yet. Please wait for the host to complete payout setup."));
            }

            metadata["payoutModel"] = "stripe-connect";
            pi = await stripeService.CreateDestinationPaymentIntentAsync(
                totalAmountCents,
                "usd",
                hostStripe.StripeAccountId,
                applicationFeeCents,
                metadata,
                idempotencyKey,
                stripeCts.Token).ConfigureAwait(false);
        }

        confirmation.SetStripePaymentIntent(pi.PaymentIntentId, pi.Status, clock);
        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        return Result<CheckoutDto>.Success(BuildDto(pi, confirmation));
    }

    private static CheckoutDto BuildDto(StripePaymentIntentResult pi, Domain.Aggregates.DealPaymentConfirmation c) =>
        new(pi.ClientSecret,
            pi.PaymentIntentId,
            pi.Status,
            c.TotalTenantPaymentCents,
            c.FirstMonthRentCents,
            c.DepositAmountCents,
            c.InsuranceFeeCents,
            c.InsuranceFeeCents + c.MonthlyProtocolFeeCents,
            "usd");
}
