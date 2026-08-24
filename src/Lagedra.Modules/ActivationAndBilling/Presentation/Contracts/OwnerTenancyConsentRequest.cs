namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

public sealed record OwnerTenancyConsentRequest(
    bool ConsentGiven = true,
    string? ConsentVersion = null);
