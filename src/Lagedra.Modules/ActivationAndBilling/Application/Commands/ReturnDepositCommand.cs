using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ReturnDepositCommand(Guid DealId) : IRequest<Result>;

public sealed partial class ReturnDepositCommandHandler(
    BillingDbContext dbContext,
    IStripeService stripeService,
    ILogger<ReturnDepositCommandHandler> logger)
    : IRequestHandler<ReturnDepositCommand, Result>
{
    public async Task<Result> Handle(
        ReturnDepositCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result.Failure(
                new Error("Deposit.NoConfirmation", "No payment confirmation found for this deal."));
        }

        if (confirmation.Status != PaymentConfirmationStatus.Confirmed)
        {
            return Result.Failure(
                new Error("Deposit.NotConfirmed", "Payment is not in confirmed status."));
        }

        if (string.IsNullOrEmpty(confirmation.StripePaymentIntentId)
            || confirmation.StripePaymentStatus != "succeeded")
        {
            return Result.Failure(
                new Error("Deposit.NotPaid", "No successful Stripe payment to refund."));
        }

        var openClaims = await dbContext.DamageClaims
            .AnyAsync(c => c.DealId == request.DealId
                && c.Status != DamageClaimStatus.Rejected
                && c.Status != DamageClaimStatus.Settled, cancellationToken)
            .ConfigureAwait(false);

        if (openClaims)
        {
            return Result.Failure(
                new Error("Deposit.OpenClaims", "Cannot return deposit while damage claims are unresolved."));
        }

        var settledDeductions = await dbContext.DamageClaims
            .Where(c => c.DealId == request.DealId && c.Status == DamageClaimStatus.Settled)
            .SumAsync(c => c.DepositDeductionCents, cancellationToken)
            .ConfigureAwait(false);

        var returnAmount = confirmation.DepositAmountCents - settledDeductions;

        if (returnAmount <= 0)
        {
            LogNoDepositToReturn(logger, request.DealId, settledDeductions);
            return Result.Success();
        }

        try
        {
            var idempotencyKey = $"deposit-return-deal-{request.DealId}";
            // Non-custodial (Option A): the deposit was transferred to the host's
            // connected account at booking, so reverse the transfer to pull it back
            // before refunding the tenant. The platform keeps its service/insurance
            // fee, so the application fee is not refunded.
            var refund = await stripeService.RefundPaymentIntentAsync(
                confirmation.StripePaymentIntentId,
                returnAmount,
                reverseTransfer: true,
                refundApplicationFee: false,
                idempotencyKey,
                cancellationToken).ConfigureAwait(false);

            LogDepositReturned(logger, request.DealId, returnAmount, refund.RefundId);
        }
        catch (Stripe.StripeException ex)
        {
            LogDepositReturnFailed(logger, request.DealId, returnAmount, ex);
            return Result.Failure(
                new Error("Deposit.RefundFailed", "Failed to issue deposit refund through Stripe."));
        }

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "No deposit to return for deal {DealId} — deductions ({DeductionCents} cents) consumed entire deposit")]
    private static partial void LogNoDepositToReturn(ILogger logger, Guid dealId, long deductionCents);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Deposit returned for deal {DealId}: {AmountCents} cents, refund {RefundId}")]
    private static partial void LogDepositReturned(ILogger logger, Guid dealId, long amountCents, string refundId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to return deposit for deal {DealId}: {AmountCents} cents")]
    private static partial void LogDepositReturnFailed(ILogger logger, Guid dealId, long amountCents, Exception ex);
}
