using Lagedra.Modules.ListingAndLocation.Domain.Enums;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record ListingAmenityDto(
    Guid Id,
    string Name,
    AmenityCategory Category,
    string IconKey);
