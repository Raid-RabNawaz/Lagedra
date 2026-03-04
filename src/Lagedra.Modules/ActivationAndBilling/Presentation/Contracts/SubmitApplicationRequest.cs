namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

public sealed record SubmitApplicationRequest(
    Guid ListingId,
    Guid TenantUserId,
    Guid LandlordUserId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut);
