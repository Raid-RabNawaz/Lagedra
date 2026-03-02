using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record StopBillingCommand(Guid DealId) : IRequest<Result<BillingStatusDto>>;

public sealed partial class StopBillingCommandHandler(
    BillingDbContext dbContext,
    IStripeService stripeService,
    ILogger<StopBillingCommandHandler> logger)
    : IRequestHandler<StopBillingCommand, Result<BillingStatusDto>>
{
    public async Task<Result<BillingStatusDto>> Handle(
        StopBillingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = await dbContext.BillingAccounts
            .Include(b => b.Invoices)
            .FirstOrDefaultAsync(b => b.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result<BillingStatusDto>.Failure(
                new Error("BillingAccount.NotFound", "Billing account not found for this deal."));
        }

        if (!string.IsNullOrEmpty(account.StripeSubscriptionId))
        {
            try
            {
                await stripeService.CancelSubscriptionAsync(account.StripeSubscriptionId, cancellationToken)
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

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<BillingStatusDto>.Success(
            new BillingStatusDto(account.Id, account.DealId, account.Status,
                account.StartDate, account.EndDate,
                account.StripeCustomerId, account.StripeSubscriptionId,
                account.Invoices.Count,
                account.Invoices.Count(i => i.Status == InvoiceStatus.Paid)));
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to cancel Stripe subscription for deal {DealId}, subscription {SubscriptionId}")]
    private static partial void LogStripeCancelFailed(ILogger logger, Guid dealId, string subscriptionId, Exception ex);
}
