namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

/// <summary>
/// Admin-facing reconciliation between the protocol fee hosts are *shown*
/// (the platform setting) and the amount they are *actually* billed (the Stripe
/// subscription price). <see cref="Issue"/> is null when in sync, otherwise a
/// stable code: <c>price_not_configured</c>, <c>stripe_error</c>,
/// <c>no_unit_amount</c>, or <c>drift</c>.
/// </summary>
public sealed record ProtocolFeeReconciliationDto(
    bool PriceConfigured,
    string? StripePriceId,
    long ConfiguredMonthlyFeeCents,
    long? StripePriceAmountCents,
    bool InSync,
    string? Issue);
