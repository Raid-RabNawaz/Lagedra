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
    /// Stripe `pm_…` id captured during the apply dialog's SetupIntent step.
    /// Required under the predetermined-deposit flow (BookingFlow.V2): the host
    /// charges it off-session on approval and the tenant skips a checkout step.
    /// </summary>
    string? StripePaymentMethodId = null,
    /// <summary>
    /// Tenant's Truth Surface consent given up-front at request time. Required
    /// under the predetermined-deposit flow. The IP/User-Agent are captured
    /// server-side from the request, not trusted from the client.
    /// </summary>
    bool TruthSurfaceConsentGiven = false,
    /// <summary>
    /// Version identifier of the consent text the tenant agreed to. When null
    /// the server records its current consent version.
    /// </summary>
    string? ConsentVersion = null);
