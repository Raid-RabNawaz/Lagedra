using Lagedra.Modules.Arbitration.Domain.Enums;

namespace Lagedra.Modules.Arbitration.Application.DTOs;

/// <summary>
/// Client material for paying an arbitration filing fee via Stripe Elements.
/// Returned when the filer opens the checkout for a <see cref="ArbitrationStatus.PendingPayment"/>
/// case.
/// </summary>
public sealed record ArbitrationFeeCheckoutDto(
    string ClientSecret,
    string PaymentIntentId,
    string PaymentStatus,
    long AmountCents,
    string Currency,
    ArbitrationStatus CaseStatus);
