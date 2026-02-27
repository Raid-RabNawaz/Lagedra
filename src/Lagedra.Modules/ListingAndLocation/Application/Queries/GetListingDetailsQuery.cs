using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetListingDetailsQuery(Guid ListingId) : IRequest<Result<ListingDetailsDto>>;

public sealed class GetListingDetailsQueryHandler(
    ListingsDbContext dbContext,
    IHostVerificationProvider hostVerificationProvider,
    IHostProfileProvider hostProfileProvider)
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

        var qualityScore = ListingQualityScoreCalculator.Calculate(
            listing.Photos.Count,
            listing.Description.Length,
            listing.Amenities.Count,
            listing.SafetyDevices.Count,
            listing.HouseRules is not null,
            listing.CancellationPolicy is not null,
            hostVerification?.IsVerified ?? false,
            hostProfile?.ResponseRatePercent);

        return Result<ListingDetailsDto>.Success(
            ListingMapper.ToDetails(listing, badges, hostProfile, qualityScore));
    }
}
