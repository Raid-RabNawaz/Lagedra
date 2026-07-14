namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

/// <summary>
/// Host payload confirming they returned the deposit directly to the tenant.
/// When <see cref="ReturnedAmountCents"/> is less than the deposit paid,
/// <see cref="Note"/> (deduction reason) and a sealed
/// <see cref="EvidenceManifestId"/> (damage photo) are required.
/// </summary>
public sealed record ConfirmDepositReturnRequest(
    long ReturnedAmountCents,
    string? Method,
    string? Note,
    Guid? EvidenceManifestId = null);
