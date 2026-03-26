using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record SearchListingsQuery(
    string? Keyword,
    double? Latitude,
    double? Longitude,
    double? RadiusKm,
    double? SwLat,
    double? SwLng,
    double? NeLat,
    double? NeLng,
    PropertyType? PropertyType,
    int? MinBedrooms,
    int? MinBathrooms,
    int? MinStayDays,
    int? MaxStayDays,
    long? MinPriceCents,
    long? MaxPriceCents,
    DateOnly? AvailableFrom,
    DateOnly? AvailableTo,
    IReadOnlyList<Guid>? AmenityIds,
    IReadOnlyList<Guid>? SafetyDeviceIds,
    IReadOnlyList<Guid>? ConsiderationIds,
    SearchListingsSortBy SortBy = SearchListingsSortBy.Newest,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<SearchListingsResultDto>>;

public sealed class SearchListingsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<SearchListingsQuery, Result<SearchListingsResultDto>>
{
    private const double KmPerDegreeLat = 111.0;

    public async Task<Result<SearchListingsResultDto>> Handle(
        SearchListingsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.Listings
            .AsNoTracking()
            .Include(l => l.Photos)
            .Include(l => l.Amenities)
            .Include(l => l.SafetyDevices)
            .Include(l => l.Considerations)
            .Include(l => l.AvailabilityBlocks)
            .Where(l => l.Status == ListingStatus.Published || l.Status == ListingStatus.Activated);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var pattern = $"%{request.Keyword.Trim()}%";
            query = query.Where(l =>
                EF.Functions.ILike(l.Title, pattern) ||
                EF.Functions.ILike(l.Description, pattern));
        }

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

        if (request.MinStayDays.HasValue)
        {
            query = query.Where(l => l.StayRange != null && l.StayRange.MaxDays >= request.MinStayDays.Value);
        }

        if (request.MaxStayDays.HasValue)
        {
            query = query.Where(l => l.StayRange != null && l.StayRange.MinDays <= request.MaxStayDays.Value);
        }

        if (request.AvailableFrom.HasValue && request.AvailableTo.HasValue)
        {
            var from = request.AvailableFrom.Value;
            var to = request.AvailableTo.Value;
            query = query.Where(l => !l.AvailabilityBlocks.Any(b =>
                b.CheckInDate < to && b.CheckOutDate > from));
        }

        if (request.Latitude.HasValue && request.Longitude.HasValue && request.RadiusKm.HasValue)
        {
            var lat = request.Latitude.Value;
            var lon = request.Longitude.Value;
            var radiusKm = request.RadiusKm.Value;
            var deltaLat = radiusKm / KmPerDegreeLat;
            var deltaLon = radiusKm / (KmPerDegreeLat * Math.Cos(lat * Math.PI / 180));
            query = query.Where(l =>
                l.ApproxGeoPoint != null &&
                l.ApproxGeoPoint.Latitude >= lat - deltaLat &&
                l.ApproxGeoPoint.Latitude <= lat + deltaLat &&
                l.ApproxGeoPoint.Longitude >= lon - deltaLon &&
                l.ApproxGeoPoint.Longitude <= lon + deltaLon);
        }
        else if (request.SwLat.HasValue && request.SwLng.HasValue &&
                 request.NeLat.HasValue && request.NeLng.HasValue)
        {
            var swLat = request.SwLat.Value;
            var swLng = request.SwLng.Value;
            var neLat = request.NeLat.Value;
            var neLng = request.NeLng.Value;
            query = query.Where(l =>
                l.ApproxGeoPoint != null &&
                l.ApproxGeoPoint.Latitude >= swLat &&
                l.ApproxGeoPoint.Latitude <= neLat &&
                l.ApproxGeoPoint.Longitude >= swLng &&
                l.ApproxGeoPoint.Longitude <= neLng);
        }

        if (request.AmenityIds is { Count: > 0 } amenityIds)
        {
            query = query.Where(l =>
                amenityIds.All(aid =>
                    l.Amenities.Any(a => a.AmenityDefinitionId == aid)));
        }

        if (request.SafetyDeviceIds is { Count: > 0 } safetyIds)
        {
            query = query.Where(l =>
                safetyIds.All(sid =>
                    l.SafetyDevices.Any(s => s.SafetyDeviceDefinitionId == sid)));
        }

        if (request.ConsiderationIds is { Count: > 0 } considerationIds)
        {
            query = query.Where(l =>
                considerationIds.All(cid =>
                    l.Considerations.Any(c => c.ConsiderationDefinitionId == cid)));
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        IReadOnlyList<Listing> listings;
        if (request.SortBy == SearchListingsSortBy.Distance &&
            request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var allMatching = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
            var center = new GeoPoint(request.Latitude.Value, request.Longitude.Value);
            listings = allMatching
                .Where(l => l.ApproxGeoPoint != null)
                .OrderBy(l => l.ApproxGeoPoint!.DistanceKmTo(center))
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();
        }
        else
        {
            query = ApplySort(query, request);
            listings = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var items = listings.Select(l => ListingMapper.ToSummary(l)).ToList();

        return Result<SearchListingsResultDto>.Success(
            new SearchListingsResultDto(items, totalCount));
    }

    private static IQueryable<Listing> ApplySort(IQueryable<Listing> query, SearchListingsQuery request)
    {
        return request.SortBy switch
        {
            SearchListingsSortBy.PriceAsc => query.OrderBy(l => l.MonthlyRentCents).ThenByDescending(l => l.CreatedAt),
            SearchListingsSortBy.PriceDesc => query.OrderByDescending(l => l.MonthlyRentCents).ThenByDescending(l => l.CreatedAt),
            SearchListingsSortBy.Distance =>
                query.OrderByDescending(l => l.CreatedAt),
            _ => query.OrderByDescending(l => l.CreatedAt)
        };
    }
}
