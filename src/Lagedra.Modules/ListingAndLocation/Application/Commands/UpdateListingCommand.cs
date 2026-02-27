using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record UpdateListingCommand(
    Guid ListingId,
    PropertyType PropertyType,
    string Title,
    string Description,
    long MonthlyRentCents,
    bool InsuranceRequired,
    int Bedrooms,
    decimal Bathrooms,
    int MinStayDays,
    int MaxStayDays,
    long MaxDepositCents,
    int? SquareFootage = null,
    HouseRulesDto? HouseRules = null,
    CancellationPolicyDto? CancellationPolicy = null,
    IReadOnlyList<Guid>? AmenityIds = null,
    IReadOnlyList<Guid>? SafetyDeviceIds = null,
    IReadOnlyList<Guid>? ConsiderationIds = null) : IRequest<Result<ListingDetailsDto>>;

public sealed class UpdateListingCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<UpdateListingCommand, Result<ListingDetailsDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<ListingDetailsDto>> Handle(
        UpdateListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .Include(l => l.Amenities)
            .Include(l => l.SafetyDevices)
            .Include(l => l.Considerations)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<ListingDetailsDto>.Failure(NotFound);
        }

        var stayRange = new StayRange(request.MinStayDays, request.MaxStayDays);

        listing.Update(
            request.PropertyType,
            request.Title,
            request.Description,
            request.MonthlyRentCents,
            request.InsuranceRequired,
            request.Bedrooms,
            request.Bathrooms,
            stayRange,
            request.MaxDepositCents,
            request.SquareFootage);

        if (request.HouseRules is { } hr)
        {
            listing.SetHouseRules(Domain.ValueObjects.HouseRules.Create(
                hr.CheckInTime, hr.CheckOutTime, hr.MaxGuests,
                hr.PetsAllowed, hr.PetsNotes, hr.SmokingAllowed,
                hr.PartiesAllowed, hr.QuietHoursStart, hr.QuietHoursEnd,
                hr.LeavingInstructions, hr.AdditionalRules));
        }

        if (request.CancellationPolicy is { } cp)
        {
            listing.SetCancellationPolicy(Domain.ValueObjects.CancellationPolicy.Create(
                cp.Type, cp.FreeCancellationDays,
                cp.PartialRefundPercent, cp.PartialRefundDays,
                cp.CustomTerms));
        }

        if (request.AmenityIds is not null)
        {
            listing.SetAmenities(request.AmenityIds);
        }

        if (request.SafetyDeviceIds is not null)
        {
            listing.SetSafetyDevices(request.SafetyDeviceIds);
        }

        if (request.ConsiderationIds is not null)
        {
            listing.SetConsiderations(request.ConsiderationIds);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }
}
