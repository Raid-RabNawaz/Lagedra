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
    IUserLookupService userLookup,
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

        var hostVerificationTask = hostVerificationProvider
            .GetVerificationAsync(listing.LandlordUserId, cancellationToken);
        var hostProfileTask = hostProfileProvider
            .GetProfileAsync(listing.LandlordUserId, cancellationToken);

        var reviewReputationProvider = serviceProvider.GetService<IReviewReputationProvider>();
        var hostReputationTask = reviewReputationProvider is null
            ? Task.FromResult<UserReputationDto?>(null)
            : reviewReputationProvider.GetListingHostReputationAsync(listing.Id, cancellationToken);

        await Task.WhenAll(hostVerificationTask, hostProfileTask, hostReputationTask)
            .ConfigureAwait(false);

        var hostVerification = await hostVerificationTask.ConfigureAwait(false);
        var hostProfile = await hostProfileTask.ConfigureAwait(false);
        var hostReputation = await hostReputationTask.ConfigureAwait(false);

        var badges = hostVerification is not null
            ? new ListingVerificationBadgesDto(
                hostVerification.IsVerified,
                hostVerification.IsKycComplete,
                null)
            : null;

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

        ListingHomeOwnerDto? homeOwner = null;
        if (listing.HomeOwnerUserId is Guid ownerId
            && (isOwner || request.RequesterIsPlatformAdmin))
        {
            var account = await userLookup
                .FindAccountByIdAsync(ownerId, cancellationToken)
                .ConfigureAwait(false);
            if (account is not null)
            {
                homeOwner = new ListingHomeOwnerDto(account.UserId, account.DisplayName, account.Email);
            }
        }

        var details = ListingMapper.ToDetails(listing, badges, hostProfile, qualityScore, homeOwner);

        // Public shoppers (and any non-owner) only get city/region — never the
        // street or ZIP — until they are a confirmed booking party (see deal
        // stay-access endpoint). Owners and platform admins keep the full address.
        if (!isOwner && !request.RequesterIsPlatformAdmin)
        {
            details = details with
            {
                PreciseAddress = details.PreciseAddress is { } addr
                    ? new AddressDto(
                        string.Empty,
                        addr.City,
                        addr.State,
                        string.Empty,
                        addr.Country)
                    : null,
                HomeOwnerUserId = null,
                HomeOwner = null,
            };
        }

        return Result<ListingDetailsDto>.Success(details);
    }
}
