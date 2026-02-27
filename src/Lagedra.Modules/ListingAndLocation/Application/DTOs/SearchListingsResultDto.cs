namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record SearchListingsResultDto(
    IReadOnlyList<ListingSummaryDto> Items,
    int TotalCount);
