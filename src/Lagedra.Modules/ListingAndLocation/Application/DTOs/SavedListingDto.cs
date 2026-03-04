namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record SavedListingDto(
    Guid ListingId,
    DateTime SavedAt);
