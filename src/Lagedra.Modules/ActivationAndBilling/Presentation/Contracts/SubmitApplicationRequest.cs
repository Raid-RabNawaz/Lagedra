namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

public sealed record SubmitApplicationRequest(
    Guid ListingId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut,
    /// <summary>
    /// Headcount the tenant is booking for. Defaults to 1 so existing
    /// clients that haven't shipped the guest stepper yet still submit
    /// valid applications.
    /// </summary>
    int GuestCount = 1,
    /// <summary>
    /// Optional cover note from the tenant — analogous to Airbnb's
    /// "Send the host a message" field on the request screen.
    /// </summary>
    string? Message = null,
    /// <summary>
    /// Phase 16.9 — optional Stripe `pm_…` id captured during the apply
    /// dialog's SetupIntent step. When supplied, the host's approve action
    /// will charge it off-session and the tenant skips the checkout page.
    /// </summary>
    string? StripePaymentMethodId = null);
