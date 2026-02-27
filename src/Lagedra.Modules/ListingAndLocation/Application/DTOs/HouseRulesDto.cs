namespace Lagedra.Modules.ListingAndLocation.Application.DTOs;

public sealed record HouseRulesDto(
    TimeOnly CheckInTime,
    TimeOnly CheckOutTime,
    int MaxGuests,
    bool PetsAllowed,
    string? PetsNotes,
    bool SmokingAllowed,
    bool PartiesAllowed,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string? LeavingInstructions,
    string? AdditionalRules);
