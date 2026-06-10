namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

/// <summary>
/// Phase 16.9 — request body for the booking SetupIntent endpoint
/// (<c>POST /v1/applications/setup-intent</c>). The tenant id is taken
/// from the authenticated principal; we only need the listing the
/// guest is applying to so the SetupIntent metadata is meaningful.
/// </summary>
public sealed record CreateBookingSetupIntentRequest(Guid ListingId);
