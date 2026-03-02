using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Policies;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record CancelBookingCommand(
    Guid DealId,
    Guid CancelledByUserId,
    string Reason,
    int FreeCancellationDays,
    int? PartialRefundPercent,
    int? PartialRefundDays) : IRequest<Result<CancellationResultDto>>;

public sealed class CancelBookingCommandHandler(
    BillingDbContext dbContext,
    IClock clock)
    : IRequestHandler<CancelBookingCommand, Result<CancellationResultDto>>
{
    public async Task<Result<CancellationResultDto>> Handle(
        CancelBookingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<CancellationResultDto>.Failure(
                new Error("Cancel.DealNotFound", "Deal application not found."));
        }

        if (application.Status == DealApplicationStatus.Cancelled)
        {
            return Result<CancellationResultDto>.Failure(
                new Error("Cancel.AlreadyCancelled", "Booking is already cancelled."));
        }

        var today = DateOnly.FromDateTime(clock.UtcNow);

        var refund = CancellationRefundCalculator.Calculate(
            application.RequestedCheckIn,
            today,
            (application.FirstMonthRentCents ?? 0) + (application.DepositAmountCents ?? 0),
            application.InsuranceFeeCents ?? 0,
            request.FreeCancellationDays,
            request.PartialRefundPercent,
            request.PartialRefundDays);

        application.Cancel(
            request.CancelledByUserId,
            request.Reason,
            isAutoCancel: false,
            refund.TenantRefundCents,
            refund.InsuranceRefundCents);

        var paymentConfirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        paymentConfirmation?.Cancel($"Booking cancelled: {request.Reason}", clock);

        var billingAccount = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (billingAccount is not null
            && billingAccount.Status is BillingAccountStatus.Active or BillingAccountStatus.Inactive)
        {
            billingAccount.Close();
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<CancellationResultDto>.Success(new CancellationResultDto(
            request.DealId,
            refund.TenantRefundCents,
            refund.InsuranceRefundCents,
            refund.PolicyApplied));
    }
}
