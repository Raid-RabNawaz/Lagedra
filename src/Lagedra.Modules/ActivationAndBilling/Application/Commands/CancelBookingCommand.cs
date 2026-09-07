using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Policies;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record CancelBookingCommand(
    Guid DealId,
    Guid CancelledByUserId,
    string Reason) : IRequest<Result<CancellationResultDto>>;

public sealed partial class CancelBookingCommandHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider,
    IStripeService stripeService,
    IClock clock,
    ILogger<CancelBookingCommandHandler> logger)
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

        if (application.TenantUserId != request.CancelledByUserId
            && application.LandlordUserId != request.CancelledByUserId)
        {
            return Result<CancellationResultDto>.Failure(
                new Error("Cancel.Forbidden",
                    "Only the deal's tenant or host can cancel this booking."));
        }

        if (application.Status == DealApplicationStatus.Cancelled)
        {
            return Result<CancellationResultDto>.Failure(
                new Error("Cancel.AlreadyCancelled", "Booking is already cancelled."));
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(application.ListingId, cancellationToken)
            .ConfigureAwait(false);

        int freeCancellationDays = 14;
        int? partialRefundPercent = 50;
        int? partialRefundDays = 7;

        if (listing?.CancellationPolicy is { } policy)
        {
            freeCancellationDays = policy.FreeCancellationDays;
            partialRefundPercent = policy.PartialRefundPercent;
            partialRefundDays = policy.PartialRefundDays;
        }

        var today = DateOnly.FromDateTime(clock.UtcNow);

        var refund = CancellationRefundCalculator.Calculate(
            application.RequestedCheckIn,
            today,
            (application.FirstMonthRentCents ?? 0) + (application.DepositAmountCents ?? 0),
            application.InsuranceFeeCents ?? 0,
            freeCancellationDays,
            partialRefundPercent,
            partialRefundDays,
            StayProtectionFee.ScreeningFeeCents);

        application.Cancel(
            request.CancelledByUserId,
            request.Reason,
            isAutoCancel: false,
            refund.TenantRefundCents,
            refund.InsuranceRefundCents);

        var paymentConfirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (refund.TenantRefundCents > 0
            && paymentConfirmation is not null
            && !string.IsNullOrEmpty(paymentConfirmation.StripePaymentIntentId)
            && paymentConfirmation.StripePaymentStatus == "succeeded")
        {
            try
            {
                var idempotencyKey = $"refund-deal-{request.DealId}";
                // Non-custodial (Option A): the refundable rent + deposit was
                // transferred to the host's connected account at booking, so reverse
                // the transfer to claw it back before refunding the tenant. The
                // platform retains its service fee. Stay protection is refunded
                // separately per policy (minus the $1 screening remainder), so
                // the application fee is not refunded here.
                await stripeService.RefundPaymentIntentAsync(
                    paymentConfirmation.StripePaymentIntentId,
                    refund.TenantRefundCents,
                    reverseTransfer: true,
                    refundApplicationFee: false,
                    idempotencyKey,
                    cancellationToken).ConfigureAwait(false);

                LogRefundIssued(logger, request.DealId, refund.TenantRefundCents);
            }
            catch (Stripe.StripeException ex)
            {
                LogRefundFailed(logger, request.DealId, ex);
            }
        }

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

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Stripe refund issued for deal {DealId}: {AmountCents} cents")]
    private static partial void LogRefundIssued(ILogger logger, Guid dealId, long amountCents);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to issue Stripe refund for deal {DealId}")]
    private static partial void LogRefundFailed(ILogger logger, Guid dealId, Exception ex);
}
