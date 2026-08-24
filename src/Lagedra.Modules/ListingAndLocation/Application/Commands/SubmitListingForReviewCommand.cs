using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using IHostStripeAccountProvider = Lagedra.SharedKernel.Integration.IHostStripeAccountProvider;
using IHostProfileProvider = Lagedra.SharedKernel.Integration.IHostProfileProvider;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

/// <summary>
/// Landlord submits a Draft (or previously Denied) listing for admin
/// review. The listing transitions to <see cref="Domain.Enums.ListingStatus.InReview"/>
/// and only becomes publicly visible after an admin approves it.
/// </summary>
public sealed record SubmitListingForReviewCommand(
    Guid ListingId,
    Guid CallerUserId) : IRequest<Result<ListingDetailsDto>>;

public sealed class SubmitListingForReviewCommandHandler(
    ListingsDbContext dbContext,
    IHostStripeAccountProvider hostStripeAccountProvider,
    IHostProfileProvider hostProfileProvider)
    : IRequestHandler<SubmitListingForReviewCommand, Result<ListingDetailsDto>>
{
    /// <summary>
    /// A host must fill in at least this much of their public profile before a
    /// listing can go to review. Guests authorising large bookings need to see
    /// who they're transacting with, so we refuse to publish a faceless host.
    /// Mirrored on the web client's submit gate.
    /// </summary>
    public const int MinimumProfileCompletenessPercent = 75;

    /// <summary>
    /// When true, a host must have Stripe charges + payouts enabled before
    /// submitting a listing for review. Temporarily off so drafts can go to
    /// review during onboarding; flip back to <c>true</c> to restore the gate.
    /// Accepting a booking still requires payouts.
    /// </summary>
    public const bool RequirePayoutSetupToSubmitForReview = false;

    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error Forbidden = new("Listing.Forbidden", "You do not own this listing.");
    private static readonly Error PayoutSetupRequired = new(
        "Listing.PayoutSetupRequired",
        "Add your payout details before submitting this listing. Guests pay through Lagedra, so your listing can only go live once there's a payout destination for the rent and deposit.");
    private static readonly Error PreciseAddressRequired = new(
        "Listing.PreciseAddressRequired",
        "Add and lock the full property address (including city) before submitting this listing. The city becomes part of the binding booking agreement, so it can't be left blank.");

    public async Task<Result<ListingDetailsDto>> Handle(
        SubmitListingForReviewCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .AsSplitQuery()
            .Include(l => l.Amenities).ThenInclude(a => a.AmenityDefinition)
            .Include(l => l.SafetyDevices).ThenInclude(s => s.SafetyDeviceDefinition)
            .Include(l => l.Considerations).ThenInclude(c => c.ConsiderationDefinition)
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<ListingDetailsDto>.Failure(NotFound);
        }

        if (listing.LandlordUserId != request.CallerUserId)
        {
            return Result<ListingDetailsDto>.Failure(Forbidden);
        }

        // The binding Truth Surface seals the property's city from the precise
        // address. Require it up front so the agreement never seals blank, and
        // return a friendly message instead of letting the domain throw.
        if (listing.PreciseAddress is null)
        {
            return Result<ListingDetailsDto>.Failure(PreciseAddressRequired);
        }

        // A listing that passes review becomes publicly bookable, and every
        // booking charges the guest through the platform. Without a payout
        // destination those funds would have nowhere to go. Temporarily
        // skipped — set RequirePayoutSetupToSubmitForReview back to true
        // to restore this gate. Accepting a booking still requires payouts.
        if (RequirePayoutSetupToSubmitForReview
            && !await HostHasPayoutsAsync(listing.LandlordUserId, cancellationToken).ConfigureAwait(false))
        {
            return Result<ListingDetailsDto>.Failure(PayoutSetupRequired);
        }

        // Guests need to know who they're renting from before authorising a
        // booking, so a faceless host can't go live. Require a sufficiently
        // complete public profile and tell the host exactly what's missing.
        var completeness = await hostProfileProvider
            .GetProfileCompletenessAsync(listing.LandlordUserId, cancellationToken)
            .ConfigureAwait(false);
        if (completeness.PercentComplete < MinimumProfileCompletenessPercent)
        {
            return Result<ListingDetailsDto>.Failure(ProfileIncomplete(completeness));
        }

        if (listing.ManagerRole == Domain.Enums.ListingManagerRole.PropertyManager
            && listing.HomeOwnerUserId is null)
        {
            return Result<ListingDetailsDto>.Failure(ListingManagementGuard.HomeOwnerRequired);
        }

        try
        {
            listing.SubmitForReview();
        }
        catch (InvalidOperationException ex)
        {
            return Result<ListingDetailsDto>.Failure(new Error("Listing.SubmitForReviewFailed", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }

    private static Error ProfileIncomplete(SharedKernel.Integration.HostProfileCompletenessDto completeness)
    {
        var detail = completeness.MissingFields.Count > 0
            ? $" Still missing: {string.Join(", ", completeness.MissingFields)}."
            : string.Empty;

        return new Error(
            "Listing.HostProfileIncomplete",
            $"Complete at least {MinimumProfileCompletenessPercent}% of your host profile before submitting " +
            $"this listing (you're at {completeness.PercentComplete}%). Guests need to see who they're " +
            $"renting from before they authorise a payment.{detail}");
    }

    // Mirrors the precondition enforced when a booking is charged (non-custodial,
    // Option A): a Stripe Connect account with charges + payouts enabled so the
    // destination charge for rent + deposit can settle straight to the host.
    private async Task<bool> HostHasPayoutsAsync(Guid hostUserId, CancellationToken cancellationToken)
    {
        var connectAccount = await hostStripeAccountProvider
            .GetByHostUserIdAsync(hostUserId, cancellationToken)
            .ConfigureAwait(false);
        return connectAccount is { ChargesEnabled: true, PayoutsEnabled: true };
    }
}
