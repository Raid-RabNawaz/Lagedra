namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

public sealed record SubmitApplicationRequest(
    Guid ListingId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut);
