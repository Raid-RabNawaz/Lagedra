using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// The host confirms they returned the deposit directly to the tenant (the
/// non-custodial model — Lagedra never holds the funds). Recording the amount
/// and method half-completes the handshake; the deal completes once the tenant
/// also confirms receipt.
/// </summary>
public sealed record ConfirmDepositReturnedByHostCommand(
    Guid DealId,
    Guid HostUserId,
    long ReturnedAmountCents,
    string? Method,
    string? Note) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class ConfirmDepositReturnedByHostCommandHandler(
    BillingDbContext dbContext,
    IClock clock)
    : IRequestHandler<ConfirmDepositReturnedByHostCommand, Result<PaymentConfirmationDto>>
{
    private static readonly Error Forbidden = new(
        "DepositReturn.Forbidden",
        "Only the listing host can confirm the deposit was returned.");

    public async Task<Result<PaymentConfirmationDto>> Handle(
        ConfirmDepositReturnedByHostCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null || application.LandlordUserId != request.HostUserId)
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
                    "End the stay before confirming the deposit return."));
        }

        try
        {
            confirmation.ConfirmDepositReturnedByHost(
                request.ReturnedAmountCents, request.Method, request.Note, clock);
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
