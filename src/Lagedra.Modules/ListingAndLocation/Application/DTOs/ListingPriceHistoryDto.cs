namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record ListingPriceHistoryDto(
    Guid Id,
    long MonthlyRentCents,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);
