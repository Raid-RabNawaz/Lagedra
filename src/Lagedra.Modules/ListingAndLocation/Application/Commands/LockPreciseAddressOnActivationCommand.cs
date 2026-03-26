using Lagedra.Infrastructure.External.Geocoding;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record LockPreciseAddressOnActivationCommand(
    Guid ListingId,
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country,
    string? JurisdictionCode) : IRequest<Result<ListingDetailsDto>>;

public sealed class LockPreciseAddressOnActivationCommandHandler(
    ListingsDbContext dbContext,
    IGeocodingService geocodingService)
    : IRequestHandler<LockPreciseAddressOnActivationCommand, Result<ListingDetailsDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<ListingDetailsDto>> Handle(
        LockPreciseAddressOnActivationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
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

        var address = new Address(
            request.Street,
            request.City,
            request.State,
            request.ZipCode,
            request.Country);

        var fullAddress = $"{request.Street}, {request.City}, {request.State} {request.ZipCode}, {request.Country}";
        var geocoded = await geocodingService
            .GeocodeAddressAsync(fullAddress, cancellationToken)
            .ConfigureAwait(false);

        if (geocoded is not null)
        {
            listing.SetApproxLocation(new GeoPoint(geocoded.Latitude, geocoded.Longitude));
        }

        var jurisdictionCode = request.JurisdictionCode;
        if (string.IsNullOrWhiteSpace(jurisdictionCode))
        {
            var jurisdiction = await geocodingService
                .ResolveJurisdictionAsync(fullAddress, cancellationToken)
                .ConfigureAwait(false);

            jurisdictionCode = jurisdiction?.JurisdictionCode;
        }

        listing.LockPreciseAddress(address, jurisdictionCode);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }
}
