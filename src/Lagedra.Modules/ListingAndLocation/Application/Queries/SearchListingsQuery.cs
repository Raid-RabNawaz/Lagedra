using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record SearchListingsQuery(
    double? Latitude,
    double? Longitude,
    double? RadiusKm,
    PropertyType? PropertyType,
    int? MinBedrooms,
    int? MinBathrooms,
    int? MinStayDays,
    int? MaxStayDays,
    long? MinPriceCents,
    long? MaxPriceCents,
    DateOnly? AvailableFrom,
    DateOnly? AvailableTo,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<IReadOnlyList<ListingSummaryDto>>>;

public sealed class SearchListingsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<SearchListingsQuery, Result<IReadOnlyList<ListingSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<ListingSummaryDto>>> Handle(
        SearchListingsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.Listings
            .AsNoTracking()
            .Include(l => l.Photos)
            .Include(l => l.AvailabilityBlocks)
            .Where(l => l.Status == ListingStatus.Published || l.Status == ListingStatus.Activated);

        if (request.PropertyType.HasValue)
        {
            query = query.Where(l => l.PropertyType == request.PropertyType.Value);
        }

        if (request.MinBedrooms.HasValue)
        {
            query = query.Where(l => l.Bedrooms >= request.MinBedrooms.Value);
        }

        if (request.MinBathrooms.HasValue)
        {
            query = query.Where(l => l.Bathrooms >= request.MinBathrooms.Value);
        }

        if (request.MinPriceCents.HasValue)
        {
            query = query.Where(l => l.MonthlyRentCents >= request.MinPriceCents.Value);
        }

        if (request.MaxPriceCents.HasValue)
        {
            query = query.Where(l => l.MonthlyRentCents <= request.MaxPriceCents.Value);
        }

        var listings = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ListingSummaryDto> results = listings
            .Where(l => MatchesLocationFilter(l.ApproxGeoPoint, request))
            .Where(l => MatchesStayFilter(l.StayRange, request))
            .Where(l => MatchesAvailabilityFilter(l, request))
            .Select(ListingMapper.ToSummary)
            .ToList();

        return Result<IReadOnlyList<ListingSummaryDto>>.Success(results);
    }

    private static bool MatchesLocationFilter(GeoPoint? geoPoint, SearchListingsQuery request)
    {
        if (!request.Latitude.HasValue || !request.Longitude.HasValue || !request.RadiusKm.HasValue)
        {
            return true;
        }

        if (geoPoint is null)
        {
            return false;
        }

        var center = new GeoPoint(request.Latitude.Value, request.Longitude.Value);
        return geoPoint.DistanceKmTo(center) <= request.RadiusKm.Value;
    }

    private static bool MatchesAvailabilityFilter(
        Domain.Aggregates.Listing listing, SearchListingsQuery request)
    {
        if (!request.AvailableFrom.HasValue || !request.AvailableTo.HasValue)
        {
            return true;
        }

        return Domain.Services.AvailabilityService.IsAvailable(
            listing.AvailabilityBlocks, request.AvailableFrom.Value, request.AvailableTo.Value);
    }

    private static bool MatchesStayFilter(StayRange? stayRange, SearchListingsQuery request)
    {
        if (!request.MinStayDays.HasValue && !request.MaxStayDays.HasValue)
        {
            return true;
        }

        if (stayRange is null)
        {
            return false;
        }

        if (request.MinStayDays.HasValue && stayRange.MaxDays < request.MinStayDays.Value)
        {
            return false;
        }

        if (request.MaxStayDays.HasValue && stayRange.MinDays > request.MaxStayDays.Value)
        {
            return false;
        }

        return true;
    }
}
