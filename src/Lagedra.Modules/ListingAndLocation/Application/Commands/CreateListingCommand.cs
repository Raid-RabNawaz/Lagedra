using Lagedra.Infrastructure.External.Geocoding;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.Policies;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record CreateListingCommand(
    Guid LandlordUserId,
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
    IReadOnlyList<Guid>? ConsiderationIds = null,
    bool InstantBookingEnabled = false,
    Uri? VirtualTourUrl = null,
    string? ApproxAddress = null) : IRequest<Result<ListingDetailsDto>>;

public sealed class CreateListingCommandHandler(
    ListingsDbContext dbContext,
    IGeocodingService geocodingService)
    : IRequestHandler<CreateListingCommand, Result<ListingDetailsDto>>
{
    public async Task<Result<ListingDetailsDto>> Handle(
        CreateListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stayRange = new StayRange(request.MinStayDays, request.MaxStayDays);

        var listing = Listing.Create(
            request.LandlordUserId,
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

        if (!string.IsNullOrWhiteSpace(request.ApproxAddress))
        {
            var geocoded = await geocodingService
                .GeocodeAddressAsync(request.ApproxAddress, cancellationToken)
                .ConfigureAwait(false);

            if (geocoded is not null)
            {
                listing.SetApproxLocation(new GeoPoint(geocoded.Latitude, geocoded.Longitude));
            }
        }

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
        else
        {
            listing.SetCancellationPolicy(CancellationPolicyDefaults.ForType(CancellationPolicyType.Moderate));
        }

        if (request.AmenityIds is { Count: > 0 })
        {
            listing.SetAmenities(request.AmenityIds);
        }

        if (request.SafetyDeviceIds is { Count: > 0 })
        {
            listing.SetSafetyDevices(request.SafetyDeviceIds);
        }

        if (request.ConsiderationIds is { Count: > 0 })
        {
            listing.SetConsiderations(request.ConsiderationIds);
        }

        listing.SetInstantBooking(request.InstantBookingEnabled);
        if (request.VirtualTourUrl is not null)
        {
            listing.SetVirtualTourUrl(request.VirtualTourUrl);
        }

        dbContext.Listings.Add(listing);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var initialPrice = ListingPriceHistory.Create(listing.Id, listing.MonthlyRentCents, today);
        dbContext.ListingPriceHistory.Add(initialPrice);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }
}
