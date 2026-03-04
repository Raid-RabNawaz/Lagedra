using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record CancelBookingByHostCommand(
    Guid DealId,
    Guid HostUserId) : IRequest<Result>;

public sealed class CancelBookingByHostCommandHandler(
    BillingDbContext dbContext,
    IClock clock)
    : IRequestHandler<CancelBookingByHostCommand, Result>
{
    public async Task<Result> Handle(CancelBookingByHostCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result.Failure(new Error("Booking.NotFound", "Payment confirmation not found."));
        }

        if (confirmation.Status != PaymentConfirmationStatus.Pending)
        {
            return Result.Failure(new Error("Booking.InvalidStatus",
                "Can only cancel bookings with pending payment."));
        }

        if (!confirmation.IsGracePeriodExpired(clock))
        {
            return Result.Failure(new Error("Booking.NotOverdue",
                "Cannot cancel before the payment grace period expires."));
        }

        var application = await dbContext.DealApplications
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null || application.LandlordUserId != request.HostUserId)
        {
            return Result.Failure(new Error("Booking.Unauthorized",
                "Only the listing host can cancel this booking."));
        }

        confirmation.Cancel("Cancelled by host: tenant payment overdue.", clock);
        application.Cancel(
            request.HostUserId,
            "Cancelled by host: tenant payment overdue.",
            isAutoCancel: false,
            refundAmountCents: 0,
            insuranceRefundCents: 0);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
