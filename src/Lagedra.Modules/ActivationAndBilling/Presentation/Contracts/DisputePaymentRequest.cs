namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

public sealed record DisputePaymentRequest(
    string Reason,
    Guid? EvidenceManifestId);
