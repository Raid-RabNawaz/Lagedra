using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// Either party ends the stay. This closes the recurring platform-fee billing
/// account and opens the deposit-return handshake. The deal only completes once
/// both parties confirm the deposit return (or it had no deposit to return).
/// </summary>
public sealed record BeginMoveOutCommand(
    Guid DealId,
    Guid CallerUserId,
    bool IsAdmin = false) : IRequest<Result<PaymentConfirmationDto>>;

public sealed partial class BeginMoveOutCommandHandler(
    BillingDbContext dbContext,
    IStripeService stripeService,
    IClock clock,
    ILogger<BeginMoveOutCommandHandler> logger)
    : IRequestHandler<BeginMoveOutCommand, Result<PaymentConfirmationDto>>
{
    public async Task<Result<PaymentConfirmationDto>> Handle(
        BeginMoveOutCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("PaymentConfirmation.NotFound", "Deal application not found."));
        }

        if (!request.IsAdmin
            && application.TenantUserId != request.CallerUserId
            && application.LandlordUserId != request.CallerUserId)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("DepositReturn.Forbidden",
                    "Only the deal's tenant or host can end this stay."));
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

        if (confirmation.Status != PaymentConfirmationStatus.Confirmed)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("DepositReturn.NotActive",
                    "Only an active booking can begin move-out."));
        }

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (account is not null && account.Status is not BillingAccountStatus.Closed)
        {
            if (!string.IsNullOrEmpty(account.StripeSubscriptionId))
            {
                try
                {
                    await stripeService
                        .CancelSubscriptionAsync(account.StripeSubscriptionId, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Stripe.StripeException ex)
                {
                    LogStripeCancelFailed(logger, account.DealId, account.StripeSubscriptionId, ex);
                }
                catch (HttpRequestException ex)
                {
                    LogStripeCancelFailed(logger, account.DealId, account.StripeSubscriptionId, ex);
                }
            }

            account.Close();
        }

        confirmation.BeginMoveOut(request.CallerUserId, clock);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PaymentConfirmationDto>.Success(
            PaymentConfirmationDtoMapper.ToDto(confirmation));
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "BeginMoveOut: failed to cancel Stripe subscription for deal {DealId}, subscription {SubscriptionId}")]
    private static partial void LogStripeCancelFailed(ILogger logger, Guid dealId, string subscriptionId, Exception ex);
}
