using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ConfirmHostPlatformPaymentCommand(
    Guid DealId) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class ConfirmHostPlatformPaymentCommandHandler(
    BillingDbContext dbContext,
    IClock clock)
    : IRequestHandler<ConfirmHostPlatformPaymentCommand, Result<PaymentConfirmationDto>>
{
    public async Task<Result<PaymentConfirmationDto>> Handle(
        ConfirmHostPlatformPaymentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("PaymentConfirmation.NotFound", "Payment confirmation record not found."));
        }

        confirmation.ConfirmHostPlatformPayment(clock);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PaymentConfirmationDto>.Success(
            new PaymentConfirmationDto(
                confirmation.Id, confirmation.DealId, confirmation.Status,
                confirmation.HostConfirmed, confirmation.HostConfirmedAt,
                confirmation.TenantDisputed, confirmation.TenantDisputedAt,
                confirmation.DisputeReason, confirmation.GracePeriodExpiresAt,
                confirmation.TotalTenantPaymentCents,
                confirmation.TotalHostPlatformPaymentCents,
                confirmation.FirstMonthRentCents,
                confirmation.DepositAmountCents,
                confirmation.InsuranceFeeCents,
                confirmation.MonthlyProtocolFeeCents,
                confirmation.HostPaidPlatform, confirmation.HostPaidPlatformAt));
    }
}
