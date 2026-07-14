namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

public sealed record AttachApplicationPaymentRequest(
    string? StripePaymentMethodId,
    bool TruthSurfaceConsentGiven,
    string? ConsentVersion = null);
