namespace Lagedra.Modules.ListingAndLocation.Presentation.Contracts;

public sealed record BlockDatesRequest(
    DateOnly CheckInDate,
    DateOnly CheckOutDate);
