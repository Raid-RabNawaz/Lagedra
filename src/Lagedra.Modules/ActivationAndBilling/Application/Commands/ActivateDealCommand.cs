using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ActivateDealCommand(Guid DealId) : IRequest<Result<BillingStatusDto>>;

public sealed class ActivateDealCommandHandler(
    BillingDbContext dbContext,
    ListingsDbContext listingsDbContext,
    IClock clock)
    : IRequestHandler<ActivateDealCommand, Result<BillingStatusDto>>
{
    public async Task<Result<BillingStatusDto>> Handle(
        ActivateDealCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null)
        {
            return Result<BillingStatusDto>.Failure(
                new Error("Deal.NotFound", "No approved application found for this deal."));
        }

        var paymentConfirmation = await dbContext.DealPaymentConfirmations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (paymentConfirmation is null
            || paymentConfirmation.Status != PaymentConfirmationStatus.Confirmed)
        {
            return Result<BillingStatusDto>.Failure(
                new Error("Deal.PaymentNotConfirmed",
                    "Payment must be confirmed before activating the deal."));
        }

        if (!paymentConfirmation.HostPaidPlatform)
        {
            return Result<BillingStatusDto>.Failure(
                new Error("Deal.HostNotPaidPlatform",
                    "Host must pay platform fees (insurance + activation) before deal activation."));
        }

        var existingAccount = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (existingAccount is not null)
        {
            return Result<BillingStatusDto>.Failure(
                new Error("Deal.AlreadyActivated", "A billing account already exists for this deal."));
        }

        var account = BillingAccount.Create(
            request.DealId,
            application.LandlordUserId,
            application.TenantUserId,
            clock.UtcNow);

        account.Activate();

        var availabilityBlock = ListingAvailabilityBlock.CreateBooked(
            application.ListingId,
            request.DealId,
            application.RequestedCheckIn,
            application.RequestedCheckOut);

        dbContext.BillingAccounts.Add(account);
        listingsDbContext.ListingAvailabilityBlocks.Add(availabilityBlock);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await listingsDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<BillingStatusDto>.Success(
            new BillingStatusDto(account.Id, account.DealId, account.Status,
                account.StartDate, account.EndDate,
                account.StripeCustomerId, account.StripeSubscriptionId, 0, 0));
    }
}
