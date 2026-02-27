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

public sealed class LockPreciseAddressOnActivationCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<LockPreciseAddressOnActivationCommand, Result<ListingDetailsDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<ListingDetailsDto>> Handle(
        LockPreciseAddressOnActivationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
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

        listing.LockPreciseAddress(address, request.JurisdictionCode);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }
}
