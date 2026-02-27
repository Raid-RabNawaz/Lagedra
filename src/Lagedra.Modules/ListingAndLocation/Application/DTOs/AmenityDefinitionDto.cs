using Lagedra.Modules.ListingAndLocation.Domain.Enums;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record AmenityDefinitionDto(
    Guid Id,
    string Name,
    AmenityCategory Category,
    string IconKey);
