using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using ListingDetailsDto = Lagedra.Modules.ListingAndLocation.Application.DTOs.ListingDetailsDto;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

/// <summary>
/// Loads a listing for display. Visibility is enforced server-side: anonymous
/// (or non-owner, non-admin) callers can only see listings whose status is
/// <see cref="ListingStatus.Published"/> or <see cref="ListingStatus.Activated"/>.
/// Drafts, in-review and denied listings are hidden (we return NotFound rather
/// than Forbidden to avoid leaking that the listing exists).
/// </summary>
public sealed record GetListingDetailsQuery(
    Guid ListingId,
    Guid? RequesterUserId = null,
    bool RequesterIsPlatformAdmin = false) : IRequest<Result<ListingDetailsDto>>;

public sealed class GetListingDetailsQueryHandler(
    ListingsDbContext dbContext,
    IHostVerificationProvider hostVerificationProvider,
    IHostProfileProvider hostProfileProvider,
    IServiceProvider serviceProvider)
    : IRequestHandler<GetListingDetailsQuery, Result<ListingDetailsDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<ListingDetailsDto>> Handle(
        GetListingDetailsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .AsNoTracking()
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

        // Visibility check: only the owner and platform admins can see a
        // listing that hasn't been admin-approved. Everyone else gets a 404.
        var isPubliclyVisible =
            listing.Status == ListingStatus.Published ||
            listing.Status == ListingStatus.Activated;
        var isOwner = request.RequesterUserId is Guid uid && uid == listing.LandlordUserId;
        if (!isPubliclyVisible && !isOwner && !request.RequesterIsPlatformAdmin)
        {
            return Result<ListingDetailsDto>.Failure(NotFound);
        }

        var hostVerification = await hostVerificationProvider
            .GetVerificationAsync(listing.LandlordUserId, cancellationToken)
            .ConfigureAwait(false);

        var hostProfile = await hostProfileProvider
            .GetProfileAsync(listing.LandlordUserId, cancellationToken)
            .ConfigureAwait(false);

        var badges = hostVerification is not null
            ? new ListingVerificationBadgesDto(
                hostVerification.IsVerified,
                hostVerification.IsKycComplete,
                null)
            : null;

        var reviewReputationProvider = serviceProvider.GetService<IReviewReputationProvider>();
        var hostReputation = reviewReputationProvider is null
            ? null
            : await reviewReputationProvider
                .GetListingHostReputationAsync(listing.Id, cancellationToken)
                .ConfigureAwait(false);

        var qualityScore = ListingQualityScoreCalculator.Calculate(
            listing.Photos.Count,
            listing.Description.Length,
            listing.Amenities.Count,
            listing.SafetyDevices.Count,
            listing.HouseRules is not null,
            listing.CancellationPolicy is not null,
            hostVerification?.IsVerified ?? false,
            hostProfile?.ResponseRatePercent,
            hostReputation?.AverageOverall);

        var details = ListingMapper.ToDetails(listing, badges, hostProfile, qualityScore);

        // Public shoppers (and any non-owner) only get city/region — never the
        // street or ZIP — until they are a confirmed booking party (see deal
        // stay-access endpoint). Owners and platform admins keep the full address.
        if (!isOwner && !request.RequesterIsPlatformAdmin && details.PreciseAddress is { } addr)
        {
            details = details with
            {
                PreciseAddress = new AddressDto(
                    string.Empty,
                    addr.City,
                    addr.State,
                    string.Empty,
                    addr.Country),
            };
        }

        return Result<ListingDetailsDto>.Success(details);
    }
}
