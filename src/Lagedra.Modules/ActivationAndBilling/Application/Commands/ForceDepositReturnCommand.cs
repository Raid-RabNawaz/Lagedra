using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// Admin / arbitration-enforced fallback for when the host does not return the
/// deposit off-platform. Runs the retained Stripe reverse-transfer refund
/// (<see cref="ReturnDepositCommand"/>) to pull the deposit back from the host's
/// connected account and refund the tenant, then marks the handshake settled.
/// This is the only path that still moves deposit money through Lagedra.
/// </summary>
public sealed record ForceDepositReturnCommand(
    Guid DealId,
    Guid AdminUserId) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class ForceDepositReturnCommandHandler(
    BillingDbContext dbContext,
    IMediator mediator,
    IClock clock)
    : IRequestHandler<ForceDepositReturnCommand, Result<PaymentConfirmationDto>>
{
    public async Task<Result<PaymentConfirmationDto>> Handle(
        ForceDepositReturnCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("PaymentConfirmation.NotFound",
                    "No payment confirmation record found for this deal."));
        }

        // Reverse-transfer the deposit back from the host and refund the tenant.
        var refundResult = await mediator
            .Send(new ReturnDepositCommand(request.DealId), cancellationToken)
            .ConfigureAwait(false);

        if (!refundResult.IsSuccess)
        {
            return Result<PaymentConfirmationDto>.Failure(refundResult.Error);
        }

        var settledDeductions = await dbContext.DamageClaims
            .Where(c => c.DealId == request.DealId && c.Status == DamageClaimStatus.Settled)
            .SumAsync(c => c.DepositDeductionCents, cancellationToken)
            .ConfigureAwait(false);

        var returnedAmount = Math.Max(0, confirmation.DepositAmountCents - settledDeductions);

        confirmation.MarkDepositReturnedByPlatform(returnedAmount, clock);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PaymentConfirmationDto>.Success(
            PaymentConfirmationDtoMapper.ToDto(confirmation));
    }
}
