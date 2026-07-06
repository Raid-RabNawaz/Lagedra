using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;

public sealed class DealApplicationStatusProvider(BillingDbContext dbContext) : IDealApplicationStatusProvider
{
    public async Task<bool> IsApprovedAsync(Guid dealId, CancellationToken ct = default)
    {
        return await dbContext.DealApplications
            .AsNoTracking()
            .AnyAsync(a => a.DealId == dealId && a.Status == DealApplicationStatus.Approved, ct)
            .ConfigureAwait(false);
    }

    public async Task<DealParticipantsDto?> GetParticipantsAsync(Guid dealId, CancellationToken ct = default)
    {
        var app = await dbContext.DealApplications
            .AsNoTracking()
            .Where(a => a.DealId == dealId)
            .Select(a => new { a.LandlordUserId, a.TenantUserId, a.ListingId })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return app is null ? null : new DealParticipantsDto(app.LandlordUserId, app.TenantUserId, app.ListingId);
    }

    public async Task<DateOnly?> GetRequestedCheckOutAsync(Guid dealId, CancellationToken ct = default)
    {
        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == dealId, ct)
            .ConfigureAwait(false);

        return application?.RequestedCheckOut;
    }

    public async Task<DealApplicationDetailsDto?> GetDealDetailsAsync(Guid dealId, CancellationToken ct = default)
    {
        // Accept Approved and PaymentFailed: a sealed Truth Surface / payment
        // retry can run while the booking sits in PaymentFailed.
        var app = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == dealId
                                      && (a.Status == DealApplicationStatus.Approved
                                          || a.Status == DealApplicationStatus.PaymentFailed), ct)
            .ConfigureAwait(false);

        if (app is null)
        {
            return null;
        }

        return new DealApplicationDetailsDto(
            app.Id,
            dealId,
            app.ListingId,
            app.TenantUserId,
            app.LandlordUserId,
            app.RequestedCheckIn,
            app.RequestedCheckOut,
            app.StayDurationDays,
            app.FirstMonthRentCents,
            app.DepositAmountCents,
            app.InsuranceFeeCents,
            app.JurisdictionWarning,
            app.GuestCount,
            app.Message,
            app.ServiceFeeCents,
            app.TotalPayableSnapshotCents,
            app.TenantVerificationTierAtRequest,
            app.DepositReason);
    }
}
