namespace Lagedra.Modules.PartnerNetwork.Presentation.Contracts;

public sealed record CreateReservationRequest(
    string GuestName,
    string GuestEmail,
    Guid ListingId);
