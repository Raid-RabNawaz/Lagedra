namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

/// <summary>
/// Host approval payload. The deposit is no longer entered here — it was
/// predetermined for the tenant's verification tier and snapshotted at request
/// time. The host only confirms the Truth Surface terms.
/// </summary>
public sealed record ApproveApplicationRequest(
    bool TruthSurfaceConsentGiven = true,
    string? ConsentVersion = null);
