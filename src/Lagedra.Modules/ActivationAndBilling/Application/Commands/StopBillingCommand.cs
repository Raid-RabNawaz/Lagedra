using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record StopBillingCommand(Guid DealId) : IRequest<Result<BillingStatusDto>>;

public sealed class StopBillingCommandHandler(
    BillingDbContext dbContext)
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

        account.Close();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<BillingStatusDto>.Success(
            new BillingStatusDto(account.Id, account.DealId, account.Status,
                account.StartDate, account.EndDate,
                account.StripeCustomerId, account.StripeSubscriptionId,
                account.Invoices.Count,
                account.Invoices.Count(i => i.Status == InvoiceStatus.Paid)));
    }
}
