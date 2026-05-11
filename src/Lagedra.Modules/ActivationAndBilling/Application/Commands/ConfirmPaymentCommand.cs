using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ConfirmPaymentCommand(
    Guid DealId,
    Guid HostUserId) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class ConfirmPaymentCommandHandler(
    BillingDbContext dbContext,
    IClock clock)
    : IRequestHandler<ConfirmPaymentCommand, Result<PaymentConfirmationDto>>
{
    private static readonly Error Forbidden = new(
        "PaymentConfirmation.Forbidden",
        "Only the listing host can confirm payment receipt for this deal.");

    public async Task<Result<PaymentConfirmationDto>> Handle(
        ConfirmPaymentCommand request,
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

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null || application.LandlordUserId != request.HostUserId)
        {
            return Result<PaymentConfirmationDto>.Failure(Forbidden);
        }

        if (confirmation.Status == PaymentConfirmationStatus.Confirmed)
        {
            return Result<PaymentConfirmationDto>.Success(MapToDto(confirmation));
        }

        confirmation.ConfirmByHost(clock);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PaymentConfirmationDto>.Success(MapToDto(confirmation));
    }

    private static PaymentConfirmationDto MapToDto(Domain.Aggregates.DealPaymentConfirmation c) =>
        new(c.Id, c.DealId, c.Status, c.HostConfirmed, c.HostConfirmedAt,
            c.TenantDisputed, c.TenantDisputedAt, c.DisputeReason, c.GracePeriodExpiresAt,
            c.TotalTenantPaymentCents, c.TotalHostPlatformPaymentCents,
            c.FirstMonthRentCents, c.DepositAmountCents,
            c.InsuranceFeeCents, c.MonthlyProtocolFeeCents,
            c.HostPaidPlatform, c.HostPaidPlatformAt);
}
