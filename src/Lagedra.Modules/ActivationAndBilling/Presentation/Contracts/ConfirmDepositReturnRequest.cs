namespace Lagedra.Modules.ActivationAndBilling.Presentation.Contracts;

/// <summary>
/// Host payload confirming they returned the deposit directly to the tenant.
/// </summary>
public sealed record ConfirmDepositReturnRequest(
    long ReturnedAmountCents,
    string? Method,
    string? Note);
