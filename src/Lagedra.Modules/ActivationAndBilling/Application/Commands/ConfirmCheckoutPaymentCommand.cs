using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ConfirmCheckoutPaymentCommand(
    Guid DealId,
    Guid TenantUserId) : IRequest<Result<CheckoutDto>>;

public sealed partial class ConfirmCheckoutPaymentCommandHandler(
    BillingDbContext dbContext,
    IStripeService stripeService,
    IMediator mediator,
    IClock clock,
    ILogger<ConfirmCheckoutPaymentCommandHandler> logger)
    : IRequestHandler<ConfirmCheckoutPaymentCommand, Result<CheckoutDto>>
{
    public async Task<Result<CheckoutDto>> Handle(
        ConfirmCheckoutPaymentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result<CheckoutDto>.Failure(
                new Error("Checkout.NoPaymentConfirmation",
                    "No payment confirmation record exists for this deal."));
        }

        if (confirmation.Status == PaymentConfirmationStatus.Confirmed)
        {
            return Result<CheckoutDto>.Success(BuildDto(confirmation));
        }

        if (string.IsNullOrEmpty(confirmation.StripePaymentIntentId))
        {
            return Result<CheckoutDto>.Failure(
                new Error("Checkout.NoPaymentIntent",
                    "No payment has been initiated yet."));
        }

        var pi = await stripeService
            .GetPaymentIntentAsync(confirmation.StripePaymentIntentId, cancellationToken)
            .ConfigureAwait(false);

        confirmation.SetStripePaymentIntent(pi.PaymentIntentId, pi.Status, clock);

        if (pi.Status == "succeeded")
        {
            confirmation.ConfirmByStripe(clock);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            LogPaymentConfirmed(logger, request.DealId, pi.PaymentIntentId);

            await mediator.Send(new ActivateDealCommand(request.DealId), cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<CheckoutDto>.Success(BuildDto(confirmation, pi));
    }

    private static CheckoutDto BuildDto(
        Domain.Aggregates.DealPaymentConfirmation c,
        StripePaymentIntentResult? pi = null) =>
        new(pi?.ClientSecret ?? string.Empty,
            pi?.PaymentIntentId ?? c.StripePaymentIntentId ?? string.Empty,
            pi?.Status ?? c.StripePaymentStatus ?? "unknown",
            c.TotalTenantPaymentCents,
            c.FirstMonthRentCents,
            c.DepositAmountCents,
            c.InsuranceFeeCents,
            c.InsuranceFeeCents + c.MonthlyProtocolFeeCents,
            "usd");

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Payment confirmed for deal {DealId}, PI {PaymentIntentId} — activating deal")]
    private static partial void LogPaymentConfirmed(ILogger logger, Guid dealId, string paymentIntentId);
}
