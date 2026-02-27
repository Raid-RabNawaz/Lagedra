namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record SavedListingCollectionDto(
    Guid Id,
    string Name,
    DateTime CreatedAt,
    int ListingCount);
