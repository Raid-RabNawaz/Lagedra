using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record GetCheckoutStatusQuery(Guid DealId, Guid RequestingUserId) : IRequest<Result<CheckoutDto>>;

public sealed class GetCheckoutStatusQueryHandler(
    BillingDbContext dbContext,
    IStripeService stripeService)
    : IRequestHandler<GetCheckoutStatusQuery, Result<CheckoutDto>>
{
    public async Task<Result<CheckoutDto>> Handle(
        GetCheckoutStatusQuery request,
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
                new Error("Checkout.NotFound", "No application found for this deal."));
        }

        if (application.TenantUserId != request.RequestingUserId &&
            application.LandlordUserId != request.RequestingUserId)
        {
            return Result<CheckoutDto>.Failure(
                new Error("Checkout.Forbidden", "You do not have access to this deal's checkout status."));
        }

        var confirmation = await dbContext.DealPaymentConfirmations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result<CheckoutDto>.Failure(
                new Error("Checkout.NotFound", "No payment confirmation found for this deal."));
        }

        if (string.IsNullOrEmpty(confirmation.StripePaymentIntentId))
        {
            return Result<CheckoutDto>.Success(
                new CheckoutDto(
                    string.Empty,
                    string.Empty,
                    "not_started",
                    confirmation.TotalTenantPaymentCents,
                    confirmation.FirstMonthRentCents,
                    confirmation.DepositAmountCents,
                    confirmation.InsuranceFeeCents,
                    confirmation.InsuranceFeeCents + confirmation.MonthlyProtocolFeeCents + confirmation.ServiceFeeCents,
                    confirmation.ServiceFeeCents,
                    "usd"));
        }

        var pi = await stripeService
            .GetPaymentIntentAsync(confirmation.StripePaymentIntentId, cancellationToken)
            .ConfigureAwait(false);

        return Result<CheckoutDto>.Success(
            new CheckoutDto(
                pi.ClientSecret,
                pi.PaymentIntentId,
                pi.Status,
                confirmation.TotalTenantPaymentCents,
                confirmation.FirstMonthRentCents,
                confirmation.DepositAmountCents,
                confirmation.InsuranceFeeCents,
                confirmation.InsuranceFeeCents + confirmation.MonthlyProtocolFeeCents + confirmation.ServiceFeeCents,
                confirmation.ServiceFeeCents,
                "usd"));
    }
}
