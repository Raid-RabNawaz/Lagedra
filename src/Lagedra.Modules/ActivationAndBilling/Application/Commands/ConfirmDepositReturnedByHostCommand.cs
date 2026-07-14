using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// The host confirms they returned the deposit directly to the tenant (the
/// non-custodial model — Lagedra never holds the funds). Recording the amount
/// and method half-completes the handshake; the deal completes once the tenant
/// also confirms receipt. Partial returns require a deduction reason and a
/// sealed damage-photo evidence manifest.
/// </summary>
public sealed record ConfirmDepositReturnedByHostCommand(
    Guid DealId,
    Guid HostUserId,
    long ReturnedAmountCents,
    string? Method,
    string? Note,
    Guid? EvidenceManifestId = null) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class ConfirmDepositReturnedByHostCommandHandler(
    BillingDbContext dbContext,
    IClock clock,
    IEvidenceManifestProvider evidenceProvider)
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

        if (request.ReturnedAmountCents < confirmation.DepositAmountCents)
        {
            if (request.EvidenceManifestId is null || request.EvidenceManifestId == Guid.Empty)
            {
                return Result<PaymentConfirmationDto>.Failure(
                    new Error("DepositReturn.EvidenceRequired",
                        "A damage photo is required when returning less than the full deposit."));
            }

            var sealedOk = await evidenceProvider
                .ExistsAndIsSealedAsync(request.EvidenceManifestId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (!sealedOk)
            {
                return Result<PaymentConfirmationDto>.Failure(
                    new Error("DepositReturn.EvidenceNotSealed",
                        "Upload and seal a damage photo before confirming a partial deposit return."));
            }
        }

        try
        {
            confirmation.ConfirmDepositReturnedByHost(
                request.ReturnedAmountCents,
                request.Method,
                request.Note,
                request.EvidenceManifestId,
                clock);
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
