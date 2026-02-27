using Lagedra.Modules.ListingAndLocation.Domain.Enums;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record AvailabilityBlockDto(
    Guid Id,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    AvailabilityBlockType BlockType);
