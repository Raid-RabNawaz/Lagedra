namespace Lagedra.Modules.PartnerNetwork.Application.DTOs;

public sealed record DirectReservationDto(
    Guid Id,
    Guid OrganizationId,
    string GuestName,
    string GuestEmail,
    Guid ListingId,
    Guid? DealApplicationId,
    Guid ReservedByUserId,
    DateTime CreatedAt);
