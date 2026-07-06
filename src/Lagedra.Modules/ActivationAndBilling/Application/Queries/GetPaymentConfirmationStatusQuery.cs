using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
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

        long? netReturnable = null;
        if (confirmation.DepositAmountCents > 0)
        {
            // What the host is expected to return: the deposit less any damage
            // deductions the platform has approved/settled. Rejected claims
            // carry a zero deduction, so only Approved/PartiallyApproved/Settled
            // reduce the returnable amount.
            var deductions = await dbContext.DamageClaims
                .AsNoTracking()
                .Where(c => c.DealId == request.DealId
                    && (c.Status == DamageClaimStatus.Approved
                        || c.Status == DamageClaimStatus.PartiallyApproved
                        || c.Status == DamageClaimStatus.Settled))
                .SumAsync(c => c.DepositDeductionCents, cancellationToken)
                .ConfigureAwait(false);

            netReturnable = Math.Max(0, confirmation.DepositAmountCents - deductions);
        }

        return Result<PaymentConfirmationDto>.Success(
            PaymentConfirmationDtoMapper.ToDto(confirmation, netReturnable));
    }
}
