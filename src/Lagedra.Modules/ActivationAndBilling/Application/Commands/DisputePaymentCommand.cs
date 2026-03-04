using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record DisputePaymentCommand(
    Guid DealId,
    Guid TenantUserId,
    string Reason,
    Guid? EvidenceManifestId) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class DisputePaymentCommandHandler(
    BillingDbContext dbContext,
    IClock clock)
    : IRequestHandler<DisputePaymentCommand, Result<PaymentConfirmationDto>>
{
    public async Task<Result<PaymentConfirmationDto>> Handle(
        DisputePaymentCommand request,
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

        confirmation.DisputeByTenant(
            request.TenantUserId,
            request.Reason,
            request.EvidenceManifestId,
            clock);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PaymentConfirmationDto>.Success(MapToDto(confirmation));
    }

    private static PaymentConfirmationDto MapToDto(Domain.Aggregates.DealPaymentConfirmation c) =>
        new(c.Id, c.DealId, c.Status, c.HostConfirmed, c.HostConfirmedAt,
            c.TenantDisputed, c.TenantDisputedAt, c.DisputeReason, c.GracePeriodExpiresAt,
            c.TotalTenantPaymentCents, c.TotalHostPlatformPaymentCents,
            c.HostPaidPlatform, c.HostPaidPlatformAt);
}
