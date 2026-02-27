namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

public sealed record FileDamageClaimRequest(
    string Description,
    long ClaimedAmountCents,
    Guid? EvidenceManifestId);
