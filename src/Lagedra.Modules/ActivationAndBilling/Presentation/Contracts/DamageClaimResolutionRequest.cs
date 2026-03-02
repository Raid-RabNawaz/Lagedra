namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

public sealed record DamageClaimResolutionRequest(
    long? ApprovedAmountCents,
    string? Notes);
