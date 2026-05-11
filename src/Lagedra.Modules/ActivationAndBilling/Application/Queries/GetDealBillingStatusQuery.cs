using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record GetDealBillingStatusQuery(
    Guid DealId,
    Guid CallerUserId,
    bool IsAdmin = false) : IRequest<Result<BillingStatusDto>>;

public sealed class GetDealBillingStatusQueryHandler(
    BillingDbContext dbContext)
    : IRequestHandler<GetDealBillingStatusQuery, Result<BillingStatusDto>>
{
    public async Task<Result<BillingStatusDto>> Handle(
        GetDealBillingStatusQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = await dbContext.BillingAccounts
            .AsNoTracking()
            .Include(b => b.Invoices)
            .FirstOrDefaultAsync(b => b.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result<BillingStatusDto>.Failure(
                new Error("BillingAccount.NotFound", "Billing account not found for this deal."));
        }

        // The Tenant/Landlord roles were merged into "Member", so participation is
        // verified per-deal: only the deal's tenant, host, or a platform admin may
        // view billing details.
        if (!request.IsAdmin
            && account.TenantUserId != request.CallerUserId
            && account.LandlordUserId != request.CallerUserId)
        {
            return Result<BillingStatusDto>.Failure(
                new Error("BillingAccount.Forbidden",
                    "You do not have access to this deal's billing account."));
        }

        return Result<BillingStatusDto>.Success(
            new BillingStatusDto(account.Id, account.DealId, account.Status,
                account.StartDate, account.EndDate,
                account.StripeCustomerId, account.StripeSubscriptionId,
                account.Invoices.Count,
                account.Invoices.Count(i => i.Status == InvoiceStatus.Paid)));
    }
}
