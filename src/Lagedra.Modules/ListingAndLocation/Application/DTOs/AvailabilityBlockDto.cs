using Lagedra.Modules.ListingAndLocation.Domain.Enums;

namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record AvailabilityBlockDto(
    Guid Id,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    AvailabilityBlockType BlockType);

/// <summary>
/// Range-aware availability response (Phase 16): when the caller supplies a
/// <c>from</c>/<c>to</c> window we evaluate <see cref="Available"/> against
/// the listing's blocks and return only the blocks that overlap the window.
/// When no window is provided, <see cref="Available"/> is <c>true</c> and
/// <see cref="Blocks"/> is the full list of upcoming blocks (legacy shape
/// preserved for the host calendar UI).
/// </summary>
public sealed record ListingAvailabilityDto(
    bool Available,
    IReadOnlyList<AvailabilityBlockDto> Blocks);
