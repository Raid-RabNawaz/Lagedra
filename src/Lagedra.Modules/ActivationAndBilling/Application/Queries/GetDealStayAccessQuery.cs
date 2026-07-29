using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

/// <summary>
/// Post-booking reveal: full listing address + counterpart contact details.
/// Only the deal's landlord or tenant may call this, and only once the booking
/// is confirmed (Active / deposit-return / Closed) — never on the public listing.
/// </summary>
public sealed record GetDealStayAccessQuery(
    Guid DealId,
    Guid CallerUserId) : IRequest<Result<DealStayAccessDto>>;

public sealed record DealStayAccessDto(
    Guid DealId,
    string DealPhase,
    bool IsUnlocked,
    string? LockedReason,
    DealStayAddressDto? PropertyAddress,
    DealStayContactDto? Counterpart);

public sealed record DealStayAddressDto(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country);

public sealed record DealStayContactDto(
    Guid UserId,
    string FullName,
    string? Email,
    string? Phone,
    string Role);

public sealed class GetDealStayAccessQueryHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider,
    ILeasePartyProfileProvider partyProfileProvider,
    ITruthSurfaceStatusProvider truthSurfaceStatusProvider)
    : IRequestHandler<GetDealStayAccessQuery, Result<DealStayAccessDto>>
{
    private static readonly Error NotFound = new("Deal.NotFound", "Deal not found.");
    private static readonly Error Forbidden = new("Deal.Forbidden", "You are not a party to this deal.");

    public async Task<Result<DealStayAccessDto>> Handle(
        GetDealStayAccessQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null || application.DealId is null)
        {
            return Result<DealStayAccessDto>.Failure(NotFound);
        }

        var isLandlord = application.LandlordUserId == request.CallerUserId;
        var isTenant = application.TenantUserId == request.CallerUserId;
        if (!isLandlord && !isTenant)
        {
            return Result<DealStayAccessDto>.Failure(Forbidden);
        }

        var billing = await dbContext.BillingAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        var payment = await dbContext.DealPaymentConfirmations
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        var truthStatuses = await truthSurfaceStatusProvider
            .GetStatusesForDealsAsync([request.DealId], cancellationToken)
            .ConfigureAwait(false);
        truthStatuses.TryGetValue(request.DealId, out var truthSurface);

        var phase = ComputePhase(application, billing, payment, truthSurface);
        var unlocked = phase is "Active" or "AwaitingDepositReturn" or "Closed";

        if (!unlocked)
        {
            return Result<DealStayAccessDto>.Success(new DealStayAccessDto(
                request.DealId,
                phase,
                IsUnlocked: false,
                LockedReason:
                    "Full address and contact details unlock after the booking is confirmed (deposit payment clears).",
                PropertyAddress: null,
                Counterpart: null));
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(application.ListingId, cancellationToken)
            .ConfigureAwait(false);

        DealStayAddressDto? address = listing?.PreciseAddress is { } addr
            && !string.IsNullOrWhiteSpace(addr.Street)
            ? new DealStayAddressDto(addr.Street, addr.City, addr.State, addr.ZipCode, addr.Country)
            : null;

        var counterpartUserId = isTenant ? application.LandlordUserId : application.TenantUserId;
        var counterpartRole = isTenant ? "Host" : "Guest";
        var profile = await partyProfileProvider
            .GetAsync(counterpartUserId, cancellationToken)
            .ConfigureAwait(false);

        DealStayContactDto? counterpart = profile is null
            ? null
            : new DealStayContactDto(
                profile.UserId,
                profile.FullName,
                profile.Email,
                profile.Phone,
                counterpartRole);

        return Result<DealStayAccessDto>.Success(new DealStayAccessDto(
            request.DealId,
            phase,
            IsUnlocked: true,
            LockedReason: null,
            address,
            counterpart));
    }

    // Mirrors ListMyDealsQuery.ComputeDealPhase so stay-access unlocks on the
    // same phases the UI already treats as a confirmed booking.
    private static string ComputePhase(
        Domain.Aggregates.DealApplication app,
        Domain.Aggregates.BillingAccount? billing,
        Domain.Aggregates.DealPaymentConfirmation? payment,
        TruthSurfaceSnapshotInfo? truthSurface)
    {
        if (app.Status == DealApplicationStatus.Cancelled)
        {
            return "Cancelled";
        }

        if (app.Status == DealApplicationStatus.PaymentFailed
            && billing is not { Status: BillingAccountStatus.Active })
        {
            return "PaymentFailed";
        }

        if (billing is not null)
        {
            return billing.Status switch
            {
                BillingAccountStatus.Active => "Active",
                BillingAccountStatus.Suspended => "Active",
                BillingAccountStatus.Closed =>
                    payment is { Status: PaymentConfirmationStatus.Confirmed }
                        && payment.DepositAmountCents > 0
                        && payment.DepositReturnSettledAt is null
                        ? "AwaitingDepositReturn"
                        : "Closed",
                _ => DerivePreActivationPhase(payment, truthSurface),
            };
        }

        return DerivePreActivationPhase(payment, truthSurface);
    }

    private static string DerivePreActivationPhase(
        Domain.Aggregates.DealPaymentConfirmation? payment,
        TruthSurfaceSnapshotInfo? truthSurface)
    {
        if (payment is not null)
        {
            return payment.Status switch
            {
                PaymentConfirmationStatus.Pending => "Checkout",
                PaymentConfirmationStatus.PaymentMethodProvided => "Checkout",
                PaymentConfirmationStatus.CapturePending => "Checkout",
                PaymentConfirmationStatus.Confirmed => "Active",
                PaymentConfirmationStatus.Disputed => "Checkout",
                PaymentConfirmationStatus.Failed => "PaymentFailed",
                PaymentConfirmationStatus.Rejected => "Cancelled",
                PaymentConfirmationStatus.Cancelled => "Cancelled",
                _ => "TruthSurface",
            };
        }

        if (truthSurface is not null)
        {
            return truthSurface.IsSealed ? "Checkout" : "TruthSurface";
        }

        return "TruthSurface";
    }
}
