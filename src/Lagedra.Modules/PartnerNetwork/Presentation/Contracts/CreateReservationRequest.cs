namespace Lagedra.Modules.PartnerNetwork.Presentation.Contracts;

public sealed record CreateReservationRequest(
    Guid TenantUserId,
    Guid ListingId,
    string PayerType,
    DateOnly? RequestedCheckIn = null,
    DateOnly? RequestedCheckOut = null,
    string? StripePaymentMethodId = null);
