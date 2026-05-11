using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record GetPaymentConfirmationStatusQuery(
    Guid DealId,
    Guid CallerUserId,
    bool IsAdmin = false) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class GetPaymentConfirmationStatusQueryHandler(
    BillingDbContext dbContext)
    : IRequestHandler<GetPaymentConfirmationStatusQuery, Result<PaymentConfirmationDto>>
{
    public async Task<Result<PaymentConfirmationDto>> Handle(
        GetPaymentConfirmationStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsAdmin)
        {
            var participantApp = await dbContext.DealApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
                .ConfigureAwait(false);

            if (participantApp is null
                || (participantApp.TenantUserId != request.CallerUserId
                    && participantApp.LandlordUserId != request.CallerUserId))
            {
                return Result<PaymentConfirmationDto>.Failure(
                    new Error("PaymentConfirmation.Forbidden",
                        "You do not have access to this deal's payment confirmation."));
            }
        }

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
