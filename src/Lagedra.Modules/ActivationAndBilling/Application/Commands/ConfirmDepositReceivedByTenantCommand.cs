using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// The tenant confirms they received their deposit back from the host. This
/// completes the handshake (and the deal) once the host has also confirmed the
/// return. If the deposit was not received, the tenant raises an arbitration
/// case instead of confirming.
/// </summary>
public sealed record ConfirmDepositReceivedByTenantCommand(
    Guid DealId,
    Guid TenantUserId) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class ConfirmDepositReceivedByTenantCommandHandler(
    BillingDbContext dbContext,
    IClock clock)
    : IRequestHandler<ConfirmDepositReceivedByTenantCommand, Result<PaymentConfirmationDto>>
{
    private static readonly Error Forbidden = new(
        "DepositReturn.Forbidden",
        "Only the deal's tenant can confirm the deposit was received.");

    public async Task<Result<PaymentConfirmationDto>> Handle(
        ConfirmDepositReceivedByTenantCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null || application.TenantUserId != request.TenantUserId)
        {
            return Result<PaymentConfirmationDto>.Failure(Forbidden);
        }

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("PaymentConfirmation.NotFound",
                    "No payment confirmation record found for this deal."));
        }

        var billingClosed = await dbContext.BillingAccounts
            .AnyAsync(b => b.DealId == request.DealId
                && b.Status == BillingAccountStatus.Closed, cancellationToken)
            .ConfigureAwait(false);

        if (!billingClosed)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("DepositReturn.NotEnded",
                    "The stay must be ended before confirming deposit receipt."));
        }

        try
        {
            confirmation.ConfirmDepositReceivedByTenant(clock);
        }
        catch (InvalidOperationException ex)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("DepositReturn.Invalid", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PaymentConfirmationDto>.Success(
            PaymentConfirmationDtoMapper.ToDto(confirmation));
    }
}
