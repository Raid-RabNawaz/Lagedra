using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record GetPaymentConfirmationStatusQuery(
    Guid DealId) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class GetPaymentConfirmationStatusQueryHandler(
    BillingDbContext dbContext)
    : IRequestHandler<GetPaymentConfirmationStatusQuery, Result<PaymentConfirmationDto>>
{
    public async Task<Result<PaymentConfirmationDto>> Handle(
        GetPaymentConfirmationStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var confirmation = await dbContext.DealPaymentConfirmations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("PaymentConfirmation.NotFound",
                    "No payment confirmation record found for this deal."));
        }

        return Result<PaymentConfirmationDto>.Success(
            new PaymentConfirmationDto(
                confirmation.Id,
                confirmation.DealId,
                confirmation.Status,
                confirmation.HostConfirmed,
                confirmation.HostConfirmedAt,
                confirmation.TenantDisputed,
                confirmation.TenantDisputedAt,
                confirmation.DisputeReason,
                confirmation.GracePeriodExpiresAt,
                confirmation.TotalTenantPaymentCents,
                confirmation.TotalHostPlatformPaymentCents,
                confirmation.FirstMonthRentCents,
                confirmation.DepositAmountCents,
                confirmation.InsuranceFeeCents,
                confirmation.MonthlyProtocolFeeCents,
                confirmation.HostPaidPlatform,
                confirmation.HostPaidPlatformAt));
    }
}
